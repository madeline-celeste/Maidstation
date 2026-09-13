// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

// Shitmed Change
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared._Shitmed.Medical.Surgery.Tools;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Robust.Shared.Audio;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// [Access(typeof(SharedBodySystem))] // Shitmed Change - no explicit access
public sealed partial class OrganComponent : Component, ISurgeryToolComponent // Shitmed Change
{
    /// <summary>
    /// Relevant body this organ is attached to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;
}
