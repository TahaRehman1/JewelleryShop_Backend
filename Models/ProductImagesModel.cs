using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JeweleryAppBackend.Models;

[Table("ProductImages")]
public class ProductImagesModel
{
	public Guid Id { get; set; }

	public string Src { get; set; }

	public string ZoomedImageSrc { get; set; }

	public Guid ProductId { get; set; }

	public bool IsTitleImage { get; set; }

	public string Name { get; set; }

	public Guid? SpecificationId { get; set; }
}
