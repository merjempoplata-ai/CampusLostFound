using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record CreateClaimCommand(Guid ListingId, ClaimCreateDto Dto) : IRequest<ClaimResponseDto?>;
