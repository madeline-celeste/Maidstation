namespace Content.Shared.Body.Components;

public sealed partial class BloodstreamComponent
{
    /// <summary>
    ///     [Woundmed]
    ///     Separated bleeding to base bleeding for simple mobs and abilities and bleeds
    ///     based on BleedInflictors from wounds
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedAmountFromWounds;

    /// <summary>
    ///     [Woundmed]
    ///     Separated bleeding to base bleeding for simple mobs and abilities and bleeds
    ///     based on BleedInflictors from wounds
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedAmountNotFromWounds;
}