using Erp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Portal.DBLayer
{
    public class PortalCategoryDbLayer: IPortalCategoryDbLayer
    {
        private readonly ApplicationDbContext _context;
        public PortalCategoryDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<PortalCategory>> GetAllAsync()
        {
            return await _context.PortalCategory.ToListAsync();
        }

        public async Task<List<PortalCategory>> GetAllByStatusAsync(string? status)
        {
            var categories = await _context.PortalCategory.ToListAsync();
            if (!string.IsNullOrEmpty(status))
            {
                categories = categories.Where(c => c.Status == status).ToList();
            }
            return categories;
        }

        public async Task<PortalCategory?> GetByIdAsync(int id)
        {
            return await _context.PortalCategory.FindAsync(id);
        }
        public async Task UpdateAsync(PortalCategory portalCategory)
        {
            _context.Entry(portalCategory).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        public async Task<PortalCategory> AddAsync(PortalCategory portalCategory)
        {
            _context.PortalCategory.Add(portalCategory);
            await _context.SaveChangesAsync();
            return portalCategory;
        }
        public async Task DeleteAsync(PortalCategory portalCategory)
        {
            _context.PortalCategory.Remove(portalCategory);
            await _context.SaveChangesAsync();
        }
        public bool Exists(int id)
        {
            return _context.PortalCategory.Any(e => e.Id == id);
        }
    }
}
