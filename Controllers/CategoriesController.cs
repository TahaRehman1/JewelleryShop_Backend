using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace JeweleryAppBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
	private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    public CategoriesController(ApplicationDbContext context, IMemoryCache cache)
	{
		_context = context;
		_cache = cache;
	}

	[HttpGet("GetAllMain")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<CategoryModel>>> GetAllMain()
	{
        return await _context.Categories
               .Include(x => x.Parent) // 👈 include parent data
               .Where(x => x.IsMenuOnly == false)
               .ToListAsync();
    }

    [HttpGet("GetAllNavCategories")]
    public async Task<ActionResult<List<NavCategoryDto>>> GetAllNavCategories()
    {
        string cacheKey = "NAV_CATEGORIES";

        // ✅ Try cache first
        if (_cache.TryGetValue(cacheKey, out List<NavCategoryDto> cachedData))
        {
            return cachedData;
        }

        // ✅ Single DB call (no duplicate queries)
        var categories = await _context.Categories
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ParentId
            })
            .ToListAsync();

        // ✅ Dictionary for O(1) lookup
        var lookup = categories.ToDictionary(x => x.Id);

        // ✅ Prepare result dictionary (parent -> dto)
        var parentMap = new Dictionary<Guid, NavCategoryDto>();

        foreach (var cat in categories)
        {
            // ✅ If parent
            if (cat.ParentId == null)
            {
                if (!parentMap.ContainsKey(cat.Id))
                {
                    parentMap[cat.Id] = new NavCategoryDto
                    {
                        Id = cat.Id,
                        Name = cat.Name,
                        Children = new List<NavCategoryDto>()
                    };
                }
            }
            else
            {
                // ✅ Ensure parent exists
                if (lookup.TryGetValue(cat.ParentId.Value, out var parent))
                {
                    if (!parentMap.ContainsKey(parent.Id))
                    {
                        parentMap[parent.Id] = new NavCategoryDto
                        {
                            Id = parent.Id,
                            Name = parent.Name,
                            Children = new List<NavCategoryDto>()
                        };
                    }

                    // ✅ Add child
                    parentMap[parent.Id].Children.Add(new NavCategoryDto
                    {
                        Id = cat.Id,
                        Name = cat.Name
                    });
                }
            }
        }

        var result = parentMap.Values.ToList();

        // ✅ Cache options
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

        // ✅ Store in cache
        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    [HttpGet("GetSubCategories")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<CategoryModel>>> GetSubCategories(Guid id)
	{
		return await _context.Categories.Where((CategoryModel x) => x.ParentId == id).ToListAsync();
	}
    [HttpGet("GetAllMenuParent")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CategoryModel>>> GetAllMenuParent()
    {
        return await _context.Categories.Where((CategoryModel x) => x.IsMenuOnly).ToListAsync();
    }
    [HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<CategoryViewModel>>> GetAll(int skip, int take)
	{
		return await (from c in _context.Categories.Skip(skip).Take(take)
			select new CategoryViewModel
			{
				Id = c.Id,
				Name = c.Name,
				ParentCategory = ((c.ParentId != null) ? _context.Categories.FirstOrDefault((CategoryModel pc) => pc.Id == c.ParentId) : new CategoryModel()),
				IsMenuOnly = c.IsMenuOnly,
			}).ToListAsync();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.Categories.ToListAsync()).Count;
	}

	[HttpGet("GetById")]
	public async Task<ActionResult<CategoryModel>> GetById(Guid id)
	{
		CategoryModel CategoryModel = await _context.Categories.FindAsync(id);
		if (CategoryModel == null)
		{
			return NotFound();
		}
		return CategoryModel;
	}

	[HttpGet("GetByName")]
	public async Task<ActionResult<CategoryModel>> GetByName(string name)
	{
		CategoryModel categoryModel = await _context.Categories.FirstOrDefaultAsync((CategoryModel x) => x.Name == name);
		if (categoryModel == null)
		{
			return NotFound();
		}
		return categoryModel;
	}

	[HttpGet("GetSubCategoriesWithProductCount")]
	public async Task<ActionResult<List<ProductCategoryViewModel>>> GetSubCategoriesWithProductCount(string name)
	{
		List<ProductCategoryViewModel> list = new List<ProductCategoryViewModel>();
		CategoryModel mainCategory = _context.Categories.FirstOrDefault((CategoryModel x) => x.Name == name && x.ParentId == null);
		if (mainCategory != null)
		{
			List<CategoryModel> subCategories = await _context.Categories.Where((CategoryModel x) => x.ParentId == mainCategory.Id).ToListAsync();
			if (subCategories.Any())
			{
				List<ProductModel> productsList = await _context.Products.Where((ProductModel x) => subCategories.Select((CategoryModel categoryModel) => categoryModel.Id).ToList().Contains(x.CategoryId)).ToListAsync();
				foreach (CategoryModel subCategory in subCategories)
				{
					list.Add(new ProductCategoryViewModel
					{
						Id = subCategory.Id,
						Name = subCategory.Name,
						ProductCount = (productsList.Where((ProductModel x) => x.CategoryId == subCategory.Id).Any() ? productsList.Where((ProductModel x) => x.CategoryId == subCategory.Id).Count() : 0)
					});
				}
			}
		}
		return list;
	}

	[HttpPost("Post")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<CategoryModel>> Post(AddCategoryModel model)
	{
		CategoryModel category = new CategoryModel
		{
			Id = Guid.NewGuid(),
			Name = model.Name,
			ParentId = model.ParentId,
			IsMenuOnly = model.IsMenuOnly
		};
		_context.Categories.Add(category);
		await _context.SaveChangesAsync();
        _cache.Remove("NAV_CATEGORIES");
        return Ok(model);
	}

	[HttpPut("Update")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<CategoryModel>> Update(CategoryModel model)
	{
		CategoryModel existingCategory = await _context.Categories.FindAsync(model.Id);
		existingCategory.Name = model.Name;
		existingCategory.ParentId = model.ParentId;
		await _context.SaveChangesAsync();
        _cache.Remove("NAV_CATEGORIES");
        return Ok(model);
	}

	[HttpDelete("Delete")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Delete(Guid id)
	{
		CategoryModel CategoryModel = await _context.Categories.FindAsync(id);
		_context.Categories.Remove(CategoryModel);
		await _context.SaveChangesAsync();
        _cache.Remove("NAV_CATEGORIES");
        return NoContent();
	}
}
