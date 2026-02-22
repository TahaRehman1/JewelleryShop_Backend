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
public class BannersController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public BannersController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<BannerModel>>> GetAll(int skip, int take)
	{
		return await _context.Banners.Skip(skip).Take(take).ToListAsync();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.Banners.ToListAsync()).Count;
	}

	[HttpGet("GetAllActive")]
	public async Task<ActionResult<List<BannerModel>>> GetAllActive()
	{
		return await _context.Banners.Where((BannerModel x) => x.IsActive).ToListAsync();
	}

	[HttpGet("GetById")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<BannerModel>> GetById(Guid id)
	{
		return Ok(await _context.Banners.FindAsync(id));
	}

	[HttpPost("Post")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<BannerModel>> Post(AddBannerModel model)
	{
		BannerModel banner = new BannerModel
		{
			Id = Guid.NewGuid(),
			Body = model.Body,
			IsActive = model.IsActive
		};
		_context.Banners.Add(banner);
		await _context.SaveChangesAsync();
		return Ok(banner);
	}

	[HttpPut("Update")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<BannerModel>> Update(BannerModel banner)
	{
		if (banner.Id != banner.Id)
		{
			return BadRequest();
		}
		BannerModel existingBanner = await _context.Banners.FindAsync(banner.Id);
		if (existingBanner == null)
		{
			return NotFound();
		}
		existingBanner.Body = banner.Body;
		existingBanner.IsActive = banner.IsActive;
		await _context.SaveChangesAsync();
		return Ok(banner);
	}

	[HttpDelete("Delete")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult> Delete(Guid id)
	{
		BannerModel banner = await _context.Banners.FindAsync(id);
		if (banner == null)
		{
			return NotFound();
		}
		_context.Banners.Remove(banner);
		await _context.SaveChangesAsync();
		return Ok();
	}
}
