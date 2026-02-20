using System;

namespace JeweleryAppBackend.Models
{
    public class CreateProductHighlightModel
    {
        public Guid ProductId { get; set; }
        public int Type { get; set; } // ✅ MUST BE INT
    }
}
