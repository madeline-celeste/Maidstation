using System.Linq;
using System.Numerics;
using Content.Shared._Shitmed.Body.Part;
using Content.Shared._Shitmed.CCVar;
using Content.Shared._Shitmed.Humanoid.Events;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Gibbing.Events;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Pulling.Events;
using Content.Shared.Rejuvenate;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeBodyWoundmed()
    {
        SubscribeLocalEvent<BodyComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<BodyComponent, ProfileLoadFinishedEvent>(OnProfileLoadFinished);
        SubscribeLocalEvent<BodyComponent, IsEquippingAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<BodyComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);

        // to prevent people from falling immediately as rejuvenated
        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnProfileLoadFinished(EntityUid uid, BodyComponent component, ProfileLoadFinishedEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            || TerminatingOrDeleted(uid)
            || !Initialized(uid))
            return;

        foreach (var part in GetBodyChildren(uid, component))
            EnsureComp<BodyPartAppearanceComponent>(part.Id);

        humanoid.ProfileLoaded = true;
        Dirty(uid, humanoid);
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnStandAttempt(Entity<BodyComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.LegEntities.Count < ent.Comp.RequiredLegs)
            args.Cancel();
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnBeingEquippedAttempt(Entity<BodyComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (!TryComp(args.EquipTarget, out BodyComponent? targetBody)
            || targetBody.Prototype == null
            || HasComp<BorgChassisComponent>(args.EquipTarget))
            return;

        if (TryGetPartFromSlotContainer(args.Slot, out var bodyPart)
            && bodyPart is not null)
        {
            var bodyPartString = bodyPart.Value.ToString().ToLower();
            var prototype = Prototypes.Index(targetBody.Prototype.Value);
            var hasPartConnection = prototype.Slots.Values.Any(slot =>
                slot.Connections.Contains(bodyPartString));

            if (hasPartConnection
                && !GetBodyChildrenOfType(args.EquipTarget, bodyPart.Value).Any())
            {
                _popup.PopupClient(Loc.GetString("equip-part-missing-error",
                    ("target", args.EquipTarget), ("part", bodyPartString)), args.Equipee, args.Equipee);
                args.Cancel();
            }
        }
    }

    private void OnAttemptStopPulling(Entity<BodyComponent> ent, ref AttemptStopPullingEvent args)
    {
        if (args.User == null || !Exists(args.User.Value))
            return;

        if (args.User.Value != ent.Owner)
            return;

        if (ent.Comp.LegEntities.Count > 0 || ent.Comp.RequiredLegs == 0)
            return;

        args.Cancelled = true;
    }

    private void OnRejuvenate(EntityUid ent, BodyComponent body, ref RejuvenateEvent args)
    {
        RestoreBody((ent, body)); // Goobstation
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="id"></param>
    /// <param name="body"></param>
    /// <param name="rootPart"></param>
    /// <returns></returns>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetVitalBodyChildren(
        EntityUid? id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id is null
            || !Resolve(id.Value, ref body, logMissing: false)
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        foreach (var child in GetBodyPartChildren(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            if ((int) (child.Component.PartType & BodyPartType.Vital) != 0)
                yield return child;
        }
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="partId"></param>
    /// <param name="part"></param>
    /// <param name="launchGibs"></param>
    /// <param name="splatDirection"></param>
    /// <param name="splatModifier"></param>
    /// <param name="splatCone"></param>
    /// <param name="gibSoundOverride"></param>
    /// <returns></returns>
    public virtual HashSet<EntityUid> GibPart(
        EntityUid partId,
        BodyPartComponent? part = null,
        bool launchGibs = true,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null)
    {
        var gibs = new HashSet<EntityUid>();

        if (!Resolve(partId, ref part, logMissing: false)
            || part.Body is not null)
            return gibs;

        _gibbingSystem.TryGibEntityWithRef(partId, partId, GibType.Gib, GibContentsOption.Drop, ref gibs,
                playAudio: true, launchGibs: true, launchDirection: splatDirection, launchImpulse: GibletLaunchImpulse * splatModifier,
                launchImpulseVariance: GibletLaunchImpulseVariance, launchCone: splatCone);

        if (HasComp<InventoryComponent>(partId))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(partId))
            {
                SharedTransform.AttachToGridOrMap(item);
                gibs.Add(item);
            }
        }
        _audioSystem.PlayPredicted(gibSoundOverride, Transform(partId).Coordinates, null);
        return gibs;
    }

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    /// <param name="partId"></param>
    /// <param name="part"></param>
    /// <returns></returns>
    public virtual bool BurnPart(EntityUid partId,
        BodyPartComponent? part = null)
    {
        if (!Resolve(partId, ref part, logMissing: false))
            return false;

        if (part.Body is { } bodyEnt)
        {
            if (IsPartRoot(bodyEnt, partId, part: part))
                return false;

            DropSlotContents((partId, part));
            QueueDel(partId);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     [Woundmed]
    ///     This is the evil master function for woundmed stuff rejuvenation
    /// </summary>
    /// <param name="entity"></param>
    public void RestoreBody(Entity<BodyComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        var ent = entity.Owner;
        var body = entity.Comp;

        if (body.Prototype == null)
            return;

        var prototype = Prototypes.Index(body.Prototype.Value);

        if (!TryGetRootPart(ent, out var rootPart))
            return;

        var rootSlot = prototype.Root;
        foreach (var organ in prototype.Slots[rootSlot].Organs)
        {
            if (!Containers.TryGetContainer(rootPart.Value.Owner, GetOrganContainerId(organ.Key), out var organContainer))
                continue;

            var organEnt = organContainer.ContainedEntities.FirstOrNull();
            if (organEnt != null)
            {
                foreach (var modifier in Comp<OrganComponent>(organEnt.Value).IntegrityModifiers)
                {
                    _trauma.TryRemoveOrganDamageModifier(organEnt.Value, modifier.Key.Item2, modifier.Key.Item1);
                }
            }
            else
            {
                SpawnInContainerOrDrop(organ.Value, rootPart.Value.Owner, GetOrganContainerId(organ.Key));
            }
        }

        Dirty(rootPart.Value.Owner, rootPart.Value.Comp);

        var frontier = new Queue<string>();
        frontier.Enqueue(rootSlot);

        var cameFrom = new Dictionary<string, string>();
        cameFrom[rootSlot] = rootSlot;

        var cameFromEntities = new Dictionary<string, EntityUid>();
        cameFromEntities[rootSlot] = rootPart.Value.Owner;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                if (!cameFrom.TryAdd(connection, currentSlotId))
                    continue;

                var connectionSlot = prototype.Slots[connection];
                var parentEntity = cameFromEntities[currentSlotId];
                var parentPartComponent = Comp<BodyPartComponent>(parentEntity);

                if (Containers.TryGetContainer(parentEntity, GetPartSlotContainerId(connection), out var container))
                {
                    if (container.ContainedEntities.Count > 0)
                    {
                        var containedEnt = container.ContainedEntities[0];
                        var containedPartComp = Comp<BodyPartComponent>(containedEnt);
                        cameFromEntities[connection] = containedEnt;

                        foreach (var organ in connectionSlot.Organs)
                        {
                            if (Containers.TryGetContainer(containedEnt, GetOrganContainerId(organ.Key), out var organContainer))
                            {
                                var organEnt = organContainer.ContainedEntities.FirstOrNull();
                                if (organEnt != null)
                                {
                                    foreach (var modifier in Comp<OrganComponent>(organEnt.Value).IntegrityModifiers)
                                    {
                                        _trauma.TryRemoveOrganDamageModifier(organEnt.Value, modifier.Key.Item2, modifier.Key.Item1);
                                    }
                                }
                                else
                                {
                                    SpawnInContainerOrDrop(organ.Value, containedEnt, GetOrganContainerId(organ.Key));
                                }
                            }
                            else
                            {
                                var slot = CreateOrganSlot((containedEnt, containedPartComp), organ.Key);
                                SpawnInContainerOrDrop(organ.Value, containedEnt, GetOrganContainerId(organ.Key));

                                if (slot is null)
                                {
                                    Log.Error($"Could not create organ for slot {organ.Key} in {ToPrettyString(ent)}");
                                }
                            }
                        }
                    }
                    else
                    {
                        var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                        cameFromEntities[connection] = childPart;

                        var childPartComponent = Comp<BodyPartComponent>(childPart);

                        var partSlot = new BodyPartSlot(connection, childPartComponent.PartType, childPartComponent.Symmetry);
                        childPartComponent.ParentSlot = partSlot;
                        parentPartComponent.Children.TryAdd(connection, partSlot);

                        Dirty(parentEntity, parentPartComponent);
                        Dirty(childPart, childPartComponent);

                        Containers.Insert(childPart, container);

                        SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                    }
                }
                else
                {
                    var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                    cameFromEntities[connection] = childPart;

                    var childPartComponent = Comp<BodyPartComponent>(childPart);

                    var partSlot = CreatePartSlot(parentEntity, connection, childPartComponent.PartType, childPartComponent.Symmetry, parentPartComponent);
                    childPartComponent.ParentSlot = partSlot;

                    Dirty(parentEntity, parentPartComponent);
                    Dirty(childPart, childPartComponent);

                    if (partSlot is null)
                    {
                        Log.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                        QueueDel(childPart);
                        continue;
                    }

                    container = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));
                    Containers.Insert(childPart, container);

                    SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                }

                frontier.Enqueue(connection);
            }
        }


        if (_trauma.TryGetBodyTraumas(ent, out var traumas, bodyComp: body))
            foreach (var trauma in traumas)
                _trauma.RemoveTrauma(trauma);

        foreach (var bodyPart in GetBodyChildren(ent, body))
        {
            if (!TryComp<WoundableComponent>(bodyPart.Id, out var woundable))
                continue;

            var bone = woundable.Bone.ContainedEntities.FirstOrNull();
            if (TryComp<BoneComponent>(bone, out var boneComp))
                _trauma.SetBoneIntegrity(bone.Value, boneComp.IntegrityCap, boneComp);

            _woundSystem.TryHaltAllBleeding(bodyPart.Id, woundable);
            _woundSystem.ForceHealWoundsOnWoundable(bodyPart.Id, out _);
        }
    }

    /// <summary>
    ///     [Woundmed]
    ///     Gets all child body parts of this entity that have component T, including the root entity if it has component T.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent BodyPart, T Component)> GetBodyChildrenWithComponent<T>(
        EntityUid? id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
        where T : IComponent
    {
        if (id is null
            || !Resolve(id.Value, ref body, logMissing: false)
            || body is null
            || body.RootContainer == null
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        foreach (var child in GetBodyPartChildrenWithComponent<T>(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return child;
        }
    }

    /// <summary>
    ///     [Woundmed]
    ///     Returns all body part components for this entity including itself that have component T.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent BodyPart, T Component)> GetBodyPartChildrenWithComponent<T>(
        EntityUid partId,
        BodyPartComponent? part = null)
        where T : IComponent
    {
        if (!Resolve(partId, ref part, logMissing: false))
            yield break;

        var query = GetEntityQuery<T>();

        // Check if the current part has the component
        if (query.TryGetComponent(partId, out var component))
            yield return (partId, part, component);

        foreach (var slotId in part.Children.Keys)
        {
            var containerSlotId = GetPartSlotContainerId(slotId);

            if (Containers.TryGetContainer(partId, containerSlotId, out var container))
            {
                foreach (var containedEnt in container.ContainedEntities)
                {
                    if (!TryComp(containedEnt, out BodyPartComponent? childPart))
                        continue;

                    foreach (var value in GetBodyPartChildrenWithComponent<T>(containedEnt, childPart))
                    {
                        yield return value;
                    }
                }
            }
        }
    }
}