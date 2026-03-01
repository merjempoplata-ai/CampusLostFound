using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record ModerationAnalyzeCommand(ModerationRequestDto Dto) : IRequest<ModerationResponseDto>;
