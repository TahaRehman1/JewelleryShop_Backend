using System;
using JeweleryAppBackend.Enumerations;

namespace JeweleryAppBackend.Models;

public class DiscountModel
{
	public Guid Id { get; set; }

	public decimal Percentage { get; set; }

	public int RedemptionLimit { get; set; }

	public int TimesRedeemed { get; set; }

	public DiscountStatus Status { get; set; }

	public string Code { get; set; }
}
