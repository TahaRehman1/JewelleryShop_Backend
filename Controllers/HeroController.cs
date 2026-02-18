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
        [Authorize(Roles ="Admin")]
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
        public async Task<ActionResult<int>> GetAllCount()
        {
            var count = await _context.HeroSections.CountAsync();
            return Ok(count);
        }
        // ✅ CREATE / UPDATE HERO
        [HttpPost]
        public async Task<IActionResult> CreateHero(
            [FromForm] string title,
            [FromForm] string subtitle,
            [FromForm] int displayOrder,
            IFormFile image)
        {
            string imagePath = null;

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

            var hero = new HeroSection
            {
                Title = title,
                Subtitle = subtitle,
                ImagePath = imagePath,
                DisplayOrder = displayOrder,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            _context.HeroSections.Add(hero);
            await _context.SaveChangesAsync();

            // ✅ CLEAR CACHE (IMPORTANT)
            _cache.Remove("hero_list");

            return Ok();
        }
    }
}
