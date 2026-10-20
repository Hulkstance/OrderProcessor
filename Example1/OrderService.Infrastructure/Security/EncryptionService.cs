using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace OrderService.Infrastructure.Security;

// https://stackoverflow.com/questions/38795103/encrypt-string-in-net-core
public sealed class EncryptionService(IOptions<SecurityOptions> options) : IEncryptionService
{
    private readonly string _encryptionKey = options.Value.EncryptionKey;

    public string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("The text must have valid value", nameof(text));

        var buffer = Encoding.UTF8.GetBytes(text);

        using var aes = GetAesKey(_encryptionKey);
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var resultStream = new MemoryStream();
        using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
        using (var plainStream = new MemoryStream(buffer))
        {
            plainStream.CopyTo(aesStream);
        }

        var result = resultStream.ToArray();
        var combined = new byte[aes.IV.Length + result.Length];
        Array.ConstrainedCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Array.ConstrainedCopy(result, 0, combined, aes.IV.Length, result.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("The encrypted text must have valid value", nameof(encryptedText));

        var combined = Convert.FromBase64String(encryptedText);
        var buffer = new byte[combined.Length];

        using var aes = GetAesKey(_encryptionKey);

        var iv = new byte[aes.IV.Length];
        var ciphertext = new byte[buffer.Length - iv.Length];

        Array.ConstrainedCopy(combined, 0, iv, 0, iv.Length);
        Array.ConstrainedCopy(combined, iv.Length, ciphertext, 0, ciphertext.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var resultStream = new MemoryStream();
        using (var aesStream = new CryptoStream(resultStream, decryptor, CryptoStreamMode.Write))
        using (var plainStream = new MemoryStream(ciphertext))
        {
            plainStream.CopyTo(aesStream);
        }

        return Encoding.UTF8.GetString(resultStream.ToArray());
    }

    private static Aes GetAesKey(string encryptionKey)
    {
        using var hash = SHA512.Create();
        var key = new byte[24];
        Buffer.BlockCopy(hash.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey)), 0, key, 0, 24);

        var aes = Aes.Create();
        aes.Key = key;

        return aes;
    }
}

public interface IEncryptionService
{
    string Encrypt(string text);
    string Decrypt(string encryptedText);
}
