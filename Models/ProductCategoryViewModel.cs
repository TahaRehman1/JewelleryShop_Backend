using System;

namespace JeweleryAppBackend.Models;

public class ProductCategoryViewModel
{
	public Guid Id { get; set; }

	public string Name { get; set; }

	public int ProductCount { get; set; }
}
