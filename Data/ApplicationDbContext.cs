using Erp.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Portal.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<PortalConfiguration> PortalConfiguration { get; set; } = default!;
        public DbSet<PortalItem> PortalItem { get; set; } = default!;
        public DbSet<PortalItemSpec> PortalItemSpec { get; set; } = default!;
        public DbSet<PortalReservation> PortalReservation { get; set; } = default!;
        public DbSet<PortalCategory> PortalCategory { get; set; } = default!;
        public DbSet<PortalItemCategory> PortalItemCategory { get; set; } = default!;
        public DbSet<PortalContent> PortalContent { get; set; } = default!;
        public DbSet<PortalContentCategory> PortalContentCategory { get; set; } = default!;
        public DbSet<PortalItemPrice> PortalItemPrice { get; set; } = default!;
    }
}
