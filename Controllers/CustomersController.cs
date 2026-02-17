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
public class CustomersController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public CustomersController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<CustomerModel>>> GetAll(int skip, int take)
	{
		return await _context.Customers.Skip(skip).Take(take).ToListAsync();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.Customers.ToListAsync()).Count();
	}
}
