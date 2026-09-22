using AmiCompanion.Core.FileSystems;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosHashTests
{
    [Theory]
    [InlineData("Filename", 53)]
    [InlineData("file_1a", 0)]
    [InlineData("file_24", 0)]
    [InlineData("file_5u", 0)]
    public void MatchesKnownAmigaDosBuckets(string name, int expected) =>
        Assert.Equal(expected, AmigaDosHash.GetBucket(name));
    
    [Fact]
    public void HashIsCaseInsensitiveForAscii()
    {
        Assert.Equal(AmigaDosHash.GetBucket("Workbench"), AmigaDosHash.GetBucket("workbench"));
    }
}
