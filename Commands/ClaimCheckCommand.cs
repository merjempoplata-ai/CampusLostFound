using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record ClaimCheckCommand(ClaimCheckRequestDto Dto) : IRequest<ClaimCheckResponseDto>;
