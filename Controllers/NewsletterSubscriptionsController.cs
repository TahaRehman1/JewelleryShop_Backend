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
public class NewsletterSubscriptionsController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public NewsletterSubscriptionsController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<NewsletterSubscriptionModel>>> GetAll(int skip, int take)
	{
		return await _context.NewsletterSubscriptions.Skip(skip).Take(take).ToListAsync();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.NewsletterSubscriptions.ToListAsync()).Count;
	}

	[HttpGet("AddNewsletterSubscription")]
	public async Task<ActionResult> AddNewsletterSubscription(string email)
	{
		if (!(await _context.NewsletterSubscriptions.Where((NewsletterSubscriptionModel x) => x.Email == email).ToListAsync()).Any())
		{
			NewsletterSubscriptionModel newsletter = new NewsletterSubscriptionModel
			{
				Id = Guid.NewGuid(),
				Email = email
			};
			_context.NewsletterSubscriptions.Add(newsletter);
			await _context.SaveChangesAsync();
			return Ok();
		}
		return Conflict();
	}
}
