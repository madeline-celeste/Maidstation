using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Telescience.Teleporter.Components;

/// <summary>
/// Data and marker for a Teleporter Hub, used to generate the portal for a teleporter.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class TeleporterHubComponent : Component
{
    /// <summary>
    ///     Sound played on arriving to this portal, centered on the destination.
    ///     The arrival sound of the entered portal will play if the destination is not a portal.
    /// </summary>
    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    ///     Sound played on departing from this portal, centered on the original portal.
    /// </summary>
    [DataField]
    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    /// <summary>
    /// Success rate for the portal when not fully calibrated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UncalibratedSuccessRate = 0.8f;

    /// <summary>
    /// Success rate for the portal when fully calibrated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CalibratedSuccessRate = 1.0f;

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedStation;

    [DataField, AutoNetworkedField]
    public bool IsPortalActive;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTarget;
};