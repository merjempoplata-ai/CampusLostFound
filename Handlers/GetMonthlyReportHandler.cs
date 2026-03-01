using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CampusLostAndFound.Handlers;

public class GetMonthlyReportHandler(AppDbContext db) : IRequestHandler<GetMonthlyReportQuery, MonthlyReportDto>
{
    public async Task<MonthlyReportDto> Handle(GetMonthlyReportQuery request, CancellationToken cancellationToken)
    {
        var start = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = start.AddMonths(1);

        var listings = await db.Listings
            .Where(l => l.EventDate >= start && l.EventDate < end)
            .ToListAsync(cancellationToken);

        return new MonthlyReportDto(
            $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(request.Month)} {request.Year}",
            listings.Count(l => l.Type == "Lost"),
            listings.Count(l => l.Type == "Found"),
            listings.Count);
    }
}
