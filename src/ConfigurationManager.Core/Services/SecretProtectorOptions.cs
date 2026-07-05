namespace ConfigurationManager.Core.Services;

/// <summary>
/// Configuration for <see cref="AesSecretProtector"/>.
/// TODO(production): source <see cref="EncryptionKeyBase64"/> from Azure Key Vault via
/// Managed Identity instead of app settings / local.settings.json, and support key rotation
/// (e.g. a key id prefix on the stored payload so old ciphertext can still be decrypted).
/// </summary>
public class SecretProtectorOptions
{
    /// <summary>A base64-encoded 256-bit (32 byte) AES key. Generate with e.g. openssl rand -base64 32.</summary>
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
}
