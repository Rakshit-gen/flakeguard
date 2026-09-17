using System.Text;
using FlakeGuard.Core.Services;
using Xunit;

namespace FlakeGuard.Tests.Services;

public class WebhookSignatureTests
{
    [Fact]
    public void ComputedSignature_VerifiesAgainstTheSamePayloadAndSecret()
    {
        var payload = Encoding.UTF8.GetBytes("""{"commitSha":"abc123"}""");
        var signature = WebhookSignature.Compute("my-secret", payload);

        Assert.True(WebhookSignature.Verify("my-secret", payload, signature));
    }

    [Fact]
    public void TamperedPayload_FailsVerification()
    {
        var original = Encoding.UTF8.GetBytes("""{"commitSha":"abc123"}""");
        var tampered = Encoding.UTF8.GetBytes("""{"commitSha":"xyz789"}""");
        var signature = WebhookSignature.Compute("my-secret", original);

        Assert.False(WebhookSignature.Verify("my-secret", tampered, signature));
    }

    [Fact]
    public void WrongSecret_FailsVerification()
    {
        var payload = Encoding.UTF8.GetBytes("""{"commitSha":"abc123"}""");
        var signature = WebhookSignature.Compute("my-secret", payload);

        Assert.False(WebhookSignature.Verify("a-different-secret", payload, signature));
    }

    [Fact]
    public void MissingSignatureHeader_FailsVerification()
    {
        var payload = Encoding.UTF8.GetBytes("""{"commitSha":"abc123"}""");

        Assert.False(WebhookSignature.Verify("my-secret", payload, providedSignature: null));
    }
}
