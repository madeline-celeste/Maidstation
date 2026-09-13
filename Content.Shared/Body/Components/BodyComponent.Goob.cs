using Content.Shared._Shitmed.Body;

namespace Content.Shared.Body.Components;

public sealed partial class BodyComponent : Component
{
    /// <summary>
    ///     [Goob]
    ///     Whether this body should be visible with thermal vision.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ThermalVisibility = true;
}