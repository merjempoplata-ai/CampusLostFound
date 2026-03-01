using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record RagSearchQuery(string Query) : IRequest<RagResponseDto>;
