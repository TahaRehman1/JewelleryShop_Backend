using System;

namespace JeweleryAppBackend.Models;

public class CategoryModel : AddCategoryModel
{
	public Guid Id { get; set; }
    public CategoryModel Parent { get; set; }

}
