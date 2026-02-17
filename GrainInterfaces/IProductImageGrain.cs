using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Orleans;
using Orleans.Runtime;

namespace JeweleryAppBackend.GrainInterfaces;

public interface IProductImageGrain : IGrainWithStringKey, IGrain, IAddressable
{
	Task<ProductImagesModel> GetImage();

	Task SetImage(ProductImagesModel image);

	Task DeleteImage();
}
