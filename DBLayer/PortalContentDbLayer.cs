using Microsoft.EntityFrameworkCore;
using Erp.Domain.Models;
using Portal.Data;

namespace Portal.DBLayer
{
    public class PortalContentDbLayer: IPortalContentDbLayer
    {
        private readonly ApplicationDbContext _context;
        public PortalContentDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<PortalContent>> GetAllAsync()
        {
            return await _context.PortalContent.ToListAsync();
        }
        public async Task<List<PortalContent>> GetContentsByCategoryAsync(string category, string? type)
        {
            var query = _context.PortalContent
                .Include(p => p.PortalContentCategories)
                .ThenInclude(pcc => pcc.PortalCategory)
                .Where(c=>c.Status=="Active")
                .AsQueryable();
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.PortalContentCategories.Any(pcc => pcc.PortalCategory != null && pcc.PortalCategory.Name == category));
            }
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.PortalContentCategories.Any(pcc => pcc.PortalCategory != null && pcc.PortalCategory.CategoryType == type));
            }
            return await query.ToListAsync();
        }
        public async Task<PortalContent?> GetByIdAsync(int id)
        {
            return await _context.PortalContent.FindAsync(id);
        }
        public async Task UpdateAsync(PortalContent portalContent)
        {
            _context.Entry(portalContent).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        public async Task<PortalContent> AddAsync(PortalContent portalContent)
        {
            _context.PortalContent.Add(portalContent);
            await _context.SaveChangesAsync();
            return portalContent;
        }
        public async Task DeleteAsync(PortalContent portalContent)
        {
            _context.PortalContent.Remove(portalContent);
            await _context.SaveChangesAsync();
        }
        public bool Exists(int id)
        {
            return _context.PortalContent.Any(e => e.Id == id);
        }
    }
}
