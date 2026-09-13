using Content.Goobstation.Common.Body;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Configuration;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly WoundSystem _woundSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly CommonInsideBodyPartSystem _insideBodyPart = default!;

    public void InitializeWoundmed()
    {
        InitializePartAppearances();
        InitializeRelay();
    }
}
