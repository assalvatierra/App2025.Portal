using Microsoft.EntityFrameworkCore;
using Erp.Domain.Models;
using Portal.Data;

namespace Portal.DBLayer
{
    public class PortalConfigurationDbLayer : IPortalConfigurationDbLayer
    {
        private readonly ApplicationDbContext _context;
        public PortalConfigurationDbLayer(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<PortalConfiguration>> GetPortalConfigurationByNameAndCodeAsync(string name, string code)
        {
            //return portal configurations that match the name and code
            return await _context.PortalConfiguration
                .Where(pc => pc.Name == name && pc.SysCode == code)
                .ToListAsync();
        }

    }
}
