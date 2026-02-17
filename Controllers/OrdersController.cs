using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JeweleryAppBackend.Enumerations;
using JeweleryAppBackend.Models;
using JeweleryAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JeweleryAppBackend.Controllers;

[Route("api/Orders")]
[ApiController]
public class OrdersController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	private readonly ShippingFeeSettings _shippingFeeSettings;
    private readonly InvoiceService _invoiceService;
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService,InvoiceService invoiceService,ApplicationDbContext context, IOptions<ShippingFeeSettings> shippingFeeSettings)
	{
		_context = context;
		_shippingFeeSettings = shippingFeeSettings.Value;
		_invoiceService = invoiceService;
		_orderService = orderService;

    }

	[HttpGet("GetAll")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<List<OrderModel>>> GetAll(int skip, int take)
	{
		return await _context.Orders.Skip(skip).Take(take).ToListAsync();
	}

	[HttpGet("GetCount")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<int>> GetCount()
	{
		return (await _context.Orders.ToListAsync()).Count;
	}

	[HttpGet("GetById")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<OrderViewModel>> GetById(Guid id)
	{
		OrderModel order = await _context.Orders.FindAsync(id);
		List<OrderProductsModel> orderProducts = await (from op in _context.OrderProducts.Include((OrderProductsModel op) => op.Product)
			where op.OrderId == id
			select op).ToListAsync();
		List<OrderProductViewModel> orderProductsViewList = new List<OrderProductViewModel>();
		foreach (OrderProductsModel item in orderProducts)
		{
			orderProductsViewList.Add(new OrderProductViewModel
			{
				Product = item.Product,
				Quantity = item.Quantity,
				Price = item.Price,
				Specification = item.Specification
			});
		}
		if (order != null)
		{
			OrderViewModel orderView = new OrderViewModel
			{
				Id = order.Id,
				DateOfCreation = order.DateOfCreation,
				OrderStatus = order.OrderStatus,
				CustomerEmail = order.CustomerEmail,
				CustomerName = order.CustomerName,
				CustomerPhone = order.CustomerPhone,
				ShippingAddress = order.ShippingAddress,
				Price = order.Price,
				PaymentStatus = order.PaymentStatus,
				OrderProducts = orderProductsViewList
			};
			return Ok(orderView);
		}
		return NotFound();
	}

	[HttpPost("Post")]
	public async Task<ActionResult<OrderModel>> PostOrder(AddOrderModel model)
	{
		List<Guid> specificationIds = (from spec in model.OrderProducts.SelectMany((AddOrderProductModel product) => product.Specifications)
			select spec.Id).ToList();
		List<ProductSpecificationModel> productSpecifications = await _context.ProductSpecifications.Where((ProductSpecificationModel x) => specificationIds.Contains(x.SpecificationId)).ToListAsync();
		decimal totalPrice = await CalculateTotalPrice(model.OrderProducts, model.ShippingAmount, model.DiscountId, productSpecifications);
		CustomerModel existingCustomer = await _context.Customers.FirstOrDefaultAsync((CustomerModel d) => d.Email == model.CustomerEmail);
		if (existingCustomer == null)
		{
			CustomerModel customer = new CustomerModel
			{
				Id = Guid.NewGuid(),
				Email = model.CustomerEmail,
				Phone = model.CustomerPhone,
				Name = model.CustomerName
			};
			_context.Customers.Add(customer);
		}
		else
		{
			existingCustomer.Name = model.CustomerName;
			existingCustomer.Phone = model.CustomerPhone;
		}
		OrderModel order = new OrderModel
		{
			Id = Guid.NewGuid(),
			DateOfCreation = DateTime.Now,
			OrderStatus = OrderStatus.Pending,
			CustomerEmail = model.CustomerEmail,
			CustomerName = model.CustomerName,
			CustomerPhone = model.CustomerPhone,
			ShippingAddress = model.ShippingAddress,
			DiscountId = (string.IsNullOrEmpty(model.DiscountId) ? ((Guid?)null) : new Guid?(Guid.Parse(model.DiscountId))),
			Price = totalPrice,
			OrderNumber = await _orderService.GenerateOrderNumber(),
			ShippingAmount = (((GetTotalItemPrice(model.OrderProducts, productSpecifications) >= 150m) & (model.ShippingAmount == (decimal)_shippingFeeSettings.Standard)) ? 0m : model.ShippingAmount),
			PaymentStatus = PaymentStatus.Pending
		};
		_context.Orders.Add(order);
		if (model.OrderProducts.Any())
		{
			foreach (AddOrderProductModel orderProduct in model.OrderProducts)
			{
				string specStrings = "";
				if (orderProduct.Specifications.Any())
				{
					IEnumerable<string> specifications = (await _context.Specifications.Where((SpecificationsModel x) => orderProduct.Specifications.Select((SpecificationsModel specificationsModel) => specificationsModel.Id).Contains(x.Id)).ToListAsync()).Select((SpecificationsModel spec) => spec.Name + " : " + spec.Value);
					specStrings = string.Join(Environment.NewLine, specifications);
				}
				_context.OrderProducts.Add(new OrderProductsModel
				{
					Id = Guid.NewGuid(),
					OrderId = order.Id,
					ProductId = orderProduct.ProductId,
					Quantity = orderProduct.Quantity,
					Price = orderProduct.Price,
					Specification = specStrings
				});
			}
		}
		await _context.SaveChangesAsync();
		return Ok(order);
	}

    private decimal GetTotalItemPrice(
    List<AddOrderProductModel> orderProducts,
    List<ProductSpecificationModel> productSpecifications)
    {
        decimal totalPrice = 0; 
        foreach (var product in orderProducts)
        {
            decimal itemPrice = 0; 
            if (product.Specifications != null && product.Specifications.Any())
            {
                foreach (var spec in product.Specifications)
                {
                    var specData = productSpecifications
                        .FirstOrDefault(x => x.SpecificationId == spec.Id && x.ProductId == product.ProductId); 
                    itemPrice += specData.Price;
                }
            }
            else
            {
                itemPrice = product.Price;
            }

            totalPrice += itemPrice * product.Quantity;
        } 
        return totalPrice;
    }

    private async Task<decimal> CalculateTotalPrice(List<AddOrderProductModel> orderProducts, decimal shippingAmount, string discountId, List<ProductSpecificationModel> productSpecifications)
	{
		decimal totalPrice = GetTotalItemPrice(orderProducts, productSpecifications);
		totalPrice = (((totalPrice >= 150m) & (shippingAmount == (decimal)_shippingFeeSettings.Standard)) ? (totalPrice + 0m) : (totalPrice + shippingAmount));
		if (!string.IsNullOrEmpty(discountId))
		{
			DiscountModel discount = await _context.Discounts.FindAsync(Guid.Parse(discountId));
			if (discount != null)
			{
				decimal discountDecimal = discount.Percentage / 100m;
				decimal discountAmount = totalPrice * discountDecimal;
				totalPrice -= discountAmount;
			}
		}
		return totalPrice;
	}

	[HttpPut]
	public async Task<IActionResult> PutOrder(Guid id, OrderModel order)
	{
		if (id != order.Id)
		{
			return BadRequest();
		}
		_context.Entry(order).State = EntityState.Modified;
		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateConcurrencyException)
		{
			if (!OrderExists(id))
			{
				return NotFound();
			}
			throw;
		}
		return NoContent();
	}

	[HttpDelete]
	public async Task<IActionResult> DeleteOrder(Guid id)
	{
		OrderModel order = await _context.Orders.FindAsync(id);
		if (order == null)
		{
			return NotFound();
		}
		_context.Orders.Remove(order);
		await _context.SaveChangesAsync();
		return NoContent();
	}

    [AllowAnonymous]
    [HttpPost("SendInvoice")]
    public async Task<IActionResult> SendOrderInvoice(Guid id)
    {
        await _invoiceService.CreateInvoice(id);
        return NoContent();
    }

    private bool OrderExists(Guid id)
	{
		return _context.Orders.Any((OrderModel e) => e.Id == id);
	}

    
}
