using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record FaqQuery(int Days) : IRequest<FaqResponseDto>;
