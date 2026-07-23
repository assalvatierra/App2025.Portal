using Erp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Portal.DBLayer
{
    public class PortalItemDbLayer : IPortalItemDbLayer
    {
        private readonly ApplicationDbContext _context;

        public PortalItemDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PortalItem>> GetAllAsync()
        {
            return await _context.PortalItem.ToListAsync();
        }

        public async Task<List<PortalItem>> SearchItemsAsync(string searchTerm, List<int> itemIds)
        {
            List<string> searchKeywords=new List<string>();
            searchTerm
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ForEach(t =>t.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(t1 => searchKeywords.Add(t1)));

            var query = _context.PortalItem                    
                .Include(itemspecs => itemspecs.PortalItemSpecs)
                .Include(portalItemPrices => portalItemPrices.PortalItemPrices
                    .Where(p => p.Status == "Active")
                    .OrderByDescending(p => p.ValidFrom)
                )
                .Where(p => !p.IsArchived && p.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => 
                        (p.IsActive && !p.IsArchived) 
                    );
                searchKeywords.ForEach(keyword =>
                {
                    query = query.Where(p =>
                        (p.Name.Contains(keyword)) ||
                        (p.Description != null && p.Description.Contains(keyword)) ||
                        (p.Code != null && p.Code.Contains(keyword))
                    );
                });
            }

            if (itemIds != null && itemIds.Count > 0)
            {
                query = query.Where(p => itemIds.Contains(p.Id));
            }

            return await query
                .OrderBy(p => p.SortOrder ?? int.MaxValue)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }
        public async Task<List<PortalItem>> GetItemsByCategoryAsync(string category, string? type)
        {
            var query = _context.PortalItem
                .Include(p => p.PortalItemCategories)
                .ThenInclude(pic => pic.PortalCategory)
                .Include(p => p.PortalItemSpecs)
                .Include(portalItemPrices => portalItemPrices.PortalItemPrices
                    .Where(p => p.Status == "Active")
                    .OrderByDescending(p => p.ValidFrom)
                )
                .Where(p =>!p.IsArchived && p.IsActive)
                .AsQueryable();
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.PortalItemCategories.Any(pic => pic.PortalCategory != null && pic.PortalCategory.Name == category));
            }
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.PortalItemCategories.Any(pic => pic.PortalCategory != null && pic.PortalCategory.CategoryType == type));
            }

            return await query.ToListAsync();
        }

        public async Task<List<PortalItem>> GetByIdListAsync(List<int> IdList)
        {
            return await _context.PortalItem.Where(p => IdList.Contains(p.Id)).ToListAsync();
        }

        public async Task<PortalItem?> GetByIdAsync(int id)
        {
            return await _context.PortalItem
                .Include(pi=>pi.PortalItemSpecs)
                .Include(portalItemPrices => portalItemPrices.PortalItemPrices
                    .Where(p => p.Status == "Active")
                    .OrderByDescending(p => p.ValidFrom)
                )
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(PortalItem portalItem)
        {
            _context.Entry(portalItem).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<PortalItem> AddAsync(PortalItem portalItem)
        {
            _context.PortalItem.Add(portalItem);
            await _context.SaveChangesAsync();
            return portalItem;
        }

        public async Task DeleteAsync(PortalItem portalItem)
        {
            _context.PortalItem.Remove(portalItem);
            await _context.SaveChangesAsync();
        }

        public bool Exists(int id)
        {
            return _context.PortalItem.Any(e => e.Id == id);
        }
    }
}
