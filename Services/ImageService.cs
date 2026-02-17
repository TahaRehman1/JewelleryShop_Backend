using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace JeweleryAppBackend.Services;

public class ImageService
{
	public async Task<IFormFile> ResizeImageAsync(IFormFile file)
	{
		if (file == null || file.Length == 0L)
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		await file.CopyToAsync(memoryStream);
		byte[] imageBytes = memoryStream.ToArray();
		using MemoryStream inputStream = new MemoryStream(imageBytes);
		using Image originalImage = Image.FromStream(inputStream);
		Bitmap resizedImage = new Bitmap(1200, 1200);
		using (Graphics graphics = Graphics.FromImage(resizedImage))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(originalImage, 0, 0, 1200, 1200);
		}
		MemoryStream resizedImageStream = new MemoryStream();
		ImageFormat format = GetImageFormat(file.FileName);
		resizedImage.Save(resizedImageStream, format);
		resizedImageStream.Position = 0L;
		string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
		string newFileName = originalFileNameWithoutExtension + "-zoomed" + Path.GetExtension(file.FileName);
		return new FormFile(resizedImageStream, 0L, resizedImageStream.Length, newFileName, newFileName)
		{
			Headers = new HeaderDictionary(),
			ContentType = GetContentType(format)
		};
	}

	private ImageFormat GetImageFormat(string fileName)
	{
		switch (Path.GetExtension(fileName).ToLowerInvariant())
		{
		case ".jpg":
		case ".jpeg":
			return ImageFormat.Jpeg;
		case ".png":
			return ImageFormat.Png;
		case ".gif":
			return ImageFormat.Gif;
		case ".bmp":
			return ImageFormat.Bmp;
		case ".tiff":
			return ImageFormat.Tiff;
		default:
			return ImageFormat.Jpeg;
		}
	}

	private string GetContentType(ImageFormat format)
	{
		if (format == ImageFormat.Jpeg)
		{
			return "image/jpeg";
		}
		if (format == ImageFormat.Png)
		{
			return "image/png";
		}
		if (format == ImageFormat.Gif)
		{
			return "image/gif";
		}
		if (format == ImageFormat.Bmp)
		{
			return "image/bmp";
		}
		if (format == ImageFormat.Tiff)
		{
			return "image/tiff";
		}
		return "application/octet-stream";
	}
}
