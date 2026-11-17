using UnityEngine;

/// <summary>
/// Optional override for the segmentation of the RealSense simulation.
///
/// Without this component every renderer is labeled automatically: by the nearest
/// spawned assembly component, adhesive dot or robot link (ArticulationBody) above it.
/// Put this component on a GameObject to make it (and every renderer below it that has
/// no closer owner) its own segment with a custom name, or to hide it from the
/// segmentation and depth images entirely (e.g. helper gizmos).
/// </summary>
[DisallowMultipleComponent]
public class SegmentationLabel : MonoBehaviour
{
    [Tooltip("Label name published in the label table. Empty = GameObject name.")]
    public string labelName = "";

    [Tooltip("Free-form kind/category published in the label table, e.g. for grouping later.")]
    public string kind = "custom";

    [Tooltip("Exclude the renderers below this object from segmentation and depth.")]
    public bool ignore = false;
}
