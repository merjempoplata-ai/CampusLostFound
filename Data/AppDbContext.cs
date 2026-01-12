using Microsoft.EntityFrameworkCore;
using CampusLostAndFound.Models;

namespace CampusLostAndFound.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Listing> Listings => Set<Listing>();
        public DbSet<Claim> Claims => Set<Claim>();
    }
}