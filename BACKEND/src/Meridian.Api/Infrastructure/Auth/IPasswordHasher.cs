namespace Meridian.Api.Infrastructure.Auth;

/// <summary>Abstracts password hashing so the algorithm can be swapped without touching services.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>BCrypt-based hasher. Passwords are never stored or logged in plain text.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // Work factor 12 is a sensible default balancing cost and security.
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
