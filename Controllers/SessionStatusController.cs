using System;
using System.Threading.Tasks;
using JeweleryAppBackend.Enumerations;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace JeweleryAppBackend.Controllers;

[Route("session-status")]
[ApiController]
public class SessionStatusController : Controller
{
	private readonly string _stripeSecretKey;

	private readonly ILogger<SessionStatusController> _logger;

	private readonly ApplicationDbContext _context;

    private readonly Services.InvoiceService _invoiceService;

    public SessionStatusController(Services.InvoiceService invoiceService,ILogger<SessionStatusController> logger, IOptions<StripeSettings> stripeSettings, ApplicationDbContext context)
	{
		_stripeSecretKey = stripeSettings.Value.SecretKey;
		StripeConfiguration.ApiKey = _stripeSecretKey;
		_invoiceService = invoiceService;
		_logger = logger;
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> SessionStatus([FromQuery] string sessionId, [FromQuery] Guid orderId)
	{
		SessionService sessionService = new SessionService();
		Session session = sessionService.Get(sessionId);
		OrderModel order = await _context.Orders.FindAsync(orderId);
		if (order != null)
		{
			if (session.Status == "complete" && session.PaymentStatus == "paid")
			{
				order.OrderStatus = OrderStatus.Confirmed;
			}
			if (session.PaymentStatus == "paid")
			{
				order.PaymentStatus = PaymentStatus.Paid;
				if (order.DiscountId.HasValue)
				{
					DiscountModel existingDiscount = await _context.Discounts.FindAsync(order.DiscountId);
					if (existingDiscount != null)
					{
						existingDiscount.TimesRedeemed++;
					}
				}
				await _invoiceService.CreateInvoice(order.Id);
			}
		}
		await _context.SaveChangesAsync();
		return Ok(new
		{
			status = session.Status,
			customer_email = session.CustomerDetails.Email
		});
	}
}
