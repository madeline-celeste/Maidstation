namespace Content.Shared.Body.Components;

public sealed partial class BloodstreamComponent
{
    /// <summary>
    ///     [Goob]
    ///     Prevents this entity from absorbing reagents from smoke/foam.
    /// </summary>
    [DataField]
    public bool SmokeImmune;
}
