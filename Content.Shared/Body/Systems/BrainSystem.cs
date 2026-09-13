using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;

namespace Content.Shared.Body.Systems
{
    public sealed partial class BrainSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly SharedBodySystem _bodySystem = default!; // Shitmed Change
        public override void Initialize()
        {
            base.Initialize();

            // <Woundmed>
            // this is slop

            //SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            //SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>((uid, _, args) => HandleMind(uid, args.OldBody));
            //SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);

            SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>(HandleAddition);
            SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>(HandleRemoval);
            SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
            // </Woundmed>
        }

        private void HandleMind(EntityUid newEntity, EntityUid oldEntity, BrainComponent? brain = null)
        {
            if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
                return;

            EnsureComp<MindContainerComponent>(newEntity);
            EnsureComp<MindContainerComponent>(oldEntity);

            var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
            ghostOnMove.MustBeDead = HasComp<MobStateComponent>(newEntity); // Don't ghost living players out of their bodies.

            if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
                return;

            _mindSystem.TransferTo(mindId, newEntity, mind: mind);
            if (brain != null)
                brain.Active = true;
        }

        private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
        {
            args.Cancel();
        }
    }
}
