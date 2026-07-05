using ConfigurationManager.Core.Services;
using Microsoft.Extensions.Options;

namespace ConfigurationManager.Tests;

internal static class TestSecretProtectorFactory
{
    // Fixed test-only key - never used outside unit tests.
    private const string TestKeyBase64 = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=";

    public static AesSecretProtector Create() =>
        new(Options.Create(new SecretProtectorOptions { EncryptionKeyBase64 = TestKeyBase64 }));
}
