using System;
using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class MaxPriceRequestModel
{
	public List<Guid> CategoryIds { get; set; }

	public string Name { get; set; }
}
