using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record CreateListingCommand(ListingCreateDto Dto) : IRequest<ListingResponseDto>;
