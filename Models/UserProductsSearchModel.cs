using System;
using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class UserProductsSearchModel
{
	public int Skip { get; set; }

	public int Take { get; set; }

	public decimal? StartPrice { get; set; }

	public decimal? EndPrice { get; set; }

	public List<Guid> CategoryIds { get; set; }

	public string Name { get; set; }

	public List<Guid> SpecificationIds { get; set; }

	public string Sort { get; set; }
}
