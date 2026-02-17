using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class TaxCalculationRequestModel
{
	public string CustomerAddress { get; set; }

	public List<TaxProductModel> Products { get; set; }

	public string State { get; set; }
}
