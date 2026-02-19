using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JeweleryAppBackend.Enumerations;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JeweleryAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DiscountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET ALL (Admin)
        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<DiscountModel>>> GetAll(int skip = 0, int take = 10)
        {
            var discounts = await _context.Discounts
                .OrderByDescending(d => d.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return Ok(discounts);
        }

        // ✅ COUNT (Optimized)
        [HttpGet("GetCount")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> GetCount()
        {
            return Ok(await _context.Discounts.CountAsync());
        }

        // ✅ GET BY ID
        [HttpGet("GetById")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DiscountModel>> GetById(Guid id)
        {
            var discount = await _context.Discounts.FindAsync(id);

            if (discount == null)
                return NotFound("Discount not found");

            return Ok(discount);
        }

        // ✅ VALIDATE DISCOUNT (USED BY FRONTEND)
        [HttpGet("GetByCode")]
        public async Task<ActionResult> GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Invalid code");

            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code.ToLower() == code.ToLower());

            if (discount == null)
                return NotFound("Invalid voucher code");

            if (discount.Status != DiscountStatus.Active)
                return Conflict("Voucher is not active");

            if (discount.RedemptionLimit > 0 &&
                discount.TimesRedeemed >= discount.RedemptionLimit)
                return Conflict("Voucher usage limit reached");

            return Ok(discount);
        }

        // ✅ CREATE
        [HttpPost("Post")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DiscountModel>> Post(AddDiscountModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
                return BadRequest("Code is required");

            // ✅ Prevent duplicate codes
            bool exists = await _context.Discounts
                .AnyAsync(d => d.Code.ToLower() == model.Code.ToLower());

            if (exists)
                return Conflict("Discount code already exists");

            DiscountStatus status = Enum.Parse<DiscountStatus>(model.Status, true);

            var discount = new DiscountModel
            {
                Id = Guid.NewGuid(),
                Code = model.Code.Trim(),
                Percentage = model.Percentage,
                Status = status,
                RedemptionLimit = model.RedemptionLimit,
                TimesRedeemed = 0
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            return Ok(discount);
        }

        // ✅ UPDATE
        [HttpPut("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(AddDiscountModel model)
        {
            var existing = await _context.Discounts.FindAsync(model.Id);

            if (existing == null)
                return NotFound("Discount not found");

            // ✅ Prevent duplicate codes (excluding self)
            bool exists = await _context.Discounts
                .AnyAsync(d => d.Id != model.Id && d.Code.ToLower() == model.Code.ToLower());

            if (exists)
                return Conflict("Discount code already exists");

            DiscountStatus status = Enum.Parse<DiscountStatus>(model.Status, true);

            existing.Code = model.Code.Trim();
            existing.Percentage = model.Percentage;
            existing.Status = status;
            existing.RedemptionLimit = model.RedemptionLimit;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // ✅ DELETE
        [HttpDelete("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var discount = await _context.Discounts.FindAsync(id);

            if (discount == null)
                return NotFound("Discount not found");

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}