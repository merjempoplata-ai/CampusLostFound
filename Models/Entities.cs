using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models
{
    public class Listing
    {
        [Key]
        public Guid Id { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string Type { get; set; } = "Lost"; // Lost or Found
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string? PhotoUrl { get; set; }
        public string Status { get; set; } = "Open"; // Open, Claimed, Closed, Hidden
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Claim
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecidedAt { get; set; }
    }
}