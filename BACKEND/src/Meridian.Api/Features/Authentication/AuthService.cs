using System.Security.Cryptography;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.Authentication.Domain;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Email;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Authentication;

/// <summary>
/// Business logic for authentication and password lifecycle. All password
/// material is hashed with BCrypt and never logged; reset flows go through
/// single-use, time-limited tokens delivered by email.
/// </summary>
public sealed class AuthService : IAuthService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(2);

    private readonly MeridianDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailSender _email;
    private readonly IAuditLogger _audit;

    public AuthService(MeridianDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwt,
        IEmailSender email, IAuditLogger audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _email = email;
        _audit = audit;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync("LoginFailed", nameof(Domain), email, user?.Id, ct);
            // Uniform message avoids leaking whether the account exists.
            throw new UnauthorizedException("Invalid email or password.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var token = _jwt.CreateToken(user, roles);

        await _audit.LogAsync("LoginSucceeded", "User", user.Id.ToString(), user.Id, ct);

        return new LoginResponse(token.Token, token.ExpiresAt, user.Id, user.FullName, user.Email, roles, user.IsOnboarding);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Always behave the same way to prevent account enumeration.
        if (user is null || !user.IsActive) return;

        var token = new PasswordResetToken
        {
            UserId = user.Id,
            Token = GenerateToken(),
            ExpirationDate = DateTime.UtcNow.Add(ResetTokenLifetime)
        };
        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync(ct);

        await _email.SendAsync(user.Email, "Reset your Meridian password",
            $"Use the following token to reset your password (valid for 2 hours): {token.Token}", ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        PasswordPolicy.Validate(request.NewPassword);

        var token = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (token is null || !token.IsValid)
            throw new BusinessRuleException("The password reset token is invalid or has expired.");

        token.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        token.Used = true;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("PasswordReset", "User", token.UserId.ToString(), token.UserId, ct);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw NotFoundException.For("User", userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("The current password is incorrect.");

        PasswordPolicy.Validate(request.NewPassword);
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("PasswordChanged", "User", user.Id.ToString(), user.Id, ct);
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
