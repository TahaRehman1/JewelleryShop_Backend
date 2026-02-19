using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeweleryAppBackend.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context; 

        public OrderService(ApplicationDbContext context)
        {
            _context = context; 
        }
        public async Task<OrderViewModel> GetById(Guid id)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
                return new OrderViewModel();

            // ✅ Fetch order products
            var orderProductsViewList = await _context.OrderProducts
                .Where(op => op.OrderId == id)
                .Select(op => new OrderProductViewModel
                {
                    Product = op.Product,
                    Quantity = op.Quantity,
                    Price = op.Price,
                    Specification = op.Specification
                })
                .ToListAsync();

            // ✅ Fetch discount if exists
            DiscountModel? discount = null;

            if (order.DiscountId != null)
            {
                discount = await _context.Discounts
                    .Where(d => d.Id == order.DiscountId)
                    .Select(d => new DiscountModel
                    {
                        Id = d.Id,
                        Code = d.Code,
                        Percentage = d.Percentage
                    })
                    .FirstOrDefaultAsync();
            }

            // ✅ Return full view model
            return new OrderViewModel
            {
                Id = order.Id,
                DateOfCreation = order.DateOfCreation,
                OrderNumber = order.OrderNumber,
                OrderStatus = order.OrderStatus,
                CustomerEmail = order.CustomerEmail,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                ShippingAddress = order.ShippingAddress,
                ShippingAmount = order.ShippingAmount,
                Price = order.Price,
                PaymentStatus = order.PaymentStatus,
                DiscountId = order.DiscountId,
                Discount = discount,
                OrderProducts = orderProductsViewList
            };
        }
        public async Task<string> GenerateOrderNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            string orderNumber;
            bool exists;

            do
            {
                var sb = new StringBuilder("Order");

                for (int i = 0; i < 10; i++)
                {
                    sb.Append(chars[random.Next(chars.Length)]);
                }

                orderNumber = sb.ToString();

                exists = await _context.Orders
                    .AnyAsync(o => o.OrderNumber == orderNumber);

            } while (exists);

            return orderNumber;
        }
    }
}
