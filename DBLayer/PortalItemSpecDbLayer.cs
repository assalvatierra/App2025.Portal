using Erp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace Portal.DBLayer
{
    public class PortalItemSpecDbLayer : IPortalItemSpecDbLayer
    {
        private readonly ApplicationDbContext _context;

        public PortalItemSpecDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PortalItemSpec>> GetAllAsync()
        {
            return await _context.PortalItemSpec
                .Include(s => s.PortalItem)
                .OrderBy(s => s.PortalItemId)
                .ThenBy(s => s.Order)
                .ToListAsync();
        }
        public async Task<List<PortalItemSpec>> GetItemISpecsCriteriaAsync(SearchDto search)
        {
            var query = _context.PortalItemSpec
                .FromSqlRaw(@"
                    SELECT pis.*
                    FROM PortalItemSpec pis
                    WHERE (TRY_CAST(JSON_VALUE(JsonData, '$.passenger_capacity') AS INT) >= {0} OR {0}=0)
                    AND (TRY_CAST(JSON_VALUE(JsonData, '$.luggage_capacity') AS INT) >= {1} OR {1}=0)
                    AND (UPPER(JSON_VALUE(JsonData, '$.transmission')) = UPPER({2}) OR UPPER({2})='ANY')
                    ",
                search.passenger_capacity, 
                search.luggage_capacity, 
                search.transmission)
                .AsQueryable();
   
            //var itemIds = await query.Select(s => (int)s.PortalItemId).Distinct().ToListAsync();
            //return itemIds;

            return query.ToList();
        }

        public async Task<List<PortalItemSpec>> GetByPortalItemIdAsync(int portalItemId)
        {
            return await _context.PortalItemSpec
                .Include(s => s.PortalItem)
                .Where(s => s.PortalItemId == portalItemId)
                .OrderBy(s => s.Order)
                .ToListAsync();
        }

        public async Task<PortalItemSpec?> GetByIdAsync(int id)
        {
            return await _context.PortalItemSpec
                .Include(s => s.PortalItem)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateAsync(PortalItemSpec portalItemSpec)
        {
            _context.Entry(portalItemSpec).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task<PortalItemSpec> AddAsync(PortalItemSpec portalItemSpec)
        {
            _context.PortalItemSpec.Add(portalItemSpec);
            await _context.SaveChangesAsync();
            return portalItemSpec;
        }

        public async Task DeleteAsync(PortalItemSpec portalItemSpec)
        {
            _context.PortalItemSpec.Remove(portalItemSpec);
            await _context.SaveChangesAsync();
        }

        public bool Exists(int id)
        {
            return _context.PortalItemSpec.Any(e => e.Id == id);
        }
    }
}
