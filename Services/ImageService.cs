using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace JeweleryAppBackend.Services
{
    public class ImageService
    { 
        public async Task<IFormFile> ResizeImageAsync(
            IFormFile file,
            int maxSize = 1200,
            bool convertToWebp = true,
            int quality = 75 // ✅ NOW SUPPORTED
        )
        {
            if (file == null || file.Length == 0)
                return null;

            if (!file.ContentType.StartsWith("image/"))
                throw new Exception("Invalid file type.");

            using var image = await Image.LoadAsync(file.OpenReadStream());

            // ✅ Resize (aspect ratio safe)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxSize, maxSize)
            }));

            var outputStream = new MemoryStream();

            if (convertToWebp)
            {
                var encoder = new WebpEncoder
                {
                    Quality = quality // ✅ USED HERE
                };

                await image.SaveAsync(outputStream, encoder);

                return new FormFile(outputStream, 0, outputStream.Length, file.Name,
                    Path.GetFileNameWithoutExtension(file.FileName) + ".webp")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/webp"
                };
            }
            else
            {
                await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat);

                return new FormFile(outputStream, 0, outputStream.Length, file.Name, file.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = file.ContentType
                };
            }
        }
        public string GetBase64Prefix(string name)
        {
            string mimeType = "image/png";
            if (name.EndsWith(".jpg") || name.EndsWith(".jpeg"))
            {
                mimeType = "image/jpeg";
            }
            if (name.EndsWith(".webp"))
            {
                mimeType = "image/webp";
            }
            else if (name.EndsWith(".gif"))
            {
                mimeType = "image/gif";
            }
            else if (name.EndsWith(".svg"))
            {
                mimeType = "image/svg+xml";
            }
            return "data:" + mimeType + ";base64,";
        }
    }
}