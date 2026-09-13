// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

// Shitmed Change Start
using Content.Shared._Shitmed.Body.Components;
using Content.Shared._Shitmed.BodyEffects;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Shared.Random;

// Goobstation
using Content.Shared.Destructible;
using Content.Shared.Random.Helpers;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    public void InitializePartsWoundmed()
    {
        SubscribeLocalEvent<BodyPartComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BodyPartComponent, ComponentRemove>(OnBodyPartRemove);
        SubscribeLocalEvent<BodyPartComponent, DestructionEventArgs>(OnBodyPartDestructed);
    }

    private void OnBodyPartDestructed(Entity<BodyPartComponent> ent, ref DestructionEventArgs args)
    {
        GibPart(ent, ent.Comp);
    }

    private void OnMapInit(Entity<BodyPartComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.PartType == BodyPartType.Chest)
        {
            // For whatever reason this slot is initialized properly on the server, but not on the client.
            // This seems to be an issue due to wiz-merge, on my old branch it was properly instantiating
            // ItemInsertionSlot's container on both ends. It does show up properly on ItemSlotsComponent though.
            _slots.AddItemSlot(ent, ent.Comp.ContainerName, ent.Comp.ItemInsertionSlot);
            Dirty(ent, ent.Comp);
        }

        if (ent.Comp.OnAdd is not null || ent.Comp.OnRemove is not null)
            EnsureComp<BodyPartEffectComponent>(ent);

        foreach (var connection in ent.Comp.Children.Keys)
        {
            Containers.EnsureContainer<ContainerSlot>(ent, GetPartSlotContainerId(connection));
        }

        foreach (var organ in ent.Comp.Organs.Keys)
        {
            Containers.EnsureContainer<ContainerSlot>(ent, GetOrganContainerId(organ));
        }
    }

    private void OnBodyPartRemove(Entity<BodyPartComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.PartType == BodyPartType.Chest)
            _slots.RemoveItemSlot(ent, ent.Comp.ItemInsertionSlot);
    }

    /// <summary>
    ///     [Woundmed]
    ///     This function handles dropping the items in an entity's slots if they lose all of a given part.
    ///     Such as their hands, feet, head, etc.
    /// </summary>
    public void DropSlotContents(Entity<BodyPartComponent> partEnt)
    {
        if (partEnt.Comp.Body is null
            || !TryComp<InventoryComponent>(partEnt.Comp.Body, out var inventory) || // Prevent error for non-humanoids
            GetBodyPartCount(partEnt.Comp.Body.Value, partEnt.Comp.PartType) != 1
            || !TryGetPartSlotContainerName(partEnt.Comp.PartType, out var containerNames))
            return;

        foreach (var containerName in containerNames)
        {
            _inventory.DropSlotContents(partEnt.Comp.Body.Value, containerName, inventory);
        }

    }

    /// <summary>
    ///     [Woundmed]
    ///     Tries to get a list of ValueTuples of EntityUid and OrganComponent on each organ
    ///     in the given part.
    /// </summary>
    /// <param name="uid">The part entity id to check on.</param>
    /// <param name="type">The type of component to check for.</param>
    /// <param name="part">The part to check for organs on.</param>
    /// <param name="organs">The organs found on the body part.</param>
    /// <returns>Whether any were found.</returns>
    /// <remarks>
    ///     This method is somewhat of a copout to the fact that we can't use reflection to generically
    ///     get the type of component on runtime due to sandboxing. So we simply do a HasComp check for each organ.
    /// </remarks>
    public bool TryGetBodyPartOrgans(
        EntityUid uid,
        Type type,
        [NotNullWhen(true)] out List<(EntityUid Id, OrganComponent Organ)>? organs,
        BodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
        {
            organs = null;
            return false;
        }

        var list = new List<(EntityUid Id, OrganComponent Organ)>();

        foreach (var organ in GetPartOrgans(uid, part))
        {
            if (HasComp(organ.Id, type))
                list.Add((organ.Id, organ.Component));
        }

        if (list.Count != 0)
        {
            organs = list;
            return true;
        }

        organs = null;
        return false;
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="partType"></param>
    /// <param name="containerNames"></param>
    /// <returns></returns>
    public bool TryGetPartSlotContainerName(BodyPartType partType, out HashSet<string> containerNames)
    {
        containerNames = partType switch
        {
            BodyPartType.Hand => ["gloves"],
            BodyPartType.Foot => ["shoes"],
            BodyPartType.Head => ["eyes", "ears", "head", "mask"],
            _ => [],
        };
        return containerNames.Count > 0;
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="partType"></param>
    /// <returns></returns>
    public bool TryGetPartFromSlotContainer(string slot, [NotNullWhen(true)] out BodyPartType? partType)
    {
        partType = slot switch
        {
            "innerclothing" or "outerclothing" => BodyPartType.Chest,
            "gloves" => BodyPartType.Hand,
            "shoes" => BodyPartType.Foot,
            "eyes" or "ears" or "head" or "mask" => BodyPartType.Head,
            _ => null,
        };
        return partType is not null;
    }

    // John Linq strikes again.
    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="bodyId"></param>
    /// <param name="partType"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public int GetBodyPartCount(EntityUid bodyId, BodyPartType partType, BodyComponent? body = null)
    {
        return !Resolve(bodyId, ref body, logMissing: false) ? 0 : GetBodyChildren(bodyId, body).Count(part => part.Component.PartType == partType);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="bodyId"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public float GetVitalBodyPartRatio(EntityUid bodyId, BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, logMissing: false))
            return 1f;

        var children = GetBodyChildren(bodyId, body);
        var vitalCount = 0;
        var count = 0;
        foreach (var child in children)
        {
            count++;
            if ((int) (child.Component.PartType & BodyPartType.Vital) != 0)
                vitalCount++;
        }

        if (vitalCount == 0)
            return 1f;

        return (float) count / vitalCount;
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="part"></param>
    /// <returns></returns>
    public string GetSlotFromBodyPart(BodyPartComponent? part)
    {
        var slotName = "";

        if (part is null)
            return slotName;

        slotName = part.SlotId != "" ? part.SlotId : part.PartType.ToString().ToLower();
        return part.Symmetry != BodyPartSymmetry.None ? $"{part.Symmetry.ToString().ToLower()} {slotName}" : slotName;
    }

    /// <summary>
    ///     [Woundmed]
    ///     Returns true if the partId can be detached from the parentId in the specified slot.
    /// </summary>
    public bool CanDetachPart(
        EntityUid parentId,
        BodyPartSlot slot,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part, logMissing: false)
               && Resolve(parentId, ref parentPart, logMissing: false)
               && CanDetachPart(parentId, slot.Id, partId, parentPart, part);
    }

    /// <summary>
    ///     [Woundmed]
    ///     Returns true if we can detach the specified partId from the parentId in the specified slot.
    /// </summary>
    public bool CanDetachPart(
        EntityUid parentId,
        string slotId,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part, logMissing: false)
               && Resolve(parentId, ref parentPart, logMissing: false)
               && parentPart.Children.TryGetValue(slotId, out var parentSlotData)
               && part.PartType == parentSlotData.Type
               && Containers.TryGetContainer(parentId, GetPartSlotContainerId(slotId), out var container)
               && Containers.CanRemove(partId, container);
    }

    /// <summary>
    ///     [Woundmed]
    ///     Tries find parent body part and detaches a partId part.
    /// </summary>
    public bool TryDetachPart(
        EntityUid partId,
        BodyPartComponent? part = null)
    {
        var parentTuple = GetParentPartAndSlotOrNull(partId);
        if (parentTuple is null)
            return false;

        var (parentPartId, slot) = parentTuple ?? default;

        return DetachPart(parentPartId, slot, partId, null, part);
    }

    /// <summary>
    ///     [Woundmed]
    ///     Detaches a body part from the specified body part parent.
    /// </summary>
    public bool DetachPart(
        EntityUid parentPartId,
        string slotId,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        return Resolve(parentPartId, ref parentPart, logMissing: false)
               && parentPart.Children.TryGetValue(slotId, out var slot)
               && DetachPart(parentPartId, slot, partId, parentPart, part);
    }

    /// <summary>
    ///     [Woundmed]
    ///     Detaches a body part from the specified body part parent.
    /// </summary>
    public bool DetachPart(
        EntityUid parentPartId,
        BodyPartSlot slot,
        EntityUid partId,
        BodyPartComponent? parentPart = null,
        BodyPartComponent? part = null)
    {
        if (!Resolve(parentPartId, ref parentPart, logMissing: false)
            || !Resolve(partId, ref part, logMissing: false)
            || !CanDetachPart(parentPartId, slot.Id, partId, parentPart, part)
            || !parentPart.Children.ContainsKey(slot.Id))
        {
            return false;
        }

        if (!Containers.TryGetContainer(parentPartId, GetPartSlotContainerId(slot.Id), out var container))
        {
            DebugTools.Assert($"Unable to find body slot {slot.Id} for {ToPrettyString(parentPartId)}");
            return false;
        }

        // TODO: Might break something. but fixes surgery!
        //parentPart.Children.Remove(slot.Id);

        // start-backmen: surgery
        return Containers.Remove(partId, container);
    }

    /// <summary>
    ///     [Woundmed]
    ///     This override fetches a random body part for an entity based on the attacker's selected part, which introduces a random chance to miss
    ///     so long as the entity isnt incapacitated or laying down.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="attacker"></param>
    /// <param name="targetComp"></param>
    /// <param name="attackerComp"></param>
    /// <returns></returns>
    public TargetBodyPart? GetRandomBodyPart(EntityUid target,
        EntityUid attacker,
        TargetingComponent? targetComp = null,
        TargetingComponent? attackerComp = null)
    {
        if (!Resolve(target, ref targetComp, false)
            || !Resolve(attacker, ref attackerComp, false))
            return TargetBodyPart.Chest;

        if (_mobState.IsIncapacitated(target)
            || Standing.IsDown(target))
            return attackerComp.Target;

        var totalWeight = targetComp.TargetOdds[attackerComp.Target].Values.Sum();
        // i think this is the way to do predicted random
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(target));
        var randomValue = random.NextFloat() * totalWeight;

        foreach (var (part, weight) in targetComp.TargetOdds[attackerComp.Target])
        {
            if (randomValue <= weight)
                return part;
            randomValue -= weight;
        }

        return TargetBodyPart.Chest; // Default to torso if something goes wrong
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="target"></param>
    /// <param name="targetPart"></param>
    /// <param name="targetComp"></param>
    /// <returns></returns>
    public TargetBodyPart GetRandomBodyPart(EntityUid target,
        TargetBodyPart targetPart = TargetBodyPart.Chest,
        TargetingComponent? targetComp = null)
    {
        if (!Resolve(target, ref targetComp, false))
            return TargetBodyPart.Chest;

        if (_mobState.IsIncapacitated(target)
            || Standing.IsDown(target))
            return targetPart;

        var totalWeight = targetComp.TargetOdds[targetPart].Values.Sum();

        // i think this is the way to do predicted random
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(target));
        var randomValue = random.NextFloat() * totalWeight;

        foreach (var (part, weight) in targetComp.TargetOdds[targetPart])
        {
            if (randomValue <= weight)
                return part;
            randomValue -= weight;
        }

        return targetPart;
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public TargetBodyPart GetRandomBodyPart(EntityUid target)
    {
        var children = GetVitalBodyChildren(target).ToList(); // Goobstation
        if (children.Count == 0)
            return TargetBodyPart.Chest;

        // i think this is the way to do predicted random
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(target));

        return GetTargetBodyPart(random.PickAndTake(children));
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="target"></param>
    /// <param name="attacker"></param>
    /// <param name="targetPart"></param>
    /// <param name="targetComp"></param>
    /// <returns></returns>
    public TargetBodyPart GetRandomBodyPart(EntityUid target,
        EntityUid? attacker,
        TargetBodyPart? targetPart = null,
        TargetingComponent? targetComp = null)
    {
        if (!Resolve(target, ref targetComp, false))
            return TargetBodyPart.Chest;

        if (targetPart.HasValue)
            return GetRandomBodyPart(target, targetPart: targetPart.Value);

        if (attacker.HasValue
            && TryComp(attacker.Value, out TargetingComponent? attackerComp))
            return GetRandomBodyPart(target, targetPart: attackerComp.Target);

        return GetRandomBodyPart(target);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="target"></param>
    /// <param name="attacker"></param>
    /// <param name="targetPart"></param>
    /// <param name="targetComp"></param>
    /// <returns></returns>
    public TargetBodyPart GetTargetBodyPart(EntityUid target,
        EntityUid? attacker,
        TargetBodyPart? targetPart = null,
        TargetingComponent? targetComp = null)
    {
        if (!Resolve(target, ref targetComp, false))
            return TargetBodyPart.Chest;

        if (targetPart.HasValue)
            return targetPart.Value;

        if (attacker.HasValue
            && TryComp(attacker.Value, out TargetingComponent? attackerComp))
            return attackerComp.Target;

        return GetRandomBodyPart(target);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="partId"></param>
    /// <returns></returns>
    [Obsolete("Use (BodyPartType, BodyPartSymmetry) syntax.")]
    public TargetBodyPart GetTargetBodyPart(EntityUid partId)
    {
        if (!TryComp(partId, out BodyPartComponent? part))
            return TargetBodyPart.Chest;

        return GetTargetBodyPart(part);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="part"></param>
    /// <returns></returns>
    [Obsolete("Use (BodyPartType, BodyPartSymmetry) syntax.")]
    public TargetBodyPart GetTargetBodyPart(Entity<BodyPartComponent> part)
    {
        return GetTargetBodyPart(part.Comp.PartType, part.Comp.Symmetry);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="part"></param>
    /// <returns></returns>
    [Obsolete("Use (BodyPartType, BodyPartSymmetry) syntax.")]
    public TargetBodyPart GetTargetBodyPart(BodyPartComponent part)
    {
        return GetTargetBodyPart(part.PartType, part.Symmetry);
    }

    /// <summary>
    ///     [Woundmed]
    ///     Converts Enums from BodyPartType to their Targeting system equivalent.
    /// </summary>
    public TargetBodyPart GetTargetBodyPart(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return (type, symmetry) switch
        {
            (BodyPartType.Head, _) => TargetBodyPart.Head,
            (BodyPartType.Chest, _) => TargetBodyPart.Chest,
            (BodyPartType.Groin, _) => TargetBodyPart.Groin,
            (BodyPartType.Arm, BodyPartSymmetry.Left) => TargetBodyPart.LeftArm,
            (BodyPartType.Arm, BodyPartSymmetry.Right) => TargetBodyPart.RightArm,
            (BodyPartType.Hand, BodyPartSymmetry.Left) => TargetBodyPart.LeftHand,
            (BodyPartType.Hand, BodyPartSymmetry.Right) => TargetBodyPart.RightHand,
            (BodyPartType.Leg, BodyPartSymmetry.Left) => TargetBodyPart.LeftLeg,
            (BodyPartType.Leg, BodyPartSymmetry.Right) => TargetBodyPart.RightLeg,
            (BodyPartType.Foot, BodyPartSymmetry.Left) => TargetBodyPart.LeftFoot,
            (BodyPartType.Foot, BodyPartSymmetry.Right) => TargetBodyPart.RightFoot,
            _ => TargetBodyPart.Chest,
        };
    }

    /// <summary>
    ///     [Woundmed]
    ///     Converts Enums from Targeting system to their BodyPartType equivalent.
    /// </summary>
    public (BodyPartType Type, BodyPartSymmetry Symmetry) ConvertTargetBodyPart(TargetBodyPart? targetPart)
    {
        return targetPart switch
        {
            TargetBodyPart.Head => (BodyPartType.Head, BodyPartSymmetry.None),
            TargetBodyPart.Chest => (BodyPartType.Chest, BodyPartSymmetry.None),
            TargetBodyPart.Groin => (BodyPartType.Groin, BodyPartSymmetry.None),
            TargetBodyPart.LeftArm => (BodyPartType.Arm, BodyPartSymmetry.Left),
            TargetBodyPart.LeftHand => (BodyPartType.Hand, BodyPartSymmetry.Left),
            TargetBodyPart.RightArm => (BodyPartType.Arm, BodyPartSymmetry.Right),
            TargetBodyPart.RightHand => (BodyPartType.Hand, BodyPartSymmetry.Right),
            TargetBodyPart.LeftLeg => (BodyPartType.Leg, BodyPartSymmetry.Left),
            TargetBodyPart.LeftFoot => (BodyPartType.Foot, BodyPartSymmetry.Left),
            TargetBodyPart.RightLeg => (BodyPartType.Leg, BodyPartSymmetry.Right),
            TargetBodyPart.RightFoot => (BodyPartType.Foot, BodyPartSymmetry.Right),
            _ => (BodyPartType.Chest, BodyPartSymmetry.None)
        };

    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bodyId"></param>
    /// <param name="type"></param>
    /// <param name="body"></param>
    /// <param name="symmetry"></param>
    /// <returns></returns>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component, T ExtraComponent)> GetBodyChildrenOfTypeWithComponent<T>(
        EntityUid bodyId,
        BodyPartType type,
        BodyComponent? body = null,
        BodyPartSymmetry? symmetry = null)
        where T : IComponent
    {
        var query = GetEntityQuery<T>();

        foreach (var part in GetBodyChildren(bodyId, body))
        {
            if (part.Component.PartType == type
                && (symmetry == null || part.Component.Symmetry == symmetry)
                && query.TryGetComponent(part.Id, out var extraComponent))
            {
                yield return (part.Id, part.Component, extraComponent);
            }
        }
    }

    /// <summary>
    ///     [Woundmed Overload]
    ///     Edited version of this that uses Entity<T> syntax and out var
    ///     Returns the root part of this body if it exists.
    /// </summary>
    public bool TryGetRootPart(EntityUid bodyId, [NotNullWhen(true)] out Entity<BodyPartComponent>? rootPart, BodyComponent? body = null)
    {
        rootPart = null;
        if (!Resolve(bodyId, ref body)
            || body.RootContainer?.ContainedEntity is not { } rootContainedEntity
            || !TryComp<BodyPartComponent>(rootContainedEntity, out var bodyPartComponent))
            return false;

        rootPart = (rootContainedEntity, bodyPartComponent);
        return true;
    }

    /// <summary>
    ///     [Woundmed]
    /// Returns true if this parentId supports attaching a new part to the specified slot.
    /// </summary>
    public bool CanAttachToSlot(
        EntityUid parentId,
        string slotId,
        BodyPartComponent? parentPart = null)
    {
        return Resolve(parentId, ref parentPart, logMissing: false)
            && parentPart.Children.ContainsKey(slotId);
    }
}