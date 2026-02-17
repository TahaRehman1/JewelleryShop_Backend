using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class AddOrderModel : OrderModel
{
	public new string DiscountId { get; set; }

	public AddressModel CustomerAddress { get; set; }

	public new List<AddOrderProductModel> OrderProducts { get; set; }
}
