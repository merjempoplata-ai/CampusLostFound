using CampusLostAndFound.Dtos;

namespace CampusLostAndFound.Services
{
    public interface IListingService
    {
        Task<ListingResponseDto> CreateAsync(ListingCreateDto dto);
        Task<ListingResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ListingResponseDto>> GetAllAsync(int page, int pageSize);
        Task<ListingResponseDto?> UpdateAsync(Guid id, ListingUpdateDto dto);
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