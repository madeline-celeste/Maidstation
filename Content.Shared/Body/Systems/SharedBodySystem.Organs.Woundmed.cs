using Content.Shared._Shitmed.Body.Organ;
using Content.Shared._Shitmed.BodyEffects;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeOrgans()
    {
        SubscribeLocalEvent<OrganComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OrganComponent, OrganEnableChangedEvent>(OnOrganEnableChanged);
    }

    private void OnMapInit(Entity<OrganComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.OnAdd is not null || ent.Comp.OnRemove is not null)
            EnsureComp<OrganEffectComponent>(ent);
    }

    private void OnOrganEnableChanged(Entity<OrganComponent> organEnt, ref OrganEnableChangedEvent args)
    {
        if (!organEnt.Comp.CanEnable && args.Enabled)
            return;

        organEnt.Comp.Enabled = args.Enabled;

        if (args.Enabled)
            EnableOrgan(organEnt);
        else
            DisableOrgan(organEnt);

        if (organEnt.Comp.Body is { Valid: true } bodyEnt)
            RaiseLocalEvent(organEnt, new OrganComponentsModifyEvent(bodyEnt, args.Enabled));

        Dirty(organEnt, organEnt.Comp);
    }

    private void EnableOrgan(Entity<OrganComponent> organEnt)
    {
        if (!TryComp(organEnt.Comp.Body, out BodyComponent? body))
            return;

        // I hate having to hardcode these checks so much.
        if (HasComp<EyesComponent>(organEnt))
        {
            var ev = new OrganEnabledEvent(organEnt);
            RaiseLocalEvent(organEnt, ref ev);
        }
    }

    private void DisableOrgan(Entity<OrganComponent> organEnt)
    {
        if (!TryComp(organEnt.Comp.Body, out BodyComponent? body))
            return;

        // I hate having to hardcode these checks so much.
        if (HasComp<EyesComponent>(organEnt))
        {
            var ev = new OrganDisabledEvent(organEnt);
            RaiseLocalEvent(organEnt, ref ev);
        }
    }

    /// <summary>
    /// Tries to remove the organ if it is inside of a body part.
    /// </summary>
    public bool TryRemoveOrgan(EntityUid organId, OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ))
            return false;

        var ev = new TryRemoveOrganEvent(organId, organ);
        RaiseLocalEvent(organId, ref ev);

        if (ev.Cancelled)
            return false;

        return RemoveOrgan(organId, organ);
    }
}