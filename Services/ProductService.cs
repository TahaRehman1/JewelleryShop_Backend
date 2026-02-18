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
    private readonly ImageService _imageService;

    public ProductService(ApplicationDbContext context, ImageService imageService)
	{
		_context = context;
		_imageService = imageService;
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
							var base64Prefix = _imageService.GetBase64Prefix(productImage.Name); 
                            string base64String = Convert.ToBase64String(await File.ReadAllBytesAsync(productImage.Src));
							string dataUrl = base64Prefix + base64String;
							string zoomedBase64String = Convert.ToBase64String(await File.ReadAllBytesAsync(productImage.ZoomedImageSrc));
							string zoomedDataUrl = base64Prefix + zoomedBase64String;
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
