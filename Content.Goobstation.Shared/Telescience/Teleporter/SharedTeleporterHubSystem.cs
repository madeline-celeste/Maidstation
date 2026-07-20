using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Shared.Telescience.Teleporter.Components;
using Content.Shared.Examine;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Content.Goobstation.Shared.Telescience.Teleporter;

public sealed partial class SharedTeleporterHubSystem : EntitySystem
{
    [Dependency] private readonly SharedTeleporterSystem _teleporter = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const string PortalFixture = "portalFixture";
    private const string ProjectileFixture = "projectile";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleporterHubComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TeleporterHubComponent, StartCollideEvent>(OnStartCollide);
    }

    // TODO: should be like a completely random location on a real map
    // for now you go to the void ig lmao
    private MapCoordinates GetBadDestination()
        => MapCoordinates.Nullspace;

    private MapCoordinates GetCurrentDestination(Entity<TeleporterHubComponent> ent)
        => ent.Comp.CurrentTarget is not null ? _xform.GetMapCoordinates(ent.Comp.CurrentTarget.Value) : GetBadDestination();

    // some of this is duplicated from SharedPortalSystem
    private void OnStartCollide(Entity<TeleporterHubComponent> ent, ref StartCollideEvent args)
    {
        EntityUid subject = args.OtherEntity;

        if (!ShouldCollide(args.OurFixtureId, args.OtherFixtureId, args.OurFixture, args.OtherFixture)
            || HasComp<PortalTimeoutComponent>(subject)
            || !ent.Comp.IsPortalActive)
            return;

        // break pulls before portal enter so we don't break shit
        if (TryComp<PullableComponent>(subject, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(subject, pullable, ignoreGrab: true);

        if (TryComp<PullerComponent>(subject, out var pullerComp)
            && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
            _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling, ignoreGrab: true);

        // TODO: check fail / success rate here
        TeleportEntity(ent, subject, ent.Comp.CurrentTarget);
    }

    private void TeleportEntity(Entity<TeleporterHubComponent> ent, EntityUid subject, EntityUid? target)
    {
        TeleportAttemptEvent ev = new TeleportAttemptEvent(false);
        RaiseLocalEvent(subject, ref ev);
        if (ev.Cancelled)
            return;

        MapCoordinates destinationCoords = GetCurrentDestination(ent);

        var arrivalSound = CompOrNull<PortalComponent>(target)?.ArrivalSound ?? ent.Comp.ArrivalSound;
        var departureSound = ent.Comp.DepartureSound;

        if (TryComp<ProjectileComponent>(subject, out var projectile))
            projectile.IgnoreShooter = false;

        _xform.SetCoordinates(subject, _xform.ToCoordinates(subject, destinationCoords));

        _audio.PlayPredicted(departureSound, ent, subject);
        _audio.PlayPredicted(arrivalSound, subject, subject);
    }

    /// <summary>
    /// Checks if the colliding fixtures are the ones we want.
    /// </summary>
    /// <returns>
    /// False if our fixture is not a portal fixture.
    /// False if other fixture is not hard, but makes an exception for projectiles.
    /// </returns>
    private bool ShouldCollide(string ourId, string otherId, Fixture our, Fixture other)
    {
        return ourId == PortalFixture && (other.Hard || otherId == ProjectileFixture);
    }
    private void OnExamined(Entity<TeleporterHubComponent> hub, ref ExaminedEvent args)
    {
        _teleporter.TryGetStation(hub, out Entity<TeleporterStationComponent>? station);

        args.PushMarkup(Loc.GetString("telescience-teleporter-examine-success-rate",
            ("rate", $"{_teleporter.GetSuccessRate(hub, station) * 100:F1}")));

        bool isCalibrated = station is not null && _teleporter.IsCalibrated(station.Value);
        args.PushMarkup(Loc.GetString($"telescience-teleporter-examine-{(isCalibrated ? "calibrated" : "not-calibrated")}"));

        if (station is null)
            args.PushMarkup(Loc.GetString("telescience-teleporter-examine-no-station-linked"));
    }
}