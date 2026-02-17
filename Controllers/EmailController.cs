using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JeweleryAppBackend.Controllers;

[Route("api/Email")]
[ApiController]
public class EmailController : ControllerBase
{
	private readonly EmailService _emailService;

	private readonly EmailSettings _emailSettings;

	public EmailController(EmailService emailService, IOptions<EmailSettings> emailSettings)
	{
		_emailService = emailService;
		_emailSettings = emailSettings.Value;
	}

	[HttpPost("SendContactEmail")]
	public async Task<IActionResult> SendContactEmail([FromBody] EmailRequest emailRequest)
	{
		string emailBody = "<html><body><h1 style='color:#000;'>Message From User !</h1><p>Sender Name: " + emailRequest.Name + "</p><p>Sender Email: " + emailRequest.From + "</p><p>Subject : " + emailRequest.Subject + "</p><p>Message : " + emailRequest.Message + "</p><footer style='font-size:small; color:gray;'>This is an automated message. Please do not reply.</footer></body></html>";
		await _emailService.SendEmailAsync(emailRequest.From, _emailSettings.From, emailRequest.Subject, emailBody);
		return Ok("Email sent successfully");
	} 
}
