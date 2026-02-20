using JeweleryAppBackend.Enumerations;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ProductHighlightsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private ProductService _productService;
    public ProductHighlightsController(ProductService productService,IMemoryCache cache,ApplicationDbContext context)
    {
        _context = context;
        _cache = cache;
        _productService = productService;
    }

    // ✅ CREATE
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateProductHighlightModel model)
    {
        var highlight = new ProductHighlight
        {
            ProductId = model.ProductId,
            Type = (ProductHighlightType)model.Type, 
            Id = Guid.NewGuid(),
            CreatedOn = DateTime.UtcNow
        };

        _context.ProductHighlights.Add(highlight);
        await _context.SaveChangesAsync();

        return Ok(model);
    }

    // ✅ UPDATE
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, ProductHighlight model)
    {
        var existing = await _context.ProductHighlights.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.ProductId = model.ProductId;
        existing.Type = model.Type;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    // ✅ DELETE
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.ProductHighlights.FindAsync(id);
        if (entity == null)
            return NotFound();

        _context.ProductHighlights.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok();
    }

    // ✅ GET BY ID
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _context.ProductHighlights
            .Include(x => x.Product)
            .Where(x => x.Id == id)
            .Select(x => new ProductHighlightViewModel
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Price = x.Product.Price
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    // ✅ GET ALL (WITH PAGINATION + COUNT 🔥)
    [HttpPost("GetAll")]
    [Authorize(Roles ="Admin")]
    public async Task<ActionResult<ProductHighlightViewModel>> GetAll(ProductHighlightSearchModel model)
    {
        var query = _context.ProductHighlights
            .Include(x => x.Product)
            .AsQueryable();
         

        // COUNT
        var count = await query.CountAsync();

        // DATA
        var data = await query
            .OrderByDescending(x => x.CreatedOn)
            .Skip(model.Skip)
            .Take(model.Take)
            .Select(x => new ProductHighlightViewModel
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Price = x.Product.Price,
                Type = x.Type
            })
            .ToListAsync();

        return Ok(new
        {
            Count = count,
            Data = data
        });
    }
    [HttpGet("GetProductsByHighlightType")]
    public async Task<ActionResult<List<ProductViewModel>>> GetProductsByHighlightType(int type)
    {
        string cacheKey = $"products_highlight_{type}";

        // ✅ 1. Try cache
        if (!_cache.TryGetValue(cacheKey, out List<ProductViewModel> cachedProducts))
        {
            // ✅ 2. Get highlight entries + product (light join)
            var highlights = await (
                from h in _context.ProductHighlights
                join p in _context.Products on h.ProductId equals p.Id
                where h.Type == (ProductHighlightType)type && p.IsActive
                orderby p.Price
                select p
            )
            .AsNoTracking()
            .Take(10)
            .ToListAsync();

            if (!highlights.Any())
                return Ok(new List<ProductViewModel>());

            var result = new List<ProductViewModel>();

            // ✅ 3. Attach images (cached inside service)
            foreach (var product in highlights)
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
    [HttpGet("GetAvailableProducts")]
    public async Task<IActionResult> GetAvailableProducts(ProductHighlightType type)
    {
        var productIdsInHighlights = await _context.ProductHighlights
            .Where(x => x.Type == type)
            .Select(x => x.ProductId)
            .ToListAsync();

        var products = await _context.Products
            .Where(p => p.IsActive && !productIdsInHighlights.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name
            })
            .ToListAsync();

        return Ok(products);
    }
}