using System;

namespace JeweleryAppBackend.Models;

public class UpdateImageSpecificationsModel
{
	public Guid ImageId { get; set; }

	public Guid SpecificationId { get; set; }
}
