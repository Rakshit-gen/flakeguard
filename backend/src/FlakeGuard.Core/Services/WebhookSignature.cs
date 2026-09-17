using System.Security.Cryptography;
using System.Text;

namespace FlakeGuard.Core.Services;

/// <summary>HMAC-SHA256 request signing, mirroring GitHub's own X-Hub-Signature-256 scheme.</summary>
public static class WebhookSignature
{
    public static string Compute(string secret, byte[] payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(payload);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string secret, byte[] payload, string? providedSignature)
    {
        if (string.IsNullOrEmpty(providedSignature))
        {
            return false;
        }

        var expected = Compute(secret, payload);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(providedSignature);

        // Constant-time comparison: a length- or timing-dependent early-exit would let an
        // attacker recover the correct signature one byte at a time.
        return expectedBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
