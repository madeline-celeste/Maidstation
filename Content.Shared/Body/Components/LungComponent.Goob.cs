namespace Content.Shared.Body.Components;

public sealed partial class LungComponent : Component
{
    /// <summary>
    ///     [Goob]
    ///     Multiplier on saturation passively lost.
    ///     Higher values require more air, lower require less.
    /// </summary>
    [DataField]
    public float SaturationLoss = 1f;
}