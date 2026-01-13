using CampusLostAndFound.Dtos;

namespace CampusLostAndFound.Services
{
    public interface IListingService
    {
        Task<ListingResponseDto> CreateAsync(ListingCreateDto dto);
        Task<ListingResponseDto?> GetByIdAsync(Guid id);
        Task<PaginatedListingsResponseDto> GetAllAsync(int page, int limit, string? type, string? search); Task<ListingResponseDto?> UpdateAsync(Guid id, ListingUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
    }

    public interface IClaimService
    {
        Task<ClaimResponseDto?> SubmitAsync(Guid listingId, ClaimCreateDto dto);
        Task<IEnumerable<ClaimResponseDto>> GetAllAsync();
        Task<bool> AcceptAsync(Guid id);
        Task<bool> RejectAsync(Guid id);
    }
}