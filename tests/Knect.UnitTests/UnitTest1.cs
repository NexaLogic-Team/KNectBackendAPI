using Knect.Domain.Entities;
using Knect.Domain.Enums;
using Knect.Infrastructure.Development;
using Knect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Knect.UnitTests;

public class DbMappingTests
{
    [Fact]
    public void UserEntityMapsToUsersTable()
    {
        var options = new DbContextOptionsBuilder<KnectDbContext>()
            .UseNpgsql("Host=localhost;Database=knect_dev;Username=postgres;Password=postgres;Port=5432;SSL Mode=Disable")
            .Options;

        using var context = new KnectDbContext(options);

        var entity = context.Model.FindEntityType(typeof(User));

        Assert.NotNull(entity);
        Assert.Equal("Users", entity!.GetTableName());
    }

    [Fact]
    public void DevelopmentAdminSeedUsesExpectedSectionName()
    {
        Assert.Equal("DevelopmentAdmin", DevelopmentAdminSeedOptions.SectionName);
    }

    [Fact]
    public void AdminRoleIsRepresentedByNameAndEnum()
    {
        var role = new Role
        {
            Name = "Admin",
            Description = "Platform administrator",
            RoleType = UserRoleType.Admin,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("Admin", role.Name);
        Assert.Equal(UserRoleType.Admin, role.RoleType);
    }
}
