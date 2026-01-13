namespace CampusLostAndFound.Dtos
{
    public record ListingCreateDto(string OwnerName, string Type, string Title, string Description, string Category, string Location, DateTime EventDate, string? PhotoUrl);
    public record ListingUpdateDto(string Title, string Description, string Category, string Location, DateTime EventDate, string? PhotoUrl, string Status);
    public record ListingResponseDto(Guid Id, string OwnerName, string Type, string Title, string Description, string Category, string Location, DateTime EventDate, string? PhotoUrl, string Status, DateTime CreatedAt, DateTime UpdatedAt);
    public record PaginatedListingsResponseDto(IEnumerable<ListingResponseDto> Items, int Total, int TotalPages, int Page);
    public record ClaimCreateDto(string RequesterName, string Message);
    public record ClaimResponseDto(Guid Id, Guid ListingId, string RequesterName, string Message, string Status, DateTime CreatedAt, DateTime? DecidedAt);
}