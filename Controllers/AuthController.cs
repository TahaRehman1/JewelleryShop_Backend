using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace JeweleryAppBackend.Controllers;

[Route("api/Auth")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly UserManager<ApplicationUser> _userManager;

	private readonly SignInManager<ApplicationUser> _signInManager;

	private readonly IConfiguration _configuration;

	public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
	{
		_userManager = userManager;
		_configuration = configuration;
		_signInManager = signInManager;
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginModel model)
	{
		ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);
		if (user == null)
		{
			return Unauthorized();
		}
		if (!(await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false)).Succeeded)
		{
			return Unauthorized();
		}
		List<Claim> claims = new List<Claim>
		{
			new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Id),
			new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", user.Email)
		};
		IList<string> roles = await _userManager.GetRolesAsync(user);
		foreach (string role in roles)
		{
			claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role));
		}
		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
		byte[] key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
		DateTime expirationTime = DateTime.UtcNow.AddHours(1.0);
		SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = expirationTime,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256")
		};
		SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
		string tokenString = tokenHandler.WriteToken(token);
		return Ok(new
		{
			token = tokenString,
			expiration = expirationTime,
			roles = roles
		});
	}

	[HttpGet("CheckToken")]
	public IActionResult CheckToken()
	{
		string authorizationHeader = base.Request.Headers["Authorization"].FirstOrDefault();
		if (authorizationHeader == null || !authorizationHeader.StartsWith("Bearer "))
		{
			return Unauthorized(new
			{
				Message = "Authorization header is missing or invalid"
			});
		}
		string token = authorizationHeader.Substring("Bearer ".Length).Trim();
		byte[] key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
		JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
		TokenValidationParameters validationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(key),
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidIssuer = _configuration["Jwt:Issuer"],
			ValidAudience = _configuration["Jwt:Audience"]
		};
		try
		{
			SecurityToken validatedToken;
			ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
			return Ok(new
			{
				Message = "Token is valid",
				Claims = principal.Claims.Select((Claim c) => new { c.Type, c.Value }).ToList()
			});
		}
		catch (SecurityTokenExpiredException)
		{
			return Unauthorized(new
			{
				Message = "Token has expired"
			});
		}
		catch (SecurityTokenInvalidSignatureException)
		{
			return Unauthorized(new
			{
				Message = "Invalid token signature"
			});
		}
		catch (Exception)
		{
			return Unauthorized(new
			{
				Message = "Invalid token"
			});
		}
	}
}
