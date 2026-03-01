using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record UpdateListingCommand(Guid Id, ListingUpdateDto Dto) : IRequest<ListingResponseDto?>;
