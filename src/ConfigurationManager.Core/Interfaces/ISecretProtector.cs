namespace ConfigurationManager.Core.Interfaces;

/// <summary>
/// Isolates encryption of sensitive configuration values so the storage/implementation
/// can change (e.g. move to Azure Key Vault-backed keys) without touching callers.
/// TODO(production): replace the local AES key with a key managed in Azure Key Vault,
/// accessed via Managed Identity, with key rotation support.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypts plain text and returns a base64-encoded ciphertext payload safe to store in the database.</summary>
    string Protect(string plainText);

    /// <summary>Decrypts a payload previously produced by <see cref="Protect"/>.</summary>
    string Unprotect(string protectedValue);
}
