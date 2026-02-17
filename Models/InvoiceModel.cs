using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JeweleryAppBackend.Models
{
    [Table("Invoices")]
    public class InvoiceModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Number { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        public OrderModel Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
