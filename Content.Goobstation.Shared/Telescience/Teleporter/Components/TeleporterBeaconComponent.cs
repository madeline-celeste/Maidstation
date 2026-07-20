using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Telescience.Teleporter.Components;

/// <summary>
/// Data and marker for a Teleporter Tracking Beacon, used for teleporter targets.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleporterBeaconComponent : Component
{
    /// <summary>
    /// Multiplier for the success rate when using this tracking beacon. When optimally calibrated, success rate will be this.
    /// // TODO
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SuccessRateModifier = 1.0f;
};