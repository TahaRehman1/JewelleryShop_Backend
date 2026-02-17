using System.Collections.Generic;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Tax;

namespace JeweleryAppBackend.Controllers;

[Route("Tax")]
[ApiController]
public class TaxController : Controller
{
	private readonly string _stripeSecretKey;

	private readonly ILogger<TaxController> _logger;

	private readonly ApplicationDbContext _context;

	public TaxController(ILogger<TaxController> logger, IOptions<StripeSettings> stripeSettings, ApplicationDbContext context)
	{
		_stripeSecretKey = stripeSettings.Value.SecretKey;
		StripeConfiguration.ApiKey = _stripeSecretKey;
		_logger = logger;
		_context = context;
	}

	[AllowAnonymous]
	[HttpGet("CalculateTax")]
	public async Task<IActionResult> CalculateTax()
	{
		CalculationCreateOptions options = new CalculationCreateOptions
		{
			Currency = "usd",
			LineItems = new List<CalculationLineItemOptions>
			{
				new CalculationLineItemOptions
				{
					Amount = 15000L,
					Quantity = 3L,
					Reference = "Clothing",
					TaxCode = "txcd_30011000"
				}
			},
			ShippingCost = new CalculationShippingCostOptions
			{
				Amount = 500L
			},
			CustomerDetails = new CalculationCustomerDetailsOptions
			{
				Address = new AddressOptions
				{
					State = "NY",
					PostalCode = "10001",
					Country = "US"
				},
				AddressSource = "shipping"
			}
		};
		CalculationService service = new CalculationService();
		Calculation calculation = service.Create(options);
		return Ok(calculation);
	}
}
