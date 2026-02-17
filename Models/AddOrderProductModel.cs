using System;
using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class AddOrderProductModel
{
	public Guid Id { get; set; }

	public Guid OrderId { get; set; }

	public Guid ProductId { get; set; }

	public int Quantity { get; set; }

	public decimal Price { get; set; }

	public List<SpecificationsModel> Specifications { get; set; }
}
