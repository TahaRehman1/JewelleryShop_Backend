using System;

namespace JeweleryAppBackend.Models;

public class AddCategoryModel
{
	public string Name { get; set; }

    public bool IsMenuOnly { get; set; }
    public Guid? ParentId { get; set; }
}
