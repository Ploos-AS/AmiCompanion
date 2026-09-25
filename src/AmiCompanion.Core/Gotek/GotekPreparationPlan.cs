namespace AmiCompanion.Core.Gotek;

public sealed record GotekPreparationPlan(IReadOnlyList<GotekImageEntry> Images)
{
    public int Count => Images.Count;
}
