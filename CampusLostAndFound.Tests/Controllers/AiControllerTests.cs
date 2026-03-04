using CampusLostAndFound.Controllers;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CampusLostAndFound.Tests.Controllers;

/// <summary>
/// Tests AI controller input validation only — the actual AI logic lives in handlers
/// which are covered by their own unit tests.
/// </summary>
public class AiControllerTests
{
    private static AiController MakeController() =>
        new(new Mock<IMediator>().Object);

    // ──────────────────────────────────────────
    //  Search
    // ──────────────────────────────────────────

    [Fact]
    public async Task Search_returns_400_for_null_query()
    {
        var result = await MakeController().Search(null) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Search_returns_400_for_whitespace_query()
    {
        var result = await MakeController().Search("   ") as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    // ──────────────────────────────────────────
    //  Similar
    // ──────────────────────────────────────────

    [Fact]
    public async Task Similar_returns_400_for_k_zero()
    {
        var result = await MakeController().Similar(Guid.NewGuid(), 0) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Similar_returns_400_for_k_fifty_one()
    {
        var result = await MakeController().Similar(Guid.NewGuid(), 51) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    // ──────────────────────────────────────────
    //  ClaimCheck
    // ──────────────────────────────────────────

    [Fact]
    public async Task ClaimCheck_returns_400_for_empty_claim_message()
    {
        var dto    = new ClaimCheckRequestDto(Guid.NewGuid(), "");
        var result = await MakeController().ClaimCheck(dto) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    // ──────────────────────────────────────────
    //  Faq
    // ──────────────────────────────────────────

    [Fact]
    public async Task Faq_returns_400_for_days_zero()
    {
        var result = await MakeController().Faq(0) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Faq_returns_400_for_days_three_sixty_six()
    {
        var result = await MakeController().Faq(366) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    // ──────────────────────────────────────────
    //  Assist
    // ──────────────────────────────────────────

    [Fact]
    public async Task Assist_returns_400_for_empty_message()
    {
        var dto    = new AssistRequestDto("");
        var result = await MakeController().Assist(dto) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }
}
