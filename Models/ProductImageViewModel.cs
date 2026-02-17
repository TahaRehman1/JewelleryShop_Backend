using System;

namespace JeweleryAppBackend.Models;

public class ProductImageViewModel
{
	public Guid Id { get; set; }

	public string Base64 { get; set; }

	public string ZoomedBase64 { get; set; }

	public string Name { get; set; }

	public Guid? SpecificationId { get; set; }
}
