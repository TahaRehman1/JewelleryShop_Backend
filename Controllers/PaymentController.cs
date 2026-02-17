using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace JeweleryAppBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class PaymentController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly string _stripeSecretKey;

	private readonly ILogger<PaymentController> _logger;

	private readonly JeweleryAppBackend.Services.TaxService _taxService;

	private readonly ShippingFeeSettings _shippingFeeSettings;

	public PaymentController(ILogger<PaymentController> logger, IOptions<ShippingFeeSettings> shippingFeeSettings, IOptions<StripeSettings> stripeSettings, ApplicationDbContext context, JeweleryAppBackend.Services.TaxService taxService)
	{
		_stripeSecretKey = stripeSettings.Value.SecretKey;
		StripeConfiguration.ApiKey = _stripeSecretKey;
		_logger = logger;
		_context = context;
		_taxService = taxService;
		_shippingFeeSettings = shippingFeeSettings.Value;
	}

	[AllowAnonymous]
	[HttpPost("Checkout")]
	public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
	{
		try
		{
			List<SessionShippingOptionOptions> shippingOptions = new List<SessionShippingOptionOptions>();
			new DiscountModel().Percentage = 0m;
			List<SessionLineItemOptions> lineItems = new List<SessionLineItemOptions>();
			List<OrderProductsModel> orderProducts = await (from op in _context.OrderProducts.Include((OrderProductsModel op) => op.Product)
				where op.OrderId == request.OrderId
				select op).ToListAsync();
			OrderModel order = await _context.Orders.FindAsync(request.OrderId);
			shippingOptions.Add(new SessionShippingOptionOptions
			{
				ShippingRateData = new SessionShippingOptionShippingRateDataOptions
				{
					Type = "fixed_amount",
					FixedAmount = new SessionShippingOptionShippingRateDataFixedAmountOptions
					{
						Amount = Convert.ToInt64(order.ShippingAmount * 100m),
						Currency = "usd"
					},
					DisplayName = ((order.ShippingAmount == 0m) ? "Free shipping" : ((order.ShippingAmount == (decimal)_shippingFeeSettings.Expedite) ? "Expedite shipping" : "Standard shipping"))
				}
			});
			foreach (OrderProductsModel item in orderProducts)
			{
				lineItems.Add(new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						Currency = "usd",
						ProductData = ((!string.IsNullOrEmpty(item.Specification)) ? new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.Product.Name,
							Description = ((!string.IsNullOrEmpty(item.Specification)) ? item.Specification : "")
						} : new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.Product.Name
						}),
						UnitAmount = Convert.ToInt64(item.Price * 100m)
					},
					Quantity = item.Quantity
				});
			}
			SessionCreateOptions options = new SessionCreateOptions
			{
				PaymentMethodTypes = new List<string> { "card" },
				LineItems = lineItems,
				Mode = "payment",
				UiMode = "embedded",
				ReturnUrl = request.SuccessUrl + "/{CHECKOUT_SESSION_ID}/" + orderProducts[0].OrderId,
				ShippingOptions = shippingOptions
			};
			if (order.DiscountId.HasValue)
			{
				DiscountModel discount = await _context.Discounts.FindAsync(order.DiscountId);
				if (discount != null)
				{
					CouponCreateOptions couponOptions = new CouponCreateOptions
					{
						Name = discount.Code,
						PercentOff = discount.Percentage,
						Duration = "forever"
					};
					CouponService couponService = new CouponService();
					Coupon coupon = couponService.Create(couponOptions);
					options.Discounts = new List<SessionDiscountOptions>
					{
						new SessionDiscountOptions
						{
							Coupon = coupon.Id
						}
					};
				}
			}
			SessionService service = new SessionService();
			return Ok(new
			{
				clientSecret = (await service.CreateAsync(options)).ClientSecret
			});
		}
		catch (Exception)
		{
			throw new Exception();
		}
	}

	[HttpPost("GetTaxAmount")]
	public async Task<ActionResult<decimal>> GetTaxAmount([FromBody] TaxCalculationRequestModel request)
	{
		return Ok(await _taxService.CalculateTaxAsync(request));
	}
}
