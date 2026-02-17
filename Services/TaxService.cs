using System.Collections.Generic;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Tax;

namespace JeweleryAppBackend.Services;

public class TaxService
{
	private readonly IOptions<StripeSettings> _stripeSettings;

	public TaxService(IOptions<StripeSettings> stripeSettings)
	{
		_stripeSettings = stripeSettings;
	}

	public async Task<decimal> CalculateTaxAsync(TaxCalculationRequestModel request)
	{
		StripeConfiguration.ApiKey = _stripeSettings.Value.SecretKey;
		List<CalculationLineItemOptions> lineItems = new List<CalculationLineItemOptions>();
		foreach (TaxProductModel item in request.Products)
		{
			lineItems.Add(new CalculationLineItemOptions
			{
				Amount = (long)(item.Amount * 100m),
				Reference = item.Name
			});
		}
		CalculationCreateOptions options = new CalculationCreateOptions
		{
			Currency = "usd",
			LineItems = lineItems,
			CustomerDetails = new CalculationCustomerDetailsOptions
			{
				Address = new AddressOptions
				{
					Line1 = request.CustomerAddress,
					State = request.State,
					Country = "US"
				}
			}
		};
		CalculationService service = new CalculationService();
		return (await service.CreateAsync(options)).AmountTotal;
	}
}
