using MediatR;

namespace CampusLostAndFound.Commands;

public record RejectClaimCommand(Guid Id) : IRequest<bool>;
