using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace JeweleryAppBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public CategoriesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("GetAllMain")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<CategoryModel>>> GetAllMain()
	{
		return await _context.Categories.Where((CategoryModel x) => x.ParentId == null).ToListAsync();
	}

	[HttpGet("GetAllNavCategories")]
	public async Task<ActionResult<List<CategoryModel>>> GetAllNavCategories()
	{
		List<Guid> categoryids = await (from x in _context.Products
			where x.IsActive
			select x.CategoryId).Distinct().ToListAsync();
		return await _context.Categories.Where((CategoryModel p) => categoryids.Contains(p.Id)).ToListAsync();
	}

	[HttpGet("GetSubCategories")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<CategoryModel>>> GetSubCategories(Guid id)
	{
		return await _context.Categories.Where((CategoryModel x) => x.ParentId == id).ToListAsync();
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
				ParentCategory = ((c.ParentId != null) ? _context.Categories.FirstOrDefault((CategoryModel pc) => pc.Id == c.ParentId) : new CategoryModel())
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
			Name = model.Name
		};
		_context.Categories.Add(category);
		await _context.SaveChangesAsync();
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
		return Ok(model);
	}

	[HttpDelete("Delete")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Delete(Guid id)
	{
		CategoryModel CategoryModel = await _context.Categories.FindAsync(id);
		_context.Categories.Remove(CategoryModel);
		await _context.SaveChangesAsync();
		return NoContent();
	}
}
