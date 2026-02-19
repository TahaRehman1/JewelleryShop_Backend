using System.Collections.Generic;

namespace JeweleryAppBackend.Models;

public class OrderViewModel : OrderModel
{
	public new List<OrderProductViewModel> OrderProducts { get; set; }
    public DiscountModel Discount { get; set; }
}
