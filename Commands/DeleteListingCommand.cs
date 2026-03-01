using MediatR;

namespace CampusLostAndFound.Commands;

public record DeleteListingCommand(Guid Id) : IRequest<bool>;
