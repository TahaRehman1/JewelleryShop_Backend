using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace JeweleryAppBackend.Models;

public class AddProductImagesModel
{
	public Guid ProductId { get; set; }

	public List<IFormFile> Images { get; set; }
}
