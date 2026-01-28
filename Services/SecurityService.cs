using System.Security.Cryptography;
using MauiApp1.Services;

namespace MauiApp1.Services;

public class SecurityService
{
    private readonly DatabaseService _db;

    public SecurityService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<bool> HasPinAsync()
    {
        var hash = await _db.GetSettingAsync("PinHash");
        return !string.IsNullOrWhiteSpace(hash);
    }

    public async Task SetPinAsync(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Hash(pin, salt);

        await _db.SetSettingAsync("PinSalt", Convert.ToBase64String(salt));
        await _db.SetSettingAsync("PinHash", Convert.ToBase64String(hash));
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var saltBase64 = await _db.GetSettingAsync("PinSalt");
        var hashBase64 = await _db.GetSettingAsync("PinHash");

        if (saltBase64 == null || hashBase64 == null) return false;

        var salt = Convert.FromBase64String(saltBase64);
        var expected = Convert.FromBase64String(hashBase64);
        var actual = Hash(pin, salt);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static byte[] Hash(string pin, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
}
