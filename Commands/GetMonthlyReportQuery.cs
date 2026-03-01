using CampusLostAndFound.Dtos;
using MediatR;

namespace CampusLostAndFound.Commands;

public record GetMonthlyReportQuery(int Year, int Month) : IRequest<MonthlyReportDto>;
