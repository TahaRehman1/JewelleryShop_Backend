using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace JeweleryAppBackend.Services;

public class ProductService
{
	private readonly ApplicationDbContext _context;

	public ProductService(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<List<ProductImageViewModel>> GetProductImages(Guid productId)
	{
		try
		{
			List<ProductImagesModel> imagesData = await _context.ProductImages.ToListAsync();
			if (imagesData.Any())
			{
				IEnumerable<ProductImagesModel> productImages = imagesData.Where((ProductImagesModel x) => x.ProductId == productId);
				if (productImages.Any())
				{
					List<ProductImageViewModel> productViewImages = new List<ProductImageViewModel>();
					foreach (ProductImagesModel productImage in productImages)
					{
						if (File.Exists(productImage.Src))
						{
							string mimeType = "image/png";
							if (productImage.Name.EndsWith(".jpg") || productImage.Name.EndsWith(".jpeg"))
							{
								mimeType = "image/jpeg";
							}
							else if (productImage.Name.EndsWith(".gif"))
							{
								mimeType = "image/gif";
							}
							else if (productImage.Name.EndsWith(".svg"))
							{
								mimeType = "image/svg+xml";
							}
							string base64String = Convert.ToBase64String(await File.ReadAllBytesAsync(productImage.Src));
							string dataUrl = "data:" + mimeType + ";base64," + base64String;
							string zoomedBase64String = Convert.ToBase64String(await File.ReadAllBytesAsync(productImage.ZoomedImageSrc));
							string zoomedDataUrl = "data:" + mimeType + ";base64," + zoomedBase64String;
							productViewImages.Add(new ProductImageViewModel
							{
								Base64 = dataUrl,
								Id = productImage.Id,
								Name = productImage.Name,
								ZoomedBase64 = zoomedDataUrl,
								SpecificationId = productImage.SpecificationId
							});
						}
					}
					return productViewImages;
				}
			}
			return new List<ProductImageViewModel>();
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}
}
