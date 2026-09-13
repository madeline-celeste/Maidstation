using Content.Shared._Shitmed.Body.Part;
using Content.Shared._Shitmed.Medical.Surgery.Wounds;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Part;

public sealed partial class BodyPartComponent
{
    [DataField, AutoNetworkedField]
    public BodyPartSlot? ParentSlot;

    /// <summary>
    ///     [Woundmed]
    ///     What composition does this body part classify as.
    ///     TODO this should probably be a prototype
    /// </summary>
    [DataField]
    public BodyPartComposition PartComposition = BodyPartComposition.Organic;

    /// <summary>
    ///     [Woundmed]
    ///     Whether this body part is enabled or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    ///     [Woundmed]
    ///     Whether this body part can be enabled or not. Used for non-functional prosthetics.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanEnable = true;

    /// <summary>
    ///     [Woundmed]
    ///     The name of the container for this body part. Used in insertion surgeries.
    /// </summary>
    [DataField]
    public string ContainerName { get; set; } = "part_slot";

    /// <summary>
    ///     [Woundmed]
    ///     The slot for item insertion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ItemSlot ItemInsertionSlot = new();

    /// <summary>
    ///     [Woundmed]
    /// </summary>
    [DataField]
    public string SlotId = string.Empty;

    /// <summary>
    ///     [Woundmed]
    ///     Current species. Dictates things like body part sprites.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Species { get; set; } = "";

    /// <summary>
    ///     [Woundmed]
    ///     The ID of the base layer for this body part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? BaseLayerId;

    /// <summary>
    ///     [Woundmed]
    ///     On what WoundableSeverity we should re-enable the part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public WoundableSeverity EnableIntegrity = WoundableSeverity.Severe;

    /// <summary>
    ///     [Woundmed]
    ///     Whether this body part can attach children or not.
    /// </summary>
    [DataField]
    public bool CanAttachChildren = true;

    /// <summary>
    ///     [Woundmed]
    ///     When attached, the part will ensure these components on the entity, and delete them on removal.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public ComponentRegistry? OnAdd;

    /// <summary>
    ///     [Woundmed]
    ///     When removed, the part will ensure these components on the entity, and add them on removal.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public ComponentRegistry? OnRemove;

    /// <inheritdoc/>
    [DataField]
    public string ToolName { get; set; } = "A body part";

    /// <inheritdoc/>
    [DataField, AutoNetworkedField]
    public bool? Used { get; set; } = null;

    /// <inheritdoc/>
    [DataField]
    public float Speed { get; set; } = 1f;
}
