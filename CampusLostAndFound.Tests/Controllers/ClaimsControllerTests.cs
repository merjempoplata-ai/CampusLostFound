using CampusLostAndFound.Commands;
using CampusLostAndFound.Controllers;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CampusLostAndFound.Tests.Controllers;

file static class ClaimDtoFactory
{
    public static ClaimResponseDto Make(Guid? listingId = null) =>
        new(Guid.NewGuid(), listingId ?? Guid.NewGuid(), "Requester", "Message",
            "Pending", DateTime.UtcNow, null);
}

public class ClaimsControllerTests
{
    // ──────────────────────────────────────────
    //  GetAll
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAll_returns_200()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllClaimsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { ClaimDtoFactory.Make() }.AsEnumerable());

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.GetAll() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    // ──────────────────────────────────────────
    //  Create
    // ──────────────────────────────────────────

    [Fact]
    public async Task Create_returns_201_when_listing_found()
    {
        var mediator = new Mock<IMediator>();
        var dto      = ClaimDtoFactory.Make();
        mediator.Setup(m => m.Send(It.IsAny<CreateClaimCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.Create(Guid.NewGuid(), new ClaimCreateDto("Bob", "Mine.")) as CreatedResult;

        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
    }

    [Fact]
    public async Task Create_returns_404_when_listing_missing()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateClaimCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimResponseDto?)null);

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.Create(Guid.NewGuid(), new ClaimCreateDto("Bob", "Mine."));

        Assert.IsType<NotFoundResult>(result);
    }

    // ──────────────────────────────────────────
    //  Accept
    // ──────────────────────────────────────────

    [Fact]
    public async Task Accept_returns_200_when_successful()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<AcceptClaimCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.Accept(Guid.NewGuid());

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Accept_returns_404_when_not_found()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<AcceptClaimCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.Accept(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ──────────────────────────────────────────
    //  Reject
    // ──────────────────────────────────────────

    [Fact]
    public async Task Reject_returns_200_when_successful()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<RejectClaimCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.Reject(Guid.NewGuid());

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Reject_returns_404_when_not_found()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<RejectClaimCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var controller = new ClaimsController(mediator.Object);
        var result     = await controller.Reject(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }
}
