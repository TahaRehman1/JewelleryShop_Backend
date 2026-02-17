using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Orleans;
using Orleans.Runtime;

namespace JeweleryAppBackend.GrainInterfaces;

public interface IProductGrain : IGrainWithStringKey, IGrain, IAddressable
{
	Task<ProductModel> GetProduct();

	Task SetProduct(ProductModel product);

	Task DeleteProduct();
}
