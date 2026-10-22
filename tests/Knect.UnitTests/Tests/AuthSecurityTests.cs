using Microsoft.AspNetCore.Identity;
using Knect.Application.Authentication.Exceptions;
using Knect.Domain.Entities;
using Knect.Domain.Enums;

namespace Knect.UnitTests.Tests;

public class AuthSecurityTests
{
    [Fact]
    public void PasswordHashing_UsesDifferentHashForSamePassword()
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
        var user = new User { FirstName = "A", LastName = "B", Email = "a@b.com", UserName = "a@b.com" };

        var hash1 = hasher.HashPassword(user, "Password123!");
        var hash2 = hasher.HashPassword(user, "Password123!");

        Assert.NotEqual(hash1, hash2);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, hash1, "Password123!"));
    }

    [Fact]
    public void AuthException_StoresHttpStatusCode()
    {
        var ex = new AuthException("error", 401);
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("error", ex.Message);
    }

    [Fact]
    public void JobTransition_RejectsInvalidState()
    {
        Assert.False(Job.IsTransitionAllowed(JobStatus.Closed, JobStatus.Published));
    }
}
