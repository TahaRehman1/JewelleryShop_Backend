using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Orleans;
using Orleans.Runtime;

namespace JeweleryAppBackend.GrainInterfaces;

public interface IOrderGrain : IGrainWithStringKey, IGrain, IAddressable
{
	Task<OrderModel> GetOrder();

	Task SetOrder(OrderModel order);

	Task DeleteOrder();
}
