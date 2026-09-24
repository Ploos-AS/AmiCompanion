using AmiCompanion.Core.DiskImages;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AmigaDosTypeTests
{
    [Theory]
    [InlineData(0x444f5300u, "OFS")]
    [InlineData(0x444f5301u, "FFS")]
    [InlineData(0x444f5303u, "FFS international")]
    [InlineData(0x444f5305u, "FFS directory cache")]
    [InlineData(0x50465301u, "PFS")]
    [InlineData(0x53465300u, "SFS")]
    public void DescribesKnownDosTypes(uint value, string expected) =>
        Assert.Equal(expected, AmigaDosType.Describe(value));

    [Fact]
    public void UnknownDosTypeStaysUnknown() =>
        Assert.Equal("unknown", AmigaDosType.Describe(0x12345678));
}
