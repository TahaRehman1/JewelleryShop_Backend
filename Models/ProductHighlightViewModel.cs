using JeweleryAppBackend.Enumerations;
using System;

namespace JeweleryAppBackend.Models
{
    public class ProductHighlightViewModel
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public ProductHighlightType Type { get; set; }
    }
}
