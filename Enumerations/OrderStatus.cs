namespace JeweleryAppBackend.Enumerations;

public enum OrderStatus
{
	Pending = 1,
	Confirmed,
	Processing,
	Shipped,
	Delivered,
	Cancelled,
	Returned
}
