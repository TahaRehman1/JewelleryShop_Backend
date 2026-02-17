using System;
using System.Collections.Generic;

namespace JeweleryAppBackend.Models
{
    public class NavCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public List<NavCategoryDto> Children { get; set; } = new();
    }
}
