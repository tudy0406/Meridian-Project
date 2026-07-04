using System.Security.Cryptography;

namespace Meridian.Api.Infrastructure.Auth;

/// <summary>Generates a random temporary password that satisfies the password policy.</summary>
public static class TemporaryPassword
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    public static string Generate() => $"Aa1{RandomNumberGenerator.GetString(Chars, 10)}";
}
