using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Shared.Telescience.Teleporter.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;

namespace Content.Goobstation.Shared.Telescience.Teleporter;



public sealed partial class SharedTeleporterStationSystem : EntitySystem
{
    [Dependency] private readonly SharedTeleporterSystem _teleporter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterStationComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TeleporterStationComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<TeleporterStationComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleporterStationComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    private void OnExamined(Entity<TeleporterStationComponent> station, ref ExaminedEvent args)
    {
        if (!_teleporter.TryGetHub(station, out Entity<TeleporterHubComponent>? hub))
            args.PushMarkup(Loc.GetString("telescience-teleporter-examine-no-hub-linked"));
    }

    // Ensuring that only Teleporter Hub can connect to that port, only one.
    private void OnLinkAttempt(Entity<TeleporterStationComponent> ent, ref LinkAttemptEvent args)
    {
        if (args.SourcePort != TeleporterStationComponent.SourcePortHub)
            return;
        if (HasComp<TeleporterHubComponent>(args.Sink) && ent.Comp.LinkedHub is null)//todo check other
            return;
        args.Cancel();
    }

    private void OnNewLink(Entity<TeleporterStationComponent> ent, ref NewLinkEvent args)
    {
        if (args.SourcePort != TeleporterStationComponent.SourcePortHub)
            return;
        if (TryComp(args.Sink, out TeleporterHubComponent? hubComp))
        {
            ent.Comp.LinkedHub = args.Sink;
            Dirty(ent);
            hubComp.LinkedStation = args.Source;
            Dirty(args.Sink, hubComp);
        }
    }

    private void OnPortDisconnected(Entity<TeleporterStationComponent> ent, ref PortDisconnectedEvent args)
    {
        if (TryComp(ent.Comp.LinkedHub, out TeleporterHubComponent? hubComp))
        {
            hubComp.LinkedStation = null;
            Dirty(ent.Comp.LinkedHub.Value, hubComp);
        }
        ent.Comp.LinkedHub = null;
        Dirty(ent);
    }
}