using System;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Identity;

namespace JeweleryAppBackend.Services;

public class UserService
{
	private readonly UserManager<ApplicationUser> _userManager;

	public UserService(UserManager<ApplicationUser> userManager)
	{
		_userManager = userManager;
	}

	public async Task CreateUserWithPasswordAsync(string username, string password)
	{
		ApplicationUser user = new ApplicationUser
		{
			UserName = username,
			Email = username
		};
		if (!(await _userManager.CreateAsync(user, password)).Succeeded)
		{
			throw new Exception();
		}
	}
}
