using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JeweleryAppBackend.Controllers;

[Route("api/User")]
[ApiController]
public class UserController : ControllerBase
{
	private readonly UserService _userService;

	public UserController(UserService userService)
	{
		_userService = userService;
	}

	[HttpPost("CreateUser")]
	public async Task<IActionResult> CreateUser([FromBody] AddNewUserModel request)
	{
		try
		{
			if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
			{
				return BadRequest("Invalid user data.");
			}
			await _userService.CreateUserWithPasswordAsync(request.Username, request.Password);
			return Ok("User created successfully.");
		}
		catch (Exception)
		{
			return BadRequest("User creation failed.");
		}
	}

	[Authorize(Roles = "Admin")]
	[HttpGet("GenerateRandomKey")]
	public async Task<IActionResult> GenerateRandomKey(int length = 32)
	{
		using RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
		byte[] randomBytes = new byte[length];
		rng.GetBytes(randomBytes);
		return Ok(Convert.ToBase64String(randomBytes));
	}
}
