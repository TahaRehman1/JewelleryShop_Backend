using System;
using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class SpecificationsModel : AddSpecificationsModel
{
	public Guid Id { get; set; }

	public ICollection<ProductSpecificationModel> ProductSpecifications { get; set; }
}
