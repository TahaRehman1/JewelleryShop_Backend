using System;

namespace JeweleryAppBackend.Models;

public class AddProductSpecificationModel
{
	public Guid ProductId { get; set; }

	public Guid SpecificationId { get; set; }

	public decimal Price { get; set; }
}
