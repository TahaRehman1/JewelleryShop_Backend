using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JeweleryAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HeroController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public HeroController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ✅ GET LIST (FAST + CACHED)
        [HttpGet("list")]
        public async Task<ActionResult<List<HeroDto>>> GetHeroList()
        {
            if (!_cache.TryGetValue("hero_list", out List<HeroDto> heroes))
            {
                heroes = await _context.HeroSections
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenByDescending(x => x.UpdatedAt)
                    .Select(x => new HeroDto
                    {
                        Title = x.Title,
                        Subtitle = x.Subtitle,
                        ImageUrl = $"{Request.Scheme}://{Request.Host}/{x.ImagePath}"
                    })
                    .ToListAsync();

                _cache.Set("hero_list", heroes, TimeSpan.FromMinutes(10));
            }

            return Ok(heroes);
        }
        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<HeroDto>>> GetAll(
    int skip = 0,
    int take = 10,
    string? search = null)
        {
            var query = _context.HeroSections.AsQueryable();

            // 🔍 Optional search (Title / Subtitle)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x =>
                    x.Title.Contains(search) ||
                    x.Subtitle.Contains(search));
            }

            // 📊 Total count (before pagination)
            var total = await query.CountAsync();

            // 📦 Data with pagination
            var data = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Subtitle,
                    x.ImagePath,
                    x.DisplayOrder,
                    x.IsActive,
                    x.UpdatedAt,
                    ImageUrl = $"{Request.Scheme}://{Request.Host}/{x.ImagePath}"
                })
                .ToListAsync();

            return Ok(new
            {
                data
            });
        }
        [HttpGet("GetAllCount")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> GetAllCount()
        {
            var count = await _context.HeroSections.CountAsync();
            return Ok(count);
        }
        // ✅ CREATE / UPDATE HERO
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateHero(
    [FromForm] string title,
    [FromForm] string subtitle,
    [FromForm] int displayOrder,
    IFormFile image)
        {
            string imagePath = null;

            // ✅ Upload Image
            if (image != null)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hero");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imagePath = $"hero/{fileName}";
            }

            int total = await _context.HeroSections.CountAsync();

            // ✅ Clamp
            if (displayOrder < 1) displayOrder = 1;
            if (displayOrder > total + 1) displayOrder = total + 1;

            // ✅ Shift if conflict
            bool exists = await _context.HeroSections
                .AnyAsync(x => x.DisplayOrder == displayOrder);

            if (exists)
            {
                var toShift = await _context.HeroSections
                    .Where(x => x.DisplayOrder >= displayOrder)
                    .OrderByDescending(x => x.DisplayOrder)
                    .ToListAsync();

                foreach (var h in toShift)
                    h.DisplayOrder++;
            }

            var hero = new HeroSection
            {
                Id = Guid.NewGuid(), // ✅ GUID
                Title = title,
                Subtitle = subtitle,
                ImagePath = imagePath,
                DisplayOrder = displayOrder,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HeroSections.Add(hero);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest("Display order conflict occurred.");
            }

            _cache.Remove("hero_list");

            return Ok(new { message = "Hero created successfully" });
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteHero(Guid id)
        {
            var hero = await _context.HeroSections.FindAsync(id);

            if (hero == null)
                return NotFound("Hero not found");

            int deletedOrder = hero.DisplayOrder;

            // ✅ Delete image
            if (!string.IsNullOrEmpty(hero.ImagePath))
            {
                var path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    hero.ImagePath.Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.HeroSections.Remove(hero);
            await _context.SaveChangesAsync();

            // ✅ Reorder remaining (gap-free)
            var toShift = await _context.HeroSections
                .Where(x => x.DisplayOrder > deletedOrder)
                .ToListAsync();

            foreach (var h in toShift)
                h.DisplayOrder--;

            await _context.SaveChangesAsync();

            _cache.Remove("hero_list");

            return Ok(new { message = "Hero deleted successfully" });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHero(
       Guid id,
       [FromForm] string title,
       [FromForm] string subtitle,
       [FromForm] int displayOrder,
       IFormFile image)
        {
            var hero = await _context.HeroSections.FindAsync(id);

            if (hero == null)
                return NotFound("Hero not found");

            int oldOrder = hero.DisplayOrder;

            // ✅ Image update
            if (image != null)
            {
                if (!string.IsNullOrEmpty(hero.ImagePath))
                {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        hero.ImagePath.Replace("/", Path.DirectorySeparatorChar.ToString())
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hero");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                hero.ImagePath = $"hero/{fileName}";
            }

            int total = await _context.HeroSections.CountAsync();

            // ✅ Clamp
            if (displayOrder < 1) displayOrder = 1;
            if (displayOrder > total) displayOrder = total;

            // ✅ Reorder if needed
            if (displayOrder != oldOrder)
            {
                if (displayOrder < oldOrder)
                {
                    var toShift = await _context.HeroSections
                        .Where(x => x.DisplayOrder >= displayOrder && x.DisplayOrder < oldOrder && x.Id != id)
                        .ToListAsync();

                    foreach (var h in toShift)
                        h.DisplayOrder++;
                }
                else
                {
                    var toShift = await _context.HeroSections
                        .Where(x => x.DisplayOrder <= displayOrder && x.DisplayOrder > oldOrder && x.Id != id)
                        .ToListAsync();

                    foreach (var h in toShift)
                        h.DisplayOrder--;
                }

                hero.DisplayOrder = displayOrder;
            }

            hero.Title = title;
            hero.Subtitle = subtitle;
            hero.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest("Display order conflict occurred.");
            }

            _cache.Remove("hero_list");

            return Ok(new { message = "Hero updated successfully" });
        }
    }
}