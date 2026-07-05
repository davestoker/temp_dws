using System.Security.Cryptography;
using ConfigurationManager.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ConfigurationManager.Core.Services;

/// <summary>
/// AES-256-GCM implementation of <see cref="ISecretProtector"/> for prototype/local-dev use.
/// The payload format is: base64( nonce(12 bytes) || tag(16 bytes) || ciphertext ).
/// TODO(production): replace with a Key-Vault-backed key (Azure Key Vault + Managed Identity),
/// add key rotation/versioning, and consider Azure Data Protection APIs for key management.
/// This class never logs plain text or ciphertext.
/// </summary>
public class AesSecretProtector : ISecretProtector
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public AesSecretProtector(IOptions<SecretProtectorOptions> options)
    {
        var keyBase64 = options.Value.EncryptionKeyBase64;
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "Secrets:EncryptionKeyBase64 is not configured. Generate one with 'openssl rand -base64 32' " +
                "and set it in local.settings.json / appsettings (Key Vault in production).");
        }

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Secrets:EncryptionKeyBase64 must decode to exactly 32 bytes (AES-256).");
        }
    }

    public string Protect(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, payload, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(cipherBytes, 0, payload, NonceSizeBytes + TagSizeBytes, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);

        var payload = Convert.FromBase64String(protectedValue);
        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Protected value payload is too short to be valid.");
        }

        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var tag = payload.AsSpan(NonceSizeBytes, TagSizeBytes);
        var cipherBytes = payload.AsSpan(NonceSizeBytes + TagSizeBytes);
        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(_key, TagSizeBytes);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
