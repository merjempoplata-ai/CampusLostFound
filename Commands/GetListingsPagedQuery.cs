using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record GetListingsPagedQuery(int Page, int Limit, string? Type, string? Search) : IRequest<PaginatedListingsResponseDto>;
