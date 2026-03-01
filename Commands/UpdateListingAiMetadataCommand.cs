using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record UpdateListingAiMetadataCommand(Guid Id, ListingAiMetadataDto Dto) : IRequest<ListingResponseDto?>;
