using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Orleans.Runtime.UniqueKey;

namespace JeweleryAppBackend.Controllers;

[Route("api/Products")]
[ApiController]
public class ProductsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private ProductService _productService;

	private ImageService _imageService;
    private readonly IMemoryCache _cache;

    public ProductsController(IMemoryCache cache,ApplicationDbContext context, ProductService productService, ImageService imageService)
	{
		_context = context;
		_productService = productService;
		_imageService = imageService;
		_cache = cache;
	}

    [HttpGet("GetAll")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ProductViewModel>>> GetProducts(int skip, int take)
    {
        var products = await _context.Products
            .Include(p => p.Category)
                .ThenInclude(c => c.Parent)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        List<ProductViewModel> list = new();

        foreach (var product in products)
        {
            var images = await _productService.GetProductImages(product.Id);
            var specifications = await GetAllProductSpecifications(product.Id);

            list.Add(new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                DetailedDescription = product.DetailedDescription,
                Price = product.Price,
                CategoryId = product.CategoryId,
                Images = images,
                Category = product.Category, // 👈 already has Parent
                Code = product.Code,
                IsActive = product.IsActive,
                Specifications = specifications
            });
        }

        return Ok(list);
    }

    [HttpPut("UpdateIsActive")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult> UpdateIsActive(Guid id, bool isActive)
	{
		(await _context.Products.FindAsync(id)).IsActive = isActive;
		await _context.SaveChangesAsync();
		return Ok();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return await _context.Products.CountAsync();
	} 
    [HttpPost("GetUserProducts")]  
    public async Task<ActionResult<List<ProductViewModel>>> GetUserProducts(UserProductsSearchModel model)
    {
        // ✅ NORMALIZED CACHE KEY
        var cacheKey = $"products_" +
            $"{string.Join(",", model.CategoryIds.OrderBy(x => x))}_" +
            $"{string.Join(",", model.SpecificationIds.OrderBy(x => x))}_" +
            $"{model.Name}_{model.StartPrice}_{model.EndPrice}_{model.Sort}_{model.Skip}_{model.Take}";

        if (_cache.TryGetValue(cacheKey, out List<ProductViewModel> cached))
            return Ok(cached);

        IQueryable<ProductModel> query = _context.Products.AsNoTracking();

        // ✅ CATEGORY FILTER
        if (model.CategoryIds.Any())
            query = query.Where(p => model.CategoryIds.Contains(p.CategoryId) && p.IsActive);
        else
            query = query.Where(p => p.IsActive);

        // ✅ 🔥 FIXED SPEC FILTER (NO CORRELATED SUBQUERY)
        if (model.SpecificationIds.Any())
        {
            var filteredProductIds = await _context.ProductSpecifications
                .AsNoTracking()
                .Where(x => model.SpecificationIds.Contains(x.SpecificationId))
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync();

            if (!filteredProductIds.Any())
                return Ok(new List<ProductViewModel>());

            query = query.Where(p => filteredProductIds.Contains(p.Id));
        }

        // ✅ NAME FILTER
        if (!string.IsNullOrWhiteSpace(model.Name))
            query = query.Where(p => p.Name.Contains(model.Name));

        // ✅ PRICE FILTER
        if (model.StartPrice.HasValue)
        {
            query = GetProductsByCategoryAndPriceRange(
                query,
                model.CategoryIds.FirstOrDefault(),
                model.StartPrice.Value,
                model.EndPrice.Value,
                model.Sort
            );
        }

        // ✅ SORT
        query = model.Sort switch
        {
            "name asc" => query.OrderBy(x => x.Name),
            "name desc" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Price)
        };

        // ✅ PAGINATION
        var products = await query
            .Skip(model.Skip)
            .Take(model.Take)
            .ToListAsync();

        if (!products.Any())
            return Ok(new List<ProductViewModel>());

        var productIds = products.Select(p => p.Id).ToList();
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();

        // ✅ 🚫 NO PARALLEL DB CALLS (FIXED ERROR)
        var categories = await _context.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var images = await _context.ProductImages
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync();

        var specifications = await (
            from ps in _context.ProductSpecifications.AsNoTracking()
            join s in _context.Specifications on ps.SpecificationId equals s.Id
            where productIds.Contains(ps.ProductId)
            select new
            {
                ps.ProductId,
                ps.SpecificationId,
                s.Name,
                s.Value,
                ps.Price
            }
        ).ToListAsync();

        // ✅ IMAGE CACHE (PREVENT DISK HIT)
        string GetBase64Cached(string path, string name)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return null;

            return _cache.GetOrCreate($"img_{path}", entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
                return _imageService.GetBase64Prefix(name) +
                       Convert.ToBase64String(System.IO.File.ReadAllBytes(path));
            });
        }

        // ✅ IMAGE LOOKUP
        var imageLookup = images
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(img => new ProductImageViewModel
                {
                    Id = img.Id,
                    Name = img.Name,
                    Base64 = GetBase64Cached(img.Src, img.Name),
                    ZoomedBase64 = GetBase64Cached(img.ZoomedImageSrc, img.Name),
                    SpecificationId = img.SpecificationId
                }).ToList()
            );

        // ✅ SPEC LOOKUP
        var specLookup = specifications
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => new ProductSpecificationViewModel
                {
                    Id = s.SpecificationId,
                    ProductId = s.ProductId,
                    Name = s.Name,
                    Value = s.Value,
                    Price = s.Price
                }).ToList()
            );

        // ✅ FINAL MAPPING
        var result = products.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            DetailedDescription = p.DetailedDescription,
            Price = p.Price,
            CategoryId = p.CategoryId,
            Category = categories.GetValueOrDefault(p.CategoryId),
            Images = imageLookup.GetValueOrDefault(p.Id) ?? new List<ProductImageViewModel>(),
            Code = p.Code,
            Specifications = specLookup.GetValueOrDefault(p.Id) ?? new List<ProductSpecificationViewModel>()
        }).ToList();

        // ✅ CACHE
        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        });

        return Ok(result);
    }
    [HttpPost("GetUserProductsCount")]
	public async Task<ActionResult<int>> GetUserProductsCount(UserProductsSearchModel model)
	{
		int count = 0;
		if (model.CategoryIds.Any())
		{
			IQueryable<ProductModel> query = _context.Products.AsQueryable();
			query = query.Where((ProductModel p) => model.CategoryIds.Contains(p.CategoryId) && p.IsActive);
			if (model.SpecificationIds.Any())
			{
				query = query.Where((ProductModel p) => _context.ProductSpecifications.Any((ProductSpecificationModel x) => x.ProductId == p.Id && model.SpecificationIds.Contains(x.SpecificationId)));
			}
			if (!string.IsNullOrEmpty(model.Name))
			{
				query = query.Where((ProductModel p) => p.Name.Contains(model.Name));
			}
			if (model.StartPrice.HasValue)
			{
				query = GetProductsByCategoryAndPriceRange(query, model.CategoryIds.FirstOrDefault(), model.StartPrice.Value, model.EndPrice.Value, model.Sort);
			}
			count = await query.CountAsync();
		}
		return Ok(count);
	}

	private IQueryable<ProductModel> GetProductsByCategoryAndPriceRange(IQueryable<ProductModel> query, Guid categoryId, decimal startPrice, decimal endPrice, string sort = "")
	{
		var result = from p in query
			where p.ProductSpecifications.Any((ProductSpecificationModel ps) => ps.Specification.CategoryId == categoryId)
			select new
			{
				Product = p,
				TotalPrice = (from ps in p.ProductSpecifications
					group ps by ps.Specification.Name into g
					select g.Min((ProductSpecificationModel ps) => ps.Price)).Sum()
			} into x
			where x.TotalPrice >= startPrice && x.TotalPrice <= endPrice
			select x;
		if (sort == "price asc")
		{
			result = result.OrderBy(x => x.TotalPrice);
		}
		if (sort == "price desc")
		{
			result = result.OrderByDescending(x => x.TotalPrice);
		}
		return result.Select(x => x.Product);
	}

    [HttpGet("GetUserProductsByCategory")]
    public async Task<ActionResult<List<ProductViewModel>>> GetUserProductsByCategory(Guid id)
    {
        string cacheKey = $"products_category_{id}";

        // ✅ 1. Try cache
        if (!_cache.TryGetValue(cacheKey, out List<ProductViewModel> cachedProducts))
        {
            // ✅ 2. Get products (light query)
            var products = await _context.Products
                .AsNoTracking()
                .Where(x => x.CategoryId == id && x.IsActive)
                .OrderBy(x => x.Price)
                .Take(10)
                .ToListAsync();

            if (!products.Any())
                return Ok(new List<ProductViewModel>());

            var result = new List<ProductViewModel>();

            // ✅ 3. Attach images (with INTERNAL caching in service)
            foreach (var product in products)
            {
                var images = await _productService.GetProductImages(product.Id);

                result.Add(new ProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Code = product.Code,
                    Images = images
                });
            }

            // ✅ 4. Cache result
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(cacheKey, result, cacheOptions);

            return Ok(result);
        }

        // ✅ 5. Return cached
        return Ok(cachedProducts);
    }

    private async Task<List<ProductSpecificationViewModel>> GetAllProductSpecifications(Guid id)
	{
		List<ProductSpecificationViewModel> list = new List<ProductSpecificationViewModel>();
		List<ProductSpecificationModel> productSpecifications = await _context.ProductSpecifications.Where((ProductSpecificationModel x) => x.ProductId == id).ToListAsync();
		if (productSpecifications.Any())
		{
			List<Guid> specificationIds = productSpecifications.Select((ProductSpecificationModel x) => x.SpecificationId).ToList();
			foreach (SpecificationsModel item in await _context.Specifications.Where((SpecificationsModel x) => specificationIds.Contains(x.Id)).ToListAsync())
			{
				list.Add(new ProductSpecificationViewModel
				{
					Name = item.Name,
					Value = item.Value,
					Id = item.Id,
					Price = productSpecifications.FirstOrDefault((ProductSpecificationModel x) => x.SpecificationId == item.Id).Price
				});
			}
		}
		return list;
	}

	[HttpGet("GetItemsCountByCategory")]
	public async Task<ActionResult<int>> GetItemsCountByCategoryAsync(Guid categoryId)
	{
		return await _context.Products.CountAsync((ProductModel p) => p.CategoryId == categoryId);
	}

	[HttpGet("GetAllProductImages")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<ProductImageViewModel>>> GetAllProductImages(Guid id)
	{
		return Ok(await _productService.GetProductImages(id));
	}

	[HttpPost("GetMaxPrice")]
	public async Task<ActionResult<decimal>> GetMaxPrice(MaxPriceRequestModel model)
	{
		decimal maxPrice = default(decimal);
		if (model.CategoryIds.Any())
		{
			List<ProductSpecificationViewModel> getCategorySpecifications = await GetAllProductSpecificationsByCategory(model.CategoryIds.FirstOrDefault());
			if (getCategorySpecifications.Any())
			{
				decimal sumOfHighestPrices = (from spec in getCategorySpecifications
					group spec by new { spec.Name, spec.ProductId } into @group
					select @group.Max((ProductSpecificationViewModel spec) => spec.Price)).Sum();
				maxPrice = Math.Ceiling(sumOfHighestPrices);
			}
			else
			{
				IQueryable<ProductModel> query = _context.Products.AsQueryable();
				query = query.Where((ProductModel p) => model.CategoryIds.Contains(p.CategoryId));
				if (!string.IsNullOrEmpty(model.Name))
				{
					query = query.Where((ProductModel p) => p.Name.Contains(model.Name));
				}
				if (query.Any())
				{
					maxPrice = await query.MaxAsync((ProductModel p) => p.Price);
				}
			}
		}
		return Ok(maxPrice);
	}

	private async Task<List<ProductSpecificationViewModel>> GetAllProductSpecificationsByCategory(Guid id)
	{
		List<ProductSpecificationViewModel> list = new List<ProductSpecificationViewModel>();
		List<Guid> specificationIdList = await (from x in _context.Specifications
			where x.CategoryId == id
			select x.Id).ToListAsync();
		List<ProductSpecificationModel> productSpecifications = await _context.ProductSpecifications.Where((ProductSpecificationModel x) => specificationIdList.Contains(x.SpecificationId)).ToListAsync();
		if (productSpecifications.Any())
		{
			List<Guid> specificationIds = productSpecifications.Select((ProductSpecificationModel x) => x.SpecificationId).ToList();
			List<SpecificationsModel> specifications = await _context.Specifications.Where((SpecificationsModel x) => specificationIds.Contains(x.Id)).ToListAsync();
			foreach (ProductSpecificationModel item in productSpecifications)
			{
				list.Add(new ProductSpecificationViewModel
				{
					Name = specifications.FirstOrDefault((SpecificationsModel x) => x.Id == item.SpecificationId).Name,
					Value = specifications.FirstOrDefault((SpecificationsModel x) => x.Id == item.SpecificationId).Value,
					Id = item.Id,
					ProductId = item.ProductId,
					Price = item.Price
				});
			}
		}
		return list;
	}

	[HttpGet("GetById")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ProductViewModel>> GetProduct(Guid id)
	{
		return await GetProductModel(await _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.Parent)
                .FirstOrDefaultAsync(x => x.Id == id));
	}

    [HttpGet("GetByCode")]
    public async Task<ActionResult<ProductViewModel>> GetProduct(string code)
    {
        string cacheKey = $"product_{code}";

        if (!_cache.TryGetValue(cacheKey, out ProductViewModel cachedProduct))
        {
            var product = await _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.Parent)
                .FirstOrDefaultAsync(x => x.Code == code);

            if (product == null)
                return NotFound();

            cachedProduct = await GetProductModel(product);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), // 🔥 adjust as needed
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            _cache.Set(cacheKey, cachedProduct, cacheOptions);
        }

        return Ok(cachedProduct);
    }
    private async Task<ProductViewModel> GetProductModel(ProductModel product)
    {
        var images = await _productService.GetProductImages(product.Id);
        var specifications = await GetAllProductSpecifications(product.Id);

        return new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            DetailedDescription = product.DetailedDescription,
            Price = product.Price,
            CategoryId = product.CategoryId,
            Code = product.Code,
            Category = product.Category,
            Images = images,
            IsActive = product.IsActive,
            Specifications = specifications
        };
    }

    [HttpPost("Post")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ProductModel>> PostProduct(AddProductModel product)
	{
		product.Id = Guid.NewGuid();
		ProductModel newProduct = new ProductModel
		{
			Id = product.Id,
			Name = product.Name,
			Description = product.Description,
			DetailedDescription = product.DetailedDescription,
			CategoryId = product.CategoryId,
			Price = product.Price,
			Code = GenerateProductCode(),
			IsActive = true
		};
		_context.Products.Add(newProduct);
		await _context.SaveChangesAsync();
        _cache.Remove($"product_{product.Code}");
        _cache.Remove($"products_category_{product.CategoryId}");
        _cache.Remove($"product_images_{product.Id}");
        return Ok(newProduct);
	}

	[HttpPost("UpdateProductImages")]
	[Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> UpdateProductImages([FromForm] AddProductImagesModel product)
    {
        try
        {
            if (product.Images == null || product.Images.Count == 0)
                return BadRequest("No images provided.");

            string directoryPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Images",
                "ProductImages",
                product.ProductId.ToString()
            );

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            foreach (var image in product.Images)
            {
                if (image == null || image.Length == 0)
                    continue;

                // 🔥 Process main image (WebP)
                var processedImage = await _imageService.ResizeImageAsync(
                    image,
                    maxSize: 1200,
                    convertToWebp: true,
                    quality: 75
                );

                // 🔥 Process zoom image (higher quality)
                var zoomedImage = await _imageService.ResizeImageAsync(
                    image,
                    maxSize: 2000,
                    convertToWebp: true,
                    quality: 85
                );

                // ✅ Keep SAME naming style (but fix overwrite issue)
                string baseName = Path.GetFileNameWithoutExtension(image.FileName);

                string fileName = $"{baseName}-{Guid.NewGuid()}.webp";
                string zoomFileName = $"{baseName}-zoom-{Guid.NewGuid()}.webp";

                string filePath = Path.Combine(directoryPath, fileName);
                string zoomedImageFilePath = Path.Combine(directoryPath, zoomFileName);

                // 💾 Save main image
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await processedImage.CopyToAsync(stream);
                }

                // 💾 Save zoom image
                using (var stream = new FileStream(zoomedImageFilePath, FileMode.Create))
                {
                    await zoomedImage.CopyToAsync(stream);
                }

                // ✅ KEEP SAME DB PATH STYLE (absolute path like before)
                ProductImagesModel productImage = new ProductImagesModel
                {
                    Id = Guid.NewGuid(),
                    Src = filePath,
                    ZoomedImageSrc = zoomedImageFilePath,
                    ProductId = product.ProductId,
                    IsTitleImage = false,
                    Name = image.FileName
                };

                _context.ProductImages.Add(productImage);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Images uploaded successfully 🚀"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("Update")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Update(ProductModel product)
	{
		try
		{
			if (product == null || product.Id == Guid.Empty)
			{
				return BadRequest("Invalid product data.");
			}
			ProductModel existingProduct = _context.Products.Find(product.Id);
			if (existingProduct == null)
			{
				return NotFound("Product not found.");
			}
			existingProduct.Name = product.Name;
			existingProduct.Description = product.Description;
			existingProduct.DetailedDescription = product.DetailedDescription;
			existingProduct.Price = product.Price;
			existingProduct.CategoryId = product.CategoryId;
			existingProduct.IsActive = product.IsActive;
			await _context.SaveChangesAsync();
            _cache.Remove($"product_{product.Code}");
            _cache.Remove($"products_category_{product.CategoryId}");
            _cache.Remove($"product_images_{product.Id}");
            return Ok();
		}
		catch (Exception)
		{
			return NotFound();
		}
	}

	[HttpDelete("Delete")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteProduct(Guid id)
	{
		ProductModel product = await _context.Products.FindAsync(id);
		if (product == null)
		{
			return NotFound();
		}
		_context.Products.Remove(product);
		await _context.SaveChangesAsync();
        _cache.Remove($"product_{product.Code}");
        _cache.Remove($"products_category_{product.CategoryId}");
        _cache.Remove($"product_images_{product.Id}");
        return NoContent();
	}

	[HttpDelete("DeleteProductImage")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteProductImage(Guid id)
	{
		ProductImagesModel productImage = await _context.ProductImages.FindAsync(id);
		if (productImage == null)
		{
			return NotFound();
		}
		if (System.IO.File.Exists(productImage.Src))
		{
			System.IO.File.Delete(productImage.Src);
		}
		if (System.IO.File.Exists(productImage.ZoomedImageSrc))
		{
			System.IO.File.Delete(productImage.ZoomedImageSrc);
		}
		_context.ProductImages.Remove(productImage);
		await _context.SaveChangesAsync();
		return NoContent();
	}

	public static string GenerateProductCode()
	{
		Random random = new Random();
		string prefix = "";
		StringBuilder productNumber = new StringBuilder(prefix);
		for (int i = 0; i < 10; i++)
		{
			productNumber.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Length)]);
		}
		return productNumber.ToString();
	}

	[HttpPost("UpdateImageSpecifications")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ProductModel>> UpdateImageSpecifications(UpdateImageSpecificationsModel model)
	{
		(await _context.ProductImages.FindAsync(model.ImageId)).SpecificationId = model.SpecificationId;
		await _context.SaveChangesAsync();
		return Ok();
	}
}
