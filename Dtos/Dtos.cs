namespace CampusLostAndFound.Dtos
{
    public record ListingCreateDto(string OwnerName, string Type, string Title, string Description, string Category, string Location, DateTime EventDate, string? PhotoUrl);
    public record ListingUpdateDto(string Title, string Description, string Category, string Location, DateTime EventDate, string? PhotoUrl, string Status);
    public record ListingResponseDto(Guid Id, string OwnerName, string Type, string Title, string Description, string Category, string Location, DateTime EventDate, string? PhotoUrl, string Status, DateTime CreatedAt, DateTime UpdatedAt);

    public record PaginatedListingsResponseDto(IEnumerable<ListingResponseDto> Items, int Total, int TotalPages, int Page);

    public record MonthlyReportDto(string Month, int Lost, int Found, int Total);

    // PATCH /api/listings/{id}/ai-metadata — written back by n8n workflow B
    public record ListingAiMetadataDto(string? AiTags, string? NormalizedLocation, string? Summary);

    public record ClaimCreateDto(string RequesterName, string Message);
    public record ClaimResponseDto(Guid Id, Guid ListingId, string RequesterName, string Message, string Status, DateTime CreatedAt, DateTime? DecidedAt);

    public record RagResponseDto(string Answer, IEnumerable<ListingResponseDto> Listings, IEnumerable<Guid> Citations);

    // B) Similar listings — returns List<ListingResponseDto> directly, no wrapper needed

    // C) Claim Confidence Checker
    public record ClaimCheckRequestDto(Guid ListingId, string ClaimMessage);
    public record ClaimCheckResponseDto(int Score, IEnumerable<string> Issues, IEnumerable<string> Suggestions, string ImprovedMessage);

    // D) Admin Moderation Analyzer
    public record ModerationRequestDto(int SinceDays = 7, int MaxListings = 200);
    public record FlaggedListingDto(Guid ListingId, string Reason, string Severity);
    public record ModerationResponseDto(IEnumerable<FlaggedListingDto> Flagged, string Summary);

    // E) FAQ / Trends Generator
    public record FaqItemDto(string Q, string A);
    public record FaqStatsDto(Dictionary<string, int> ByCategory, Dictionary<string, int> ByLocation, int LostCount, int FoundCount);
    public record FaqResponseDto(IEnumerable<FaqItemDto> Faq, FaqStatsDto Stats);

    // F) Campus Assistant
    public record AssistRequestDto(string Message);
    public record ToolCallLogDto(string Tool, object Args);
    public record AssistResponseDto(string Answer, IEnumerable<ToolCallLogDto> ToolCalls, IEnumerable<Guid> Citations);
}
