using MediatR;

namespace CampusLostAndFound.Commands;

public record AcceptClaimCommand(Guid Id) : IRequest<bool>;
