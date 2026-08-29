using Microsoft.EntityFrameworkCore;
using Erp.Domain.Models;
using Portal.Data;

namespace Portal.DBLayer
{
    public class PortalContentDataDbLayer : IPortalContentDataDbLayer
    {
        private readonly ApplicationDbContext _context;

        public PortalContentDataDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PortalContentData>> GetAllAsync()
        {
            return await _context.PortalContentData.ToListAsync();
        }

        public async Task<List<PortalContentData>> GetByTypeAsync(string dataType)
        {
            return await _context.PortalContentData
                .Where(c => c.DataType == dataType)
                .ToListAsync();
        }

        public async Task<PortalContentData?> GetByIdAsync(int id)
        {
            return await _context.PortalContentData.FindAsync(id);
        }

        public async Task UpdateAsync(PortalContentData portalContentData)
        {
            _context.Entry(portalContentData).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<PortalContentData> AddAsync(PortalContentData portalContentData)
        {
            _context.PortalContentData.Add(portalContentData);
            await _context.SaveChangesAsync();
            return portalContentData;
        }

        public async Task DeleteAsync(PortalContentData portalContentData)
        {
            _context.PortalContentData.Remove(portalContentData);
            await _context.SaveChangesAsync();
        }

        public bool Exists(int id)
        {
            return _context.PortalContentData.Any(e => e.Id == id);
        }
    }
}
