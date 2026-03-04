using CampusLostAndFound.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Tests.Helpers;

public static class DbContextFactory
{
    public static AppDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
