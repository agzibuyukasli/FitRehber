using System.Linq.Expressions;
using DietitianClinic.Business.Exceptions;
using DietitianClinic.Business.Interfaces;
using DietitianClinic.Business.Services;
using DietitianClinic.Entity.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using DataRepos = DietitianClinic.DataAccess.Repositories;

namespace DietitianClinic.Tests.Unit;

public class UserServiceTests
{
    private readonly Mock<DataRepos.IUnitOfWork>         _uow      = new();
    private readonly Mock<DataRepos.IRepository<User>>   _userRepo = new();
    private readonly Mock<ITokenService>       _tokens   = new();
    private readonly Mock<IPasswordService>    _passwords = new();

    private UserService CreateSut()
    {
        _uow.Setup(u => u.UserRepository).Returns((DataRepos.IRepository<User>)_userRepo.Object);
        return new UserService(
            _uow.Object,
            NullLogger<UserService>.Instance,
            _tokens.Object,
            _passwords.Object);
    }

    // ─── RegisterUserAsync ────────────────────────────────────────────

    [Fact]
    public async Task Register_ThrowsValidation_WhenEmailAlreadyExists()
    {
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User { Email = "taken@test.com" });

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.RegisterUserAsync("A", "B", "taken@test.com", "Pass@1", "", "", "", "Dietitian"));
    }

    [Fact]
    public async Task Register_ThrowsValidation_WhenPasswordIsEmpty()
    {
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.RegisterUserAsync("A", "B", "new@test.com", "", "", "", "", "Dietitian"));
    }

    [Fact]
    public async Task Register_ReturnsUserId_OnSuccess()
    {
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 42)
            .ReturnsAsync(42);
        _passwords.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hash");

        var sut    = CreateSut();
        var result = await sut.RegisterUserAsync("Ali", "Veli", "new@test.com", "Pass@1", "0500", "Beslenme", "LIC123", "Dietitian");

        Assert.Equal(42, result);
    }

    // ─── LoginAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_ThrowsNotFound_WhenUserDoesNotExist()
    {
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _passwords.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.LoginAsync("ghost@test.com", "pass"));
    }

    [Fact]
    public async Task Login_ThrowsUnauthorized_WhenAccountIsLocked()
    {
        var lockedUser = new User
        {
            Email         = "locked@test.com",
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(lockedUser);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync("locked@test.com", "pass"));
    }

    [Fact]
    public async Task Login_ThrowsUnauthorized_WhenUserIsDeleted()
    {
        var deletedUser = new User { Email = "del@test.com", IsDeleted = true, IsActive = true };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(deletedUser);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync("del@test.com", "pass"));
    }

    [Fact]
    public async Task Login_ThrowsUnauthorized_WhenUserIsInactive()
    {
        var inactiveUser = new User { Email = "inactive@test.com", IsActive = false };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(inactiveUser);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync("inactive@test.com", "pass"));
    }

    [Fact]
    public async Task Login_ThrowsUnauthorized_WhenPasswordIsWrong()
    {
        var user = new User { Email = "u@test.com", IsActive = true, PasswordHash = "hash" };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("wrongpass", "hash")).Returns(false);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync("u@test.com", "wrongpass"));
    }

    [Fact]
    public async Task Login_IncrementsFailedCount_WhenPasswordIsWrong()
    {
        var user = new User { Email = "u@test.com", IsActive = true, PasswordHash = "hash", AccessFailedCount = 1 };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var sut = CreateSut();
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.LoginAsync("u@test.com", "bad"));

        _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.AccessFailedCount == 2)), Times.Once);
    }

    [Fact]
    public async Task Login_LocksAccount_WhenMaxFailedAttemptsReached()
    {
        var user = new User
        {
            Email              = "u@test.com",
            IsActive           = true,
            PasswordHash       = "hash",
            AccessFailedCount  = 4   // bir sonraki hatalı giriş = 5 (max)
        };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var sut = CreateSut();
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.LoginAsync("u@test.com", "bad"));

        Assert.NotNull(user.LockoutEndUtc);
        Assert.True(user.LockoutEndUtc > DateTime.UtcNow);
        Assert.Equal(0, user.AccessFailedCount); // sıfırlanmış olmalı
    }

    [Fact]
    public async Task Login_ReturnsTokens_OnSuccess()
    {
        var user = new User
        {
            Id           = 1,
            Email        = "ok@test.com",
            IsActive     = true,
            PasswordHash = "hash",
            Role         = UserRole.Dietitian
        };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("correct", "hash")).Returns(true);
        _passwords.Setup(p => p.NeedsRehash("hash")).Returns(false);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);
        _tokens.Setup(t => t.GenerateTokenAsync(1, "ok@test.com", "Dietitian")).ReturnsAsync("jwt");
        _tokens.Setup(t => t.GenerateRefreshTokenAsync()).ReturnsAsync("refresh");

        var sut           = CreateSut();
        var (token, refr) = await sut.LoginAsync("ok@test.com", "correct");

        Assert.Equal("jwt",     token);
        Assert.Equal("refresh", refr);
    }

    [Fact]
    public async Task Login_RehashesPassword_WhenNeedsRehash()
    {
        var user = new User
        {
            Id           = 1,
            Email        = "ok@test.com",
            IsActive     = true,
            PasswordHash = "legacy_hash",
            Role         = UserRole.Dietitian
        };
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("pass", "legacy_hash")).Returns(true);
        _passwords.Setup(p => p.NeedsRehash("legacy_hash")).Returns(true);
        _passwords.Setup(p => p.HashPassword("pass")).Returns("bcrypt_hash");
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);
        _tokens.Setup(t => t.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("jwt");
        _tokens.Setup(t => t.GenerateRefreshTokenAsync()).ReturnsAsync("refresh");

        var sut = CreateSut();
        await sut.LoginAsync("ok@test.com", "pass");

        Assert.Equal("bcrypt_hash", user.PasswordHash);
    }

    // ─── ChangePasswordAsync ──────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ThrowsNotFound_WhenUserNotExists()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.ChangePasswordAsync(99, "old", "New@Pass1"));
    }

    [Fact]
    public async Task ChangePassword_ThrowsUnauthorized_WhenOldPasswordIsWrong()
    {
        var user = new User { Id = 1, PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("wrongold", "hash")).Returns(false);

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.ChangePasswordAsync(1, "wrongold", "New@Pass1"));
    }

    [Fact]
    public async Task ChangePassword_ThrowsValidation_WhenNewPasswordSameAsOld()
    {
        var user = new User { Id = 1, PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("same", "hash")).Returns(true);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangePasswordAsync(1, "same", "same"));
    }

    [Fact]
    public async Task ChangePassword_ThrowsValidation_WhenNewPasswordIsWeak()
    {
        var user = new User { Id = 1, PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("old", "hash")).Returns(true);
        _passwords
            .Setup(p => p.ValidatePasswordStrengthAsync("weak"))
            .ReturnsAsync((false, new List<string> { "Çok kısa." }));

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ChangePasswordAsync(1, "old", "weak"));
    }

    [Fact]
    public async Task ChangePassword_ReturnsTrue_OnSuccess()
    {
        var user = new User { Id = 1, PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwords.Setup(p => p.VerifyPassword("old", "hash")).Returns(true);
        _passwords
            .Setup(p => p.ValidatePasswordStrengthAsync("New@Pass1"))
            .ReturnsAsync((true, new List<string>()));
        _passwords.Setup(p => p.HashPassword("New@Pass1")).Returns("new_hash");
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.ChangePasswordAsync(1, "old", "New@Pass1");

        Assert.True(result);
        Assert.Equal("new_hash", user.PasswordHash);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEndUtc);
    }

    // ─── ResetPasswordByEmailAsync ────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ThrowsValidation_WhenEmailIsEmpty()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordByEmailAsync("", "New@Pass1"));
    }

    [Fact]
    public async Task ResetPassword_ThrowsValidation_WhenNewPasswordIsEmpty()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordByEmailAsync("u@test.com", ""));
    }

    [Fact]
    public async Task ResetPassword_ThrowsValidation_WhenPasswordIsWeak()
    {
        _passwords
            .Setup(p => p.ValidatePasswordStrengthAsync("weak"))
            .ReturnsAsync((false, new List<string> { "Çok kısa." }));

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordByEmailAsync("u@test.com", "weak"));
    }

    [Fact]
    public async Task ResetPassword_ThrowsNotFound_WhenUserNotExists()
    {
        _passwords
            .Setup(p => p.ValidatePasswordStrengthAsync("New@Pass1"))
            .ReturnsAsync((true, new List<string>()));
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.ResetPasswordByEmailAsync("ghost@test.com", "New@Pass1"));
    }

    [Fact]
    public async Task ResetPassword_ReturnsTrue_OnSuccess()
    {
        var user = new User { Email = "u@test.com", PasswordHash = "old_hash" };
        _passwords
            .Setup(p => p.ValidatePasswordStrengthAsync("New@Pass1"))
            .ReturnsAsync((true, new List<string>()));
        _userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        _passwords.Setup(p => p.HashPassword("New@Pass1")).Returns("new_hash");
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.ResetPasswordByEmailAsync("u@test.com", "New@Pass1");

        Assert.True(result);
        Assert.Equal("new_hash", user.PasswordHash);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEndUtc);
    }

    // ─── GetAllUsersAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers_WithNoFilter()
    {
        var users = new List<User>
        {
            new() { FirstName = "Zeynep", Role = UserRole.Dietitian },
            new() { FirstName = "Ali",    Role = UserRole.Admin }
        };
        _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var sut    = CreateSut();
        var result = await sut.GetAllUsersAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsSortedByFirstName()
    {
        var users = new List<User>
        {
            new() { FirstName = "Zeynep", Role = UserRole.Dietitian },
            new() { FirstName = "Ali",    Role = UserRole.Dietitian }
        };
        _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var sut    = CreateSut();
        var result = await sut.GetAllUsersAsync();

        Assert.Equal("Ali",    result[0].FirstName);
        Assert.Equal("Zeynep", result[1].FirstName);
    }

    [Fact]
    public async Task GetAllUsers_FiltersByRole()
    {
        var users = new List<User>
        {
            new() { FirstName = "Admin1", Role = UserRole.Admin },
            new() { FirstName = "Diet1",  Role = UserRole.Dietitian }
        };
        _userRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var sut    = CreateSut();
        var result = await sut.GetAllUsersAsync(roleFilter: (int)UserRole.Admin);

        Assert.Single(result);
        Assert.Equal("Admin1", result[0].FirstName);
    }

    // ─── UpdateUserAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_ThrowsNotFound_WhenUserNotExists()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateUserAsync(99, "A", "B", "0500", "spec", "lic"));
    }

    [Fact]
    public async Task UpdateUser_ReturnsTrue_AndUpdatesFields()
    {
        var user = new User { Id = 1, FirstName = "Eski", LastName = "Soyad" };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.UpdateUserAsync(1, "Yeni", "Ad", "0555", "Klinik Beslenme", "LIC999");

        Assert.True(result);
        Assert.Equal("Yeni", user.FirstName);
        Assert.Equal("Ad",   user.LastName);
        Assert.Equal("0555", user.Phone);
    }

    // ─── DeleteUserAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_ThrowsNotFound_WhenUserNotExists()
    {
        _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.DeleteUserAsync(99));
    }

    [Fact]
    public async Task DeleteUser_ReturnsTrue_OnSuccess()
    {
        var user = new User { Id = 1 };
        _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepo.Setup(r => r.DeleteAsync(user)).ReturnsAsync(1);

        var sut    = CreateSut();
        var result = await sut.DeleteUserAsync(1);

        Assert.True(result);
        _userRepo.Verify(r => r.DeleteAsync(user), Times.Once);
    }
}
