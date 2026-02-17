using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Orleans;
using Orleans.Runtime;

namespace JeweleryAppBackend.GrainInterfaces;

public interface IOrderProductGrain : IGrainWithStringKey, IGrain, IAddressable
{
	Task<OrderProductsModel> GetOrderProduct();

	Task SetOrderProduct(OrderProductsModel orderProduct);

	Task DeleteOrderProduct();
}
