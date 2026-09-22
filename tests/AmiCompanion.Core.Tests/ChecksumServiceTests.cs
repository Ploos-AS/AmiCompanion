using System.Text;
using AmiCompanion.Core.Checksums;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class ChecksumServiceTests
{
    [Fact]
    public void EmptyInputMatchesKnownVectors()
    {
        var result = ChecksumService.Compute(ReadOnlySpan<byte>.Empty);
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", result.Sha256);
        Assert.Equal(0u, result.Crc32);
    }

    [Fact]
    public void StandardVectorMatches()
    {
        var result = ChecksumService.Compute(Encoding.ASCII.GetBytes("123456789"));
        Assert.Equal("15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225", result.Sha256);
        Assert.Equal(0xcbf43926u, result.Crc32);
    }
}
