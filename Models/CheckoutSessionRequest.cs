using System;

namespace JeweleryAppBackend.Models;

public class CheckoutSessionRequest
{
	public string SuccessUrl { get; set; }

	public string CancelUrl { get; set; }

	public Guid OrderId { get; set; }
}
