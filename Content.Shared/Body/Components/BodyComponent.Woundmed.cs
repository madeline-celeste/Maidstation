using Content.Shared._Shitmed.Body;

namespace Content.Shared.Body.Components;

public sealed partial class BodyComponent : Component
{
    /// <summary>
    ///     [Woundmed]
    ///     Fuck borgs.
    /// </summary>
    [DataField]
    public BodyType BodyType = BodyType.Complex;

    /// <summary>
    ///     [Shitmed]
    ///     When should wounds on this be healed.
    /// </summary>
    [ViewVariables, AutoNetworkedField, Access(Other = AccessPermissions.ReadWrite)]
    public TimeSpan HealAt;
}