using SmartFleet.Backend.Models;
using SmartFleet.Backend.Models.Enums;
using Xunit;

namespace SmartFleet.Backend.Tests;

public class UnitTest1
{
    [Fact]
    public void RoleEnum_ShouldContainExpectedRoles()
    {
        // Assert that the three required roles exist exactly as specified
        Assert.True(Enum.IsDefined(typeof(Role), "Operator"));
        Assert.True(Enum.IsDefined(typeof(Role), "Technician"));
        Assert.True(Enum.IsDefined(typeof(Role), "Supervisor"));
        Assert.Equal(3, Enum.GetValues<Role>().Length);
    }

    [Fact]
    public void UserModel_ShouldInstantiateProperly()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = id,
            Name = "Alex Rover",
            Email = "alex@smartfleet.internal",
            PasswordHash = "hashed_pw_placeholder",
            Role = Role.Operator,
            CreatedAt = now,
            UpdatedAt = now
        };

        Assert.Equal(id, user.Id);
        Assert.Equal("Alex Rover", user.Name);
        Assert.Equal("alex@smartfleet.internal", user.Email);
        Assert.Equal(Role.Operator, user.Role);
        Assert.Equal(now, user.CreatedAt);
    }
}
