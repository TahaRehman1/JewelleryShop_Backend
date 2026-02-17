using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JeweleryAppBackend.Models;

[Table("OrderProducts")]
public class OrderProductsModel
{
	public Guid ProductId { get; set; }

	public int Quantity { get; set; }

	public decimal Price { get; set; }

	public Guid Id { get; set; }

	public Guid OrderId { get; set; }

	public string Specification { get; set; }

	[JsonIgnore]
	public OrderModel Order { get; set; }

	[JsonIgnore]
	public ProductModel Product { get; set; }
}
