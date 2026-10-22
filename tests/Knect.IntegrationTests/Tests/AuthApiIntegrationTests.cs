using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.IntegrationTests.Tests;

public class AuthApiIntegrationTests
{
    [Fact]
    public void CanCreateDbContextWithNpgsqlConnectionString()
    {
        var options = new DbContextOptionsBuilder<KnectDbContext>()
            .UseNpgsql("Host=localhost;Database=knect_test;Username=postgres;Password=postgres;Port=5432;SSL Mode=Disable")
            .Options;

        using var context = new KnectDbContext(options);
        Assert.NotNull(context);
    }
}
