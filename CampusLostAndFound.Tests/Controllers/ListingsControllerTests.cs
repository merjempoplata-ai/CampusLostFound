using CampusLostAndFound.Commands;
using CampusLostAndFound.Controllers;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CampusLostAndFound.Tests.Controllers;

file static class ListingDtoFactory
{
    public static ListingResponseDto Make(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Owner", "Lost", "Title", "Desc", "Category",
        "Location", DateTime.UtcNow, null, "Open", DateTime.UtcNow, DateTime.UtcNow);

    public static PaginatedListingsResponseDto Paged(int count = 1) =>
        new(Enumerable.Range(0, count).Select(_ => Make()), count, 1, 1);
}

public class ListingsControllerTests
{
    // ──────────────────────────────────────────
    //  GetAll
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetAll_returns_200_with_paged_result()
    {
        var mediator   = new Mock<IMediator>();
        var paged      = ListingDtoFactory.Paged(3);
        mediator.Setup(m => m.Send(It.IsAny<GetListingsPagedQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

        var controller = new ListingsController(mediator.Object);
        var result     = await controller.GetAll(1, 9, null, null) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        mediator.Verify(m => m.Send(It.Is<GetListingsPagedQuery>(q => q.Page == 1 && q.Limit == 9),
                                    It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────
    //  Get
    // ──────────────────────────────────────────

    [Fact]
    public async Task Get_returns_200_when_found()
    {
        var mediator = new Mock<IMediator>();
        var dto      = ListingDtoFactory.Make();
        mediator.Setup(m => m.Send(It.IsAny<GetListingByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

        var controller = new ListingsController(mediator.Object);
        var result     = await controller.Get(dto.Id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task Get_returns_404_when_not_found()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetListingByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ListingResponseDto?)null);

        var controller = new ListingsController(mediator.Object);
        var result     = await controller.Get(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ──────────────────────────────────────────
    //  Create
    // ──────────────────────────────────────────

    [Fact]
    public async Task Create_returns_201()
    {
        var mediator = new Mock<IMediator>();
        var created  = ListingDtoFactory.Make();
        mediator.Setup(m => m.Send(It.IsAny<CreateListingCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(created);

        var controller = new ListingsController(mediator.Object);
        var dto        = new ListingCreateDto("Owner", "Lost", "Title", "Desc", "Cat", "Loc", DateTime.UtcNow, null);
        var result     = await controller.Create(dto) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
    }

    // ──────────────────────────────────────────
    //  Update
    // ──────────────────────────────────────────

    [Fact]
    public async Task Update_returns_200_when_found()
    {
        var mediator = new Mock<IMediator>();
        var updated  = ListingDtoFactory.Make();
        mediator.Setup(m => m.Send(It.IsAny<UpdateListingCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updated);

        var controller = new ListingsController(mediator.Object);
        var dto        = new ListingUpdateDto("T", "D", "C", "L", DateTime.UtcNow, null, "Open");
        var result     = await controller.Update(Guid.NewGuid(), dto) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task Update_returns_404_when_not_found()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateListingCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ListingResponseDto?)null);

        var controller = new ListingsController(mediator.Object);
        var dto        = new ListingUpdateDto("T", "D", "C", "L", DateTime.UtcNow, null, "Open");
        var result     = await controller.Update(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundResult>(result);
    }

    // ──────────────────────────────────────────
    //  Delete
    // ──────────────────────────────────────────

    [Fact]
    public async Task Delete_returns_204_when_deleted()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<DeleteListingCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var controller = new ListingsController(mediator.Object);
        var result     = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_returns_404_when_not_found()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<DeleteListingCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var controller = new ListingsController(mediator.Object);
        var result     = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ──────────────────────────────────────────
    //  GetReport — input validation
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetReport_returns_400_for_month_zero()
    {
        var mediator   = new Mock<IMediator>();
        var controller = new ListingsController(mediator.Object);

        var result = await controller.GetReport(2025, 0) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        mediator.Verify(m => m.Send(It.IsAny<GetMonthlyReportQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetReport_returns_400_for_month_thirteen()
    {
        var mediator   = new Mock<IMediator>();
        var controller = new ListingsController(mediator.Object);

        var result = await controller.GetReport(2025, 13) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        mediator.Verify(m => m.Send(It.IsAny<GetMonthlyReportQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
