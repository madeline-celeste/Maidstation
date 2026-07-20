namespace Content.Goobstation.Shared.Telescience.Teleporter.Events;

[ByRefEvent]
public record struct GetTeleportBeaconDisplayNameEvent(bool Handled = false, string DisplayName = "");
