using JeweleryAppBackend.Enumerations;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Threading.Tasks;

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

        var order = await _context.Orders.FindAsync(orderId);

        if (order == null)
            return NotFound();

        if (order.PaymentStatus != PaymentStatus.Paid && session.PaymentStatus == "paid")
        {
            order.PaymentStatus = PaymentStatus.Paid;

            if (session.Status == "complete")
            {
                order.OrderStatus = OrderStatus.Confirmed;
            }

            // ✅ Safe discount handling
            if (order.DiscountId.HasValue && !order.DiscountApplied)
            {
                var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE Discounts
                SET TimesRedeemed = TimesRedeemed + 1
                WHERE Id = {order.DiscountId}
                AND Status = {(int)DiscountStatus.Active}
                AND (RedemptionLimit = 0 OR TimesRedeemed < RedemptionLimit)
            ");

                if (rowsAffected > 0)
                {
                    order.DiscountApplied = true;
                }
                else
                { 
                    _logger.LogWarning($"Discount {order.DiscountId} was already fully redeemed but used in order {order.Id}");
                }
            }

            await _invoiceService.CreateInvoice(order.Id);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            status = session.Status,
            customer_email = session.CustomerDetails?.Email
        });
    }
}
