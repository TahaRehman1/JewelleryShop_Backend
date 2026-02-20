using JeweleryAppBackend.Enumerations;
using System;

namespace JeweleryAppBackend.Models
{
    public class ProductHighlight
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public ProductHighlightType Type { get; set; }
        public DateTime CreatedOn { get; set; }

        public ProductModel Product { get; set; }
    }
}
