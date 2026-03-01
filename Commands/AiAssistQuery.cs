using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record AiAssistQuery(string Message) : IRequest<AssistResponseDto>;
