using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JeweleryAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JeweleryAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        private const string ALL_BANNERS_KEY = "ALL_BANNERS";
        private const string ACTIVE_BANNERS_KEY = "ACTIVE_BANNERS";
        private const string BANNERS_COUNT_KEY = "BANNERS_COUNT";

        public BannersController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ✅ Get All (Cached)
        [HttpGet("GetAll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<BannerModel>>> GetAll(int skip, int take)
        {
            string cacheKey = $"{ALL_BANNERS_KEY}";

            if (!_cache.TryGetValue(cacheKey, out List<BannerModel> banners))
            {
                banners = await _context.Banners
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                _cache.Set(cacheKey, banners, TimeSpan.FromMinutes(5));
            }

            return banners;
        }

        // ✅ Get Count (Cached)
        [HttpGet("GetCount")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<int>> GetCount()
        {
            if (!_cache.TryGetValue(BANNERS_COUNT_KEY, out int count))
            {
                count = await _context.Banners.CountAsync();
                _cache.Set(BANNERS_COUNT_KEY, count, TimeSpan.FromMinutes(5));
            }

            return count;
        }

        // ✅ Get Active (Cached)
        [HttpGet("GetAllActive")]
        public async Task<ActionResult<List<BannerModel>>> GetAllActive()
        {
            if (!_cache.TryGetValue(ACTIVE_BANNERS_KEY, out List<BannerModel> banners))
            {
                banners = await _context.Banners
                    .Where(x => x.IsActive)
                    .ToListAsync();

                _cache.Set(ACTIVE_BANNERS_KEY, banners, TimeSpan.FromMinutes(5));
            }

            return banners;
        }

        [HttpGet("GetById")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BannerModel>> GetById(Guid id)
        {
            var banner = await _context.Banners.FindAsync(id);
            return banner == null ? NotFound() : Ok(banner);
        }

        // ✅ Create
        [HttpPost("Post")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BannerModel>> Post(AddBannerModel model)
        {
            var banner = new BannerModel
            {
                Id = Guid.NewGuid(),
                Body = model.Body,
                IsActive = model.IsActive
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();

            ClearCache();

            return Ok(banner);
        }

        // ✅ Update
        [HttpPut("Update")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BannerModel>> Update(BannerModel banner)
        {
            var existingBanner = await _context.Banners.FindAsync(banner.Id);

            if (existingBanner == null)
                return NotFound();

            existingBanner.Body = banner.Body;
            existingBanner.IsActive = banner.IsActive;

            await _context.SaveChangesAsync();

            ClearCache();

            return Ok(existingBanner);
        }

        // ✅ Delete
        [HttpDelete("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var banner = await _context.Banners.FindAsync(id);

            if (banner == null)
                return NotFound();

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();

            ClearCache();

            return Ok();
        }

        // ✅ Cache Clear Helper
        private void ClearCache()
        {
            _cache.Remove(ALL_BANNERS_KEY);
            _cache.Remove(ACTIVE_BANNERS_KEY);
            _cache.Remove(BANNERS_COUNT_KEY);

            // Optional: clear paginated keys (best effort)
            // If many variations exist, consider using a cache prefix strategy
        }
    }
}