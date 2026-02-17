using System;

namespace JeweleryAppBackend.Models;

public class AddProductModel
{
	public Guid Id { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string DetailedDescription { get; set; }

	public decimal Price { get; set; }

	public Guid CategoryId { get; set; }

	public bool IsActive { get; set; }

	public string Code { get; set; }
}
