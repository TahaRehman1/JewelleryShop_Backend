using System;

namespace JeweleryAppBackend.Models;

public class CategoryModel : AddCategoryModel
{
	public Guid Id { get; set; }

	public Guid? ParentId { get; set; }
}
