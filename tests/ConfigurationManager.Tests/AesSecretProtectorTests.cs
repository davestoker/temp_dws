using ConfigurationManager.Core.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConfigurationManager.Tests;

public class AesSecretProtectorTests
{
    [Fact]
    public void Protect_Then_Unprotect_RoundTripsOriginalValue()
    {
        var protector = TestSecretProtectorFactory.Create();

        var ciphertext = protector.Protect("my-secret-value");
        var plaintext = protector.Unprotect(ciphertext);

        Assert.Equal("my-secret-value", plaintext);
    }

    [Fact]
    public void Protect_DoesNotReturnThePlainTextValue()
    {
        var protector = TestSecretProtectorFactory.Create();

        var ciphertext = protector.Protect("my-secret-value");

        Assert.DoesNotContain("my-secret-value", ciphertext);
    }

    [Fact]
    public void Protect_ProducesDifferentCiphertextEachTime()
    {
        var protector = TestSecretProtectorFactory.Create();

        var first = protector.Protect("my-secret-value");
        var second = protector.Protect("my-secret-value");

        Assert.NotEqual(first, second); // random nonce per call
    }

    [Fact]
    public void Constructor_ThrowsWhenKeyIsMissing()
    {
        var options = Options.Create(new SecretProtectorOptions { EncryptionKeyBase64 = string.Empty });

        Assert.Throws<InvalidOperationException>(() => new AesSecretProtector(options));
    }

    [Fact]
    public void Constructor_ThrowsWhenKeyIsWrongLength()
    {
        var options = Options.Create(new SecretProtectorOptions { EncryptionKeyBase64 = Convert.ToBase64String(new byte[16]) });

        Assert.Throws<InvalidOperationException>(() => new AesSecretProtector(options));
    }
}
