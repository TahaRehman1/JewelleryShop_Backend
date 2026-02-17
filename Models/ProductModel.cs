using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class ProductModel : AddProductModel
{
	public ICollection<ProductSpecificationModel> ProductSpecifications { get; set; }
}
