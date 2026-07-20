using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Telescience.Teleporter.Components;

/// <summary>
/// Data and marker for a Teleporter Station, used to power the teleporter and store calibration data.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class TeleporterStationComponent : Component
{
    public static ProtoId<SourcePortPrototype> SourcePortHub = "TeleporterHubLink";

    [DataField, AutoNetworkedField]
    public EntityUid? CalibratedTarget;

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedHub;
};