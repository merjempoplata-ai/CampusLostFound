using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services
{
    public class ClaimService(AppDbContext context) : IClaimService
    {
        public async Task<ClaimResponseDto?> SubmitAsync(Guid listingId, ClaimCreateDto dto)
        {
            var listing = await context.Listings.FindAsync(listingId);
            if (listing == null) return null;

            var claim = new Claim
            {
                Id = Guid.NewGuid(),
                ListingId = listingId,
                RequesterName = dto.RequesterName,
                Message = dto.Message,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            context.Claims.Add(claim);
            await context.SaveChangesAsync();
            return Map(claim);
        }

        public async Task<IEnumerable<ClaimResponseDto>> GetAllAsync()
        {
            var claims = await context.Claims.ToListAsync();
            return claims.Select(Map);
        }

        public async Task<bool> AcceptAsync(Guid id)
        {
            var claim = await context.Claims.FindAsync(id);
            if (claim == null) return false;

            var listing = await context.Listings.FindAsync(claim.ListingId);
            if (listing == null) return false;

            claim.Status = "Accepted";
            claim.DecidedAt = DateTime.UtcNow;
            listing.Status = "Claimed";

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(Guid id)
        {
            var claim = await context.Claims.FindAsync(id);
            if (claim == null) return false;

            claim.Status = "Rejected";
            claim.DecidedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        private static ClaimResponseDto Map(Claim c) =>
            new(c.Id, c.ListingId, c.RequesterName, c.Message, c.Status, c.CreatedAt, c.DecidedAt);
    }
}