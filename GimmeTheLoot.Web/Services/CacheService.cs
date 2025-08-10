using GimmeTheLoot.Web.Data;
using GimmeTheLoot.Shared.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace GimmeTheLoot.Web.Services
{
    public class CacheService
    {
        private readonly AppDbContext _db;
        private Dictionary<string, TransactionCategory> _categoryCache = null!;

        public CacheService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Loads categories from DB and caches them keyed by CsvName (lowercase).
        /// </summary>
        public async Task<Dictionary<string, TransactionCategory>> GetOrLoadCategoryCacheAsync()
        {
            if (_categoryCache == null)
            {
                var categories = await _db.TransactionCategory.ToListAsync();
                _categoryCache = categories
                    .ToDictionary(c => c.Detailed.ToLowerInvariant(), c => c);
            }
            return _categoryCache;
        }

        /// <summary>
        /// Adds new categories to cache and DB in bulk.
        /// </summary>
        public async Task AddNewCategoriesAsync(IEnumerable<TransactionCategory> newCategories)
        {
            if (newCategories == null || !newCategories.Any())
                return;

            _db.TransactionCategory.AddRange(newCategories);
            await _db.SaveChangesAsync();

            // Reload newly added categories with IDs assigned
            var keys = newCategories.Select(c => c.Detailed.ToLowerInvariant()).ToList();
            var freshCategories = await _db.TransactionCategory
                .Where(c => keys.Contains(c.Detailed.ToLowerInvariant()))
                .ToListAsync();

            foreach (var cat in freshCategories)
            {
                _categoryCache[cat.Detailed.ToLowerInvariant()] = cat;
            }
        }
    }
}
