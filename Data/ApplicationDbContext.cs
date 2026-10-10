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
        public DbSet<PortalContentData> PortalContentData { get; set; } = default!;
        public DbSet<PortalItemPrice> PortalItemPrice { get; set; } = default!;
        public DbSet<Portal.Services.MessageBroker.OutboxMessage> OutboxMessage { get; set; } = default!;
        public DbSet<Portal.Services.MessageBroker.ProcessedMessage> ProcessedMessage { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Portal.Services.MessageBroker.OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessage");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasIndex(e => e.CreatedAt)
                    .HasFilter("[ProcessedAt] IS NULL")
                    .HasDatabaseName("IX_OutboxMessage_Pending");
            });

            builder.Entity<Portal.Services.MessageBroker.ProcessedMessage>(entity =>
            {
                entity.ToTable("ProcessedMessage");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasIndex(e => new { e.MessageId, e.ConsumerType }).IsUnique().HasDatabaseName("UX_ProcessedMessage_Message_Consumer");
                entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_ProcessedMessage_ExpiresAt");
            });
        }
    }
}
