using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JeweleryAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly InvoiceSettings _invoiceSettings;

        public InvoicesController(ApplicationDbContext context, IOptions<InvoiceSettings> invoiceSettings)
        {
            _context = context;
            _invoiceSettings = invoiceSettings.Value;
        } 
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(
            int skip = 0,
            int take = 10,
            string? orderNumber = null)
        {
            var query = _context.Invoices
                .Include(i => i.Order)
                .AsQueryable();

            // Optional filter
            if (!string.IsNullOrEmpty(orderNumber))
            {
                query = query.Where(i => i.Order.OrderNumber == orderNumber);
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(i => new
                {
                    i.Id,
                    i.Number,
                    i.CreatedAt,
                    Order = new
                    {
                        i.Order.Id,
                        i.Order.OrderNumber,
                        i.Order.CustomerName,
                        i.Order.CustomerEmail
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                skip,
                take,
                data
            });
        } 
        [HttpGet("downloadpdf")]
        public async Task<IActionResult> DownloadPdf(Guid invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Order)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return NotFound("Invoice not found");
                var folderPath = _invoiceSettings.FolderPath;
                // 👉 Assuming PDF is saved on disk
                var filePath = Path.Combine(folderPath, $"{invoice.Number}.pdf");

                if (!System.IO.File.Exists(filePath))
                    return NotFound("PDF file not found");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                var base64 = Convert.ToBase64String(fileBytes);

                return Ok(new
                {
                    fileName = $"{invoice.Number}.pdf",
                    base64
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error generating PDF response",
                    error = ex.Message
                });
            }
        }
    }
}