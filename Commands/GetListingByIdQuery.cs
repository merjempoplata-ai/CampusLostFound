using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record GetListingByIdQuery(Guid Id) : IRequest<ListingResponseDto?>;
