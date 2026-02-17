using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JeweleryAppBackend.Enumerations;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace JeweleryAppBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DiscountsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public DiscountsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<DiscountModel>>> GetAll(int skip, int take)
	{
		return await _context.Discounts.Skip(skip).Take(take).ToListAsync();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.Discounts.ToListAsync()).Count();
	}

	[HttpGet("GetById")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<DiscountModel>> GetById(Guid id)
	{
		DiscountModel discount = await _context.Discounts.FindAsync(id);
		if (discount == null)
		{
			return NotFound();
		}
		return discount;
	}

	[HttpGet("GetByCode")]
	public async Task<ActionResult<DiscountModel>> GetByCode(string code)
	{
		DiscountModel discount = await _context.Discounts.FirstOrDefaultAsync((DiscountModel d) => d.Code == code);
		if (discount == null)
		{
			return NotFound();
		}
		if (discount.RedemptionLimit == discount.TimesRedeemed)
		{
			return Conflict();
		}
		return discount;
	}

	[HttpPost("Post")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<DiscountModel>> Post(AddDiscountModel model)
	{
		DiscountStatus status = (DiscountStatus)Enum.Parse(typeof(DiscountStatus), model.Status, ignoreCase: true);
		DiscountModel discount = new DiscountModel
		{
			Id = Guid.NewGuid(),
			Code = model.Code,
			Percentage = model.Percentage,
			Status = status,
			RedemptionLimit = model.RedemptionLimit
		};
		_context.Discounts.Add(discount);
		await _context.SaveChangesAsync();
		return Ok(discount);
	}

	[HttpPut("Update")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Update(AddDiscountModel model)
	{
		DiscountStatus status = (DiscountStatus)Enum.Parse(typeof(DiscountStatus), model.Status, ignoreCase: true);
		DiscountModel existingDiscount = _context.Discounts.Find(model.Id);
		existingDiscount.Code = model.Code;
		existingDiscount.Percentage = model.Percentage;
		existingDiscount.Status = status;
		existingDiscount.RedemptionLimit = model.RedemptionLimit;
		await _context.SaveChangesAsync();
		return Ok();
	}

	[HttpDelete("Delete")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Delete(Guid id)
	{
		DiscountModel discount = await _context.Discounts.FindAsync(id);
		if (discount == null)
		{
			return NotFound();
		}
		_context.Discounts.Remove(discount);
		await _context.SaveChangesAsync();
		return NoContent();
	}
}
