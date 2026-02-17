using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JeweleryAppBackend.Enumerations;

namespace JeweleryAppBackend.Models;

[Table("Orders")]
public class OrderModel
{
	[Key]
	public Guid Id { get; set; }

	public DateTime DateOfCreation { get; set; }

	public string CustomerEmail { get; set; }

	public string CustomerName { get; set; }

	public string CustomerPhone { get; set; }

	public string ShippingAddress { get; set; }

	public decimal ShippingAmount { get; set; }

	public decimal Price { get; set; }

	public Guid? DiscountId { get; set; }

	public OrderStatus OrderStatus { get; set; }

	public string OrderNumber { get; set; }

	public PaymentStatus PaymentStatus { get; set; }

	public ICollection<OrderProductsModel> OrderProducts { get; set; }
}
