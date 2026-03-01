using MediatR;

namespace CampusLostAndFound.Commands;

public record ReindexListingsCommand() : IRequest<int>;
