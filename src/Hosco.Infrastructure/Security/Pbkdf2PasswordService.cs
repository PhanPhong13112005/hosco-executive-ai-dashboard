using System.Security.Cryptography;
using Hosco.Application.Abstractions;

namespace Hosco.Infrastructure.Security;

public sealed class Pbkdf2PasswordService : IPasswordVerifier
{
    private const int Iterations = 100_000;
    private const int SaltLength = 16;
    private const int KeyLength = 32;

    public bool Verify(string password, string encodedHash)
    {
        try
        {
            var parts = encodedHash.Split('.', 4);
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256") return false;
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }

    public static string HashForSeed(string password, Guid userId)
    {
        var salt = userId.ToByteArray()[..SaltLength];
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return $"PBKDF2-SHA256.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }
}
