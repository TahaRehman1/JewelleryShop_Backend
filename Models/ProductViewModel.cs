using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class ProductViewModel : AddProductModel
{
	public List<ProductImageViewModel> Images { get; set; }

	public CategoryModel Category { get; set; }

	public List<ProductSpecificationViewModel> Specifications { get; set; }
}
