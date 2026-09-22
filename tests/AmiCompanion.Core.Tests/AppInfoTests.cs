using AmiCompanion.Core;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AppInfoTests
{
    [Fact]
    public void MilestoneIsM0() => Assert.Equal("M0", AppInfo.Milestone);

    [Fact]
    public void ProductNameIsStable() => Assert.Equal("AmiCompanion", AppInfo.Name);
}
