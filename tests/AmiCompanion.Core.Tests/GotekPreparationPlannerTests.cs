using AmiCompanion.Core.Gotek;
using Xunit;

namespace AmiCompanion.Core.Tests;

public sealed class GotekPreparationPlannerTests
{
    [Fact]
    public void FiltersSortsAndAssignsStableSlots()
    {
        var plan = GotekPreparationPlanner.Create(new[]
        {
            "zeta.HFE", "ignore.txt", "Alpha.adf", "beta.img"
        });

        Assert.Equal(3, plan.Count);
        Assert.Collection(plan.Images,
            x => { Assert.Equal(0, x.Slot); Assert.Equal("Alpha.adf", x.FileName); },
            x => { Assert.Equal(1, x.Slot); Assert.Equal("beta.img", x.FileName); },
            x => { Assert.Equal(2, x.Slot); Assert.Equal("zeta.HFE", x.FileName); });
    }

    [Fact]
    public void EmptyInputProducesEmptyPlan()
    {
        Assert.Empty(GotekPreparationPlanner.Create(Array.Empty<string>()).Images);
    }
}
