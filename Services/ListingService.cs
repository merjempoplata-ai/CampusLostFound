using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services
{
    public class ListingService(AppDbContext context) : IListingService
    {
        public async Task<ListingResponseDto> CreateAsync(ListingCreateDto dto)
        {
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                OwnerName = dto.OwnerName,
                Type = dto.Type,
                Title = dto.Title,
                Description = dto.Description,
                Category = dto.Category,
                Location = dto.Location,
                EventDate = dto.EventDate,
                PhotoUrl = dto.PhotoUrl,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Listings.Add(listing);
            await context.SaveChangesAsync();
            return Map(listing);
        }

        public async Task<ListingResponseDto?> GetByIdAsync(Guid id)
        {
            var listing = await context.Listings.FindAsync(id);
            return listing == null ? null : Map(listing);
        }

        public async Task<IEnumerable<ListingResponseDto>> GetAllAsync(int page, int pageSize)
        {
            var listings = await context.Listings
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return listings.Select(Map);
        }

        public async Task<ListingResponseDto?> UpdateAsync(Guid id, ListingUpdateDto dto)
        {
            var listing = await context.Listings.FindAsync(id);
            if (listing == null) return null;

            listing.Title = dto.Title;
            listing.Description = dto.Description;
            listing.Category = dto.Category;
            listing.Location = dto.Location;
            listing.EventDate = dto.EventDate;
            listing.PhotoUrl = dto.PhotoUrl;
            listing.Status = dto.Status;
            listing.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return Map(listing);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var listing = await context.Listings.FindAsync(id);
            if (listing == null) return false;
            context.Listings.Remove(listing);
            await context.SaveChangesAsync();
            return true;
        }

        private static ListingResponseDto Map(Listing l) => 
            new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location, l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
    }
}