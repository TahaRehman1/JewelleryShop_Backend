using JeweleryAppBackend.Controllers;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; 
using Stripe.Climate;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
namespace JeweleryAppBackend.Services
{
    public class InvoiceService
    {
        private readonly EmailService _emailService;
        private readonly OrderService _orderService;
        private readonly InvoiceSettings _invoiceSettings;
        private readonly EmailSettings _emailSettings; 
        private readonly PlaywrightService _playwrightService;
        private readonly ILogger<PaymentController> _logger;
        private readonly ApplicationDbContext _context;
        public InvoiceService(PlaywrightService playwrightService,ApplicationDbContext context,ILogger<PaymentController> logger,IOptions<InvoiceSettings> invoiceSettings, EmailService emailService,OrderService orderService, IOptions<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _orderService = orderService;
            _emailSettings = emailSettings.Value;
            _invoiceSettings = invoiceSettings.Value;
            _logger = logger;
            _context = context;
            _playwrightService = playwrightService;
        }
        public async Task CreateInvoice(Guid orderId)
        {
            var order = await _orderService.GetById(orderId);

            if (order == null)
                throw new Exception("Order not found"); 
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.OrderId == orderId); 
            var invoiceNumber = await GenerateInvoiceNumber();
            if (existingInvoice == null)
            {
                var invoice = new InvoiceModel
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Number = invoiceNumber
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            else
            {
                invoiceNumber = existingInvoice.Number;
            }
            await SendInvoice(order, invoiceNumber);
        }
        public async Task SendInvoice(OrderViewModel order,string invoiceNumber)
        {
            var template = LoadTemplate();
            var html = PopulateTemplate(template, order, invoiceNumber);
            var pdfBytes = await _playwrightService.GeneratePdfAsync(html);
            SaveInvoiceToFolder(pdfBytes, invoiceNumber);
            await _emailService.SendEmailAsync(_emailSettings.From,order.CustomerEmail,"Invoice for Order#"+order.OrderNumber, "Thanks for shopping at The Carats , Here is your Invoice for the Order#" + order.OrderNumber.Replace("Order",""),pdfBytes,"Invoice.pdf");
        }
        private string LoadTemplate()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "InvoiceTemplate.html");
            return System.IO.File.ReadAllText(path);
        }
        private string PopulateTemplate(string html, OrderViewModel order,string invoiceNumber)
        {
            // Replace simple fields
            html = html.Replace("{{InvoiceNumber}}", invoiceNumber.Replace("Invoice", ""));
            html = html.Replace("{{OrderNumber}}", order.OrderNumber.Replace("Order", ""));
            html = html.Replace("{{Date}}", order.DateOfCreation.ToString("dd MMM yyyy"));
            html = html.Replace("{{CustomerName}}", order.CustomerName);
            html = html.Replace("{{CustomerPhone}}", order.CustomerPhone);
            html = html.Replace("{{Total}}", order.Price.ToString());
            html = html.Replace("{{Address}}", order.ShippingAddress.ToString());
            html = html.Replace("{{ShippingAmount}}", order.ShippingAmount.ToString());
            html = html.Replace("{{Price}}", (order.Price - order.ShippingAmount).ToString());
            html = html.Replace("{{TotalPrice}}",(order.Price).ToString());

            // Generate items rows
            string itemsHtml = "";  
            foreach (var item in order.OrderProducts)
            {
                itemsHtml += $@"
        <tr> 
            <td>{item.Product.Name}</td>
            <td>{item.Specification}</td>
            <td>{item.Quantity}</td> 
            <td>{item.Price}</td> 
        </tr>";
            }

            html = html.Replace("{{Items}}", itemsHtml);

            return html;
        } 
        private void SaveInvoiceToFolder(byte[] pdfBytes, string invoiceNumber)
        {
            try
            {
                // Read path from config
                var folderPath = _invoiceSettings.FolderPath;

                // Fallback to current directory if not set
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Invoices");
                }

                // Ensure directory exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    _logger.LogInformation("Created invoice directory at {FolderPath}", folderPath);
                } 
                var filePath = Path.Combine(folderPath, $"{invoiceNumber}.pdf"); 
                File.WriteAllBytes(filePath, pdfBytes); 
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission issue while saving invoice " + invoiceNumber, invoiceNumber);
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Directory not found while saving invoice " + invoiceNumber, invoiceNumber);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "I/O error while saving invoice " + invoiceNumber, invoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while saving invoice "+ invoiceNumber, invoiceNumber);
            }
        }
        public async Task<string> GenerateInvoiceNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            string invoiceNumber;
            bool exists;

            do
            {
                var sb = new StringBuilder("Invoice");

                for (int i = 0; i < 10; i++)
                {
                    sb.Append(chars[random.Next(chars.Length)]);
                }

                invoiceNumber = sb.ToString();

                exists = await _context.Invoices
                    .AnyAsync(i => i.Number == invoiceNumber);

            } while (exists);

            return invoiceNumber;
        }
    }
}
