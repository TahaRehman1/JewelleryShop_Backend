namespace JeweleryAppBackend.Models;

public class OrderProductViewModel
{
	public ProductModel Product { get; set; }

	public decimal Price { get; set; } 
    public string Specification { get; set; }
    public int Quantity { get; set; }
}
