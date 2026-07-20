namespace Content.Goobstation.Shared.Telescience.Teleporter.Events;

[ByRefEvent]
public record struct DiscoverTeleportBeaconsEvent
{
    public List<EntityUid> Entities;

    public DiscoverTeleportBeaconsEvent(List<EntityUid>? startingList) { Entities = startingList ?? new(); }
}
