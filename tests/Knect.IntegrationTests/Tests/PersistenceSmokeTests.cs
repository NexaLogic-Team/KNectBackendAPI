using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.IntegrationTests.Tests;

public class PersistenceSmokeTests
{
    [Fact]
    public void CanCreateDbContextWithPostgresProvider()
    {
        var options = new DbContextOptionsBuilder<KnectDbContext>()
            .UseNpgsql("Host=localhost;Database=knect_test;Username=postgres;Password=postgres;Port=5432;SSL Mode=Require")
            .Options;

        using var context = new KnectDbContext(options);
        Assert.NotNull(context);
    }
}
