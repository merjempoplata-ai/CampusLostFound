using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record GetAllClaimsQuery() : IRequest<IEnumerable<ClaimResponseDto>>;
