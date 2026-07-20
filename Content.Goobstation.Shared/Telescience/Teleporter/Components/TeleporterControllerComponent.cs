using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Telescience.Teleporter.Components;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class TeleporterControllerComponent : Component
{
    public static ProtoId<SourcePortPrototype> SourcePortStation = "TeleporterStationLink";

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedStation;
};