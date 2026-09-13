using Content.Goobstation.Common.Body;
using Content.Shared._Shitmed.Body.Organ;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    private void HandleRemoval(Entity<BrainComponent> brain, ref OrganRemovedFromBodyEvent args)
    {
        if (TerminatingOrDeleted(brain)
            || TerminatingOrDeleted(args.OldBody))
            return;

        // SHITCODE
        // this is used to stpo changeling mind being removed, slop
        var remEv = new BeforeBrainRemovedEvent();
        RaiseLocalEvent(args.OldBody, ref remEv);
        if (remEv.Blocked)
            return;

        brain.Comp.Active = false;
        if (!CheckOtherBrains(args.OldBody))
        {
            // Prevents revival, should kill the user within a given timespan too.
            EnsureComp<DebrainedComponent>(args.OldBody);
            HandleMind(brain, args.OldBody, brain);
        }
    }

    private void HandleAddition(Entity<BrainComponent> brain, ref OrganAddedToBodyEvent args)
    {
        if (TerminatingOrDeleted(brain)
            || TerminatingOrDeleted(args.Body))
            return;

        // SHITCODE
        // this is used to stpo another mind being added to changeling mind i think, slopppp idk
        var addEv = new BeforeBrainAddedEvent();
        RaiseLocalEvent(args.Body, ref addEv);
        if (addEv.Blocked)
            return;

        if (!CheckOtherBrains(args.Body))
        {
            RemComp<DebrainedComponent>(args.Body);
            HandleMind(args.Body, brain, brain);
        }
    }

    private bool CheckOtherBrains(EntityUid entity)
    {
        var hasOtherBrains = false;
        if (TryComp<BodyComponent>(entity, out var body))
        {
            if (TryComp<BrainComponent>(entity, out var bodyBrain))
                hasOtherBrains = true;
            else
            {
                foreach (var (organ, _) in _bodySystem.GetBodyOrgans(entity, body))
                {
                    if (TryComp<BrainComponent>(organ, out var brain) && brain.Active)
                    {
                        hasOtherBrains = true;
                        break;
                    }
                }
            }
        }

        return hasOtherBrains;
    }
}