using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JeweleryAppBackend.Models;

[Table("ProductSpecifications")]
public class ProductSpecificationModel : AddProductSpecificationModel
{
	public Guid Id { get; set; }

	[JsonIgnore]
	public SpecificationsModel Specification { get; set; }

	[JsonIgnore]
	public ProductModel Product { get; set; }
}
