using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class ProductModel : AddProductModel
{
	public ICollection<ProductSpecificationModel> ProductSpecifications { get; set; }
    public CategoryModel Category { get; set; }
}
