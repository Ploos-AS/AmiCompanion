using AmiCompanion.Core;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class AppInfoTests
{
    [Fact]
    public void MilestoneIsM1() => Assert.Equal("M1", AppInfo.Milestone);

    [Fact]
    public void ProductNameIsStable() => Assert.Equal("AmiCompanion", AppInfo.Name);
}
