using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Listing>(e =>
            {
                e.ToTable("listings");
                e.Property(x => x.OwnerName).HasColumnName("owner_name");
                e.Property(x => x.EventDate).HasColumnName("event_date");
                e.Property(x => x.PhotoUrl).HasColumnName("photo_url");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Claim>(e =>
            {
                e.ToTable("claims");
                e.Property(x => x.ListingId).HasColumnName("listing_id");
                e.Property(x => x.RequesterName).HasColumnName("requester_name");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.DecidedAt).HasColumnName("decided_at");
            });
        }

        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Claim> Claims => Set<Claim>();
    }
}