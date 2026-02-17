using System;

namespace JeweleryAppBackend.Models;

public class AddSpecificationsModel
{
	public Guid CategoryId { get; set; }

	public string Name { get; set; }

	public string Value { get; set; }
}
