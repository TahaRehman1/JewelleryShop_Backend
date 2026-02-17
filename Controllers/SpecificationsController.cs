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
public class SpecificationsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public SpecificationsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<SpecificationsViewModel>>> GetAll(int skip, int take)
	{
		List<SpecificationsViewModel> list = new List<SpecificationsViewModel>();
		List<SpecificationsModel> data = await _context.Specifications.Skip(skip).Take(take).ToListAsync();
		if (data.Any())
		{
			List<Guid> categoryIds = data.Select((SpecificationsModel spec) => spec.CategoryId).Distinct().ToList();
			List<CategoryModel> categories = await _context.Categories.Where((CategoryModel category) => categoryIds.Contains(category.Id)).ToListAsync();
			foreach (SpecificationsModel specification in data)
			{
				list.Add(new SpecificationsViewModel
				{
					Id = specification.Id,
					Name = specification.Name,
					Value = specification.Value,
					Category = categories.FirstOrDefault((CategoryModel x) => x.Id == specification.CategoryId)
				});
			}
		}
		return list;
	}

	[HttpGet("GetById")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<SpecificationsViewModel>> GetById(Guid id)
	{
		new List<SpecificationsViewModel>();
		SpecificationsModel specification = await _context.Specifications.FindAsync(id);
		CategoryModel category = await _context.Categories.FirstOrDefaultAsync((CategoryModel x) => x.Id == specification.CategoryId);
		return Ok(new SpecificationsViewModel
		{
			Id = specification.Id,
			Name = specification.Name,
			Value = specification.Value,
			Category = category
		});
	}

	[HttpGet("GetByCategory")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<SpecificationsModel>> GetByCategory(Guid id)
	{
		return Ok(await _context.Specifications.Where((SpecificationsModel x) => x.CategoryId == id).ToListAsync());
	}

	[HttpGet("GetFilterByCategory")]
	public async Task<ActionResult<SpecificationsModel>> GetFilterByCategory(Guid categoryId)
	{
		return Ok(await (from spec in _context.Specifications
			where spec.CategoryId == categoryId
			join prodSpec in _context.ProductSpecifications on spec.Id equals prodSpec.SpecificationId
			select spec).ToListAsync());
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.Specifications.ToListAsync()).Count;
	}

	[HttpPost("Post")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<SpecificationsModel>> Post(AddSpecificationsModel model)
	{
		SpecificationsModel Specification = new SpecificationsModel
		{
			Id = Guid.NewGuid(),
			Name = model.Name,
			Value = model.Value,
			CategoryId = model.CategoryId
		};
		_context.Specifications.Add(Specification);
		await _context.SaveChangesAsync();
		return Ok(Specification);
	}

	[HttpPost("AddProductSpecifications")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ProductSpecificationModel>> AddProductSpecifications(AddProductSpecificationModel model)
	{
		if (!(await _context.ProductSpecifications.AnyAsync((ProductSpecificationModel x) => x.ProductId == model.ProductId && x.SpecificationId == model.SpecificationId)))
		{
			ProductSpecificationModel specification = new ProductSpecificationModel
			{
				Id = Guid.NewGuid(),
				ProductId = model.ProductId,
				SpecificationId = model.SpecificationId,
				Price = model.Price
			};
			_context.ProductSpecifications.Add(specification);
			await _context.SaveChangesAsync();
			return Ok(specification);
		}
		return Conflict();
	}

	[HttpGet("GetAllProductSpecifications")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<ProductSpecificationViewModel>>> GetAllProductSpecifications(Guid id)
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
					Id = productSpecifications.FirstOrDefault((ProductSpecificationModel x) => x.SpecificationId == item.Id).Id,
					Price = productSpecifications.FirstOrDefault((ProductSpecificationModel x) => x.SpecificationId == item.Id).Price
				});
			}
		}
		return Ok(list);
	}

	[HttpDelete("DeleteProductSpecifications")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ProductSpecificationViewModel>> DeleteProductSpecifications(Guid id)
	{
		ProductSpecificationModel productSpecification = await _context.ProductSpecifications.FindAsync(id);
		_context.ProductSpecifications.Remove(productSpecification);
		await _context.SaveChangesAsync();
		return Ok();
	}

	[HttpPut("UpdateProductSpecifications")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<ProductSpecificationViewModel>> UpdateProductSpecifications(UpdateProductSpecificationViewModel model)
	{
		(await _context.ProductSpecifications.FindAsync(model.Id)).Price = model.Price;
		await _context.SaveChangesAsync();
		return Ok();
	}

	[HttpPut("Update")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<SpecificationsModel>> Update(SpecificationsModel model)
	{
		if (model.Id != model.Id)
		{
			return BadRequest();
		}
		SpecificationsModel existingSpecification = await _context.Specifications.FindAsync(model.Id);
		if (existingSpecification == null)
		{
			return NotFound();
		}
		existingSpecification.CategoryId = model.CategoryId;
		existingSpecification.Name = model.Name;
		existingSpecification.Value = model.Value;
		await _context.SaveChangesAsync();
		return Ok(model);
	}
}
