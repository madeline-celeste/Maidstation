using Content.Goobstation.Shared.Telescience.Teleporter.Components;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Telescience.Teleporter;

[Serializable, NetSerializable]
public enum TeleporterConsoleUiKey : byte
{
    Key
}

public sealed partial class SharedTeleporterControllerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterControllerComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<TeleporterControllerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleporterControllerComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    // TODO : automation ports(?)
    // Ensuring that only Teleporter Station can connect to that port, only one.
    private void OnLinkAttempt(Entity<TeleporterControllerComponent> ent, ref LinkAttemptEvent args)
    {
        if (args.SourcePort != TeleporterControllerComponent.SourcePortStation)
            return;
        if (HasComp<TeleporterStationComponent>(args.Sink) && ent.Comp.LinkedStation is null)
            return;
        args.Cancel();
    }

    private void OnNewLink(Entity<TeleporterControllerComponent> ent, ref NewLinkEvent args)
    {
        if (args.SourcePort != TeleporterControllerComponent.SourcePortStation || !HasComp<TeleporterHubComponent>(args.Sink))
            return;
        ent.Comp.LinkedStation = args.Sink;
        Dirty(ent);
    }

    private void OnPortDisconnected(Entity<TeleporterControllerComponent> ent, ref PortDisconnectedEvent args)
    {
        ent.Comp.LinkedStation = null;
        Dirty(ent);
    }
}