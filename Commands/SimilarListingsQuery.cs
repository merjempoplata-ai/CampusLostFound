using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record SimilarListingsQuery(Guid ListingId, int K) : IRequest<IEnumerable<ListingResponseDto>>;
