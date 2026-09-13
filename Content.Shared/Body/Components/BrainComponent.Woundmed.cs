namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent : Component
{
    /// <summary>
    ///     [Shitmed]
    ///     Is this brain currently controlling the entity?
    /// </summary>
    [DataField]
    public bool Active = true;
}