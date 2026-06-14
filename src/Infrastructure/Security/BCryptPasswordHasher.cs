using Application.Common.Interfaces;
using BCrypt.Net;

namespace Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    // Work factor of 11 is a strong balance of security and speed (approx 100-200ms per hash)
    private const int WorkFactor = 11;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword);
    }
}
