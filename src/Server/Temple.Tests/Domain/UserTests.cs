using Temple.Domain.Users;

namespace Temple.Tests.Domain;

public class UserTests
{
    [Fact]
    public void New_User_Has_Unique_Id()
    {
        var user1 = new User();
        var user2 = new User();
        
        Assert.NotEqual(user1.Id, user2.Id);
    }

    [Fact]
    public void New_User_Has_Creation_Timestamp()
    {
        var before = DateTime.UtcNow;
        var user = new User();
        var after = DateTime.UtcNow;
        
        Assert.InRange(user.CreatedUtc, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void New_User_Is_Not_SuperAdmin_By_Default()
    {
        var user = new User();
        
        Assert.False(user.IsSuperAdmin);
    }

    [Fact]
    public void New_User_Is_Not_Guest_By_Default()
    {
        var user = new User();
        
        Assert.False(user.IsGuest);
    }

    [Fact]
    public void User_Can_Be_Created_With_Properties()
    {
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            TenantId = tenantId,
            PasswordHash = "hashed-password",
            EmailVerifiedUtc = DateTime.UtcNow
        };
        
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);
        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.NotNull(user.EmailVerifiedUtc);
    }

    [Fact]
    public void User_Can_Be_SuperAdmin()
    {
        var user = new User { IsSuperAdmin = true };
        
        Assert.True(user.IsSuperAdmin);
    }

    [Fact]
    public void User_Can_Be_Guest()
    {
        var user = new User { IsGuest = true };
        
        Assert.True(user.IsGuest);
    }

    [Fact]
    public void User_Can_Have_Password_Reset_Token()
    {
        var user = new User
        {
            PasswordResetToken = "reset-token-123",
            PasswordResetTokenExpiresUtc = DateTime.UtcNow.AddHours(1)
        };
        
        Assert.Equal("reset-token-123", user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetTokenExpiresUtc);
        Assert.True(user.PasswordResetTokenExpiresUtc > DateTime.UtcNow);
    }

    [Fact]
    public void User_Can_Have_Verification_Token()
    {
        var user = new User
        {
            VerificationToken = "verify-token-456",
            VerificationTokenExpiresUtc = DateTime.UtcNow.AddDays(1)
        };
        
        Assert.Equal("verify-token-456", user.VerificationToken);
        Assert.NotNull(user.VerificationTokenExpiresUtc);
        Assert.True(user.VerificationTokenExpiresUtc > DateTime.UtcNow);
    }
}
