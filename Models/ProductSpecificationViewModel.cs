using System;

namespace JeweleryAppBackend.Models;

public class ProductSpecificationViewModel
{
	public Guid Id { get; set; }

	public Guid ProductId { get; set; }

	public string Name { get; set; }

	public string Value { get; set; }

	public decimal Price { get; set; }
}
