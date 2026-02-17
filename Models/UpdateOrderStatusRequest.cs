using JeweleryAppBackend.Enumerations;
using System;

namespace JeweleryAppBackend.Models
{
    public class UpdateOrderStatusRequest
    {
        public Guid OrderId { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }
}
