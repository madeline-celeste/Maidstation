using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Shared.Telescience.Teleporter.Components;
using Content.Goobstation.Shared.Telescience.Teleporter.Events;

namespace Content.Goobstation.Shared.Telescience.Teleporter;

public sealed partial class SharedTeleporterSystem : EntitySystem
{
    public bool TryGetStation(Entity<TeleporterHubComponent> hub, [NotNullWhen(true)] out Entity<TeleporterStationComponent>? station)
    {
        station = null;
        if (hub.Comp.LinkedStation is not { } stationUid
            || !TryComp(stationUid, out TeleporterStationComponent? stationComp))
            return false;

        station = (stationUid, stationComp);
        return true;
    }

    public bool TryGetHub(Entity<TeleporterStationComponent> station, [NotNullWhen(true)] out Entity<TeleporterHubComponent>? hub)
    {
        hub = null;
        if (station.Comp.LinkedHub is not { } hubUid
            || !TryComp(hubUid, out TeleporterHubComponent? hubComp))
            return false;

        hub = (hubUid, hubComp);
        return true;
    }

    public List<EntityUid> GetTeleporterBeacons()
    {
        List<EntityUid> beacons = new();
        var query = EntityQueryEnumerator<TeleporterBeaconComponent>();
        while (query.MoveNext(out EntityUid uid, out TeleporterBeaconComponent? comp))
            beacons.Add(uid);

        // let other systems say they got some beacons based on their own logic, eg. GPS
        DiscoverTeleportBeaconsEvent ev = new DiscoverTeleportBeaconsEvent(beacons);
        RaiseLocalEvent(ref ev);

        return ev.Entities;
    }

    /// <summary>
    /// Returns the name that should be used to display this beacon after raising <see cref="GetTeleportBeaconDisplayNameEvent"/>.
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public string GetBeaconName(EntityUid uid)
    {
        GetTeleportBeaconDisplayNameEvent ev = new GetTeleportBeaconDisplayNameEvent();
        RaiseLocalEvent(ref ev);

        if (ev.Handled)
            return ev.DisplayName;
        else
            return Loc.GetString("telescience-teleporter-target-name-generic", ("name", Name(uid)));
    }

    /// <summary>
    /// Returns whether the provided TeleporterStation is calibrated a linked TeleporterHub's target.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="station"></param>
    /// <returns></returns>
    public bool IsCalibrated(Entity<TeleporterStationComponent> station)
        => station.Comp.CalibratedTarget is not null
        && TryComp(station, out TeleporterHubComponent? hubComp) && hubComp.CurrentTarget == station.Comp.CalibratedTarget;

    /// <summary>
    /// Returns the estimated success rate for the provided TeleporterHub and TeleportationStation.
    /// </summary>
    /// <param name="hub"></param>
    /// <param name="station"></param>
    /// <returns></returns>
    public float GetSuccessRate(Entity<TeleporterHubComponent> hub, Entity<TeleporterStationComponent>? station)
        => (station is not null && IsCalibrated(station.Value))
            ? hub.Comp.CalibratedSuccessRate : hub.Comp.UncalibratedSuccessRate;
}