using UnityEngine;

// Visual representation of a single adhesive dot dispensed at a glue point frame.
//
// The dot is not a fixed size: pm_robot_primitive_skills derives the dot diameter from
// the dispense time (a longer open valve dispenses more adhesive, so the dot gets bigger)
// and sends it as the 'diameter' field of the VizAdhesivePoint message. Add_Adhesive_Points
// feeds that value into SetSize when it spawns the dot, and again if the same glue point is
// dispensed a second time.
//
// All sizes here are in millimetres because that is what the ROS message carries; the scene
// itself is in metres, hence the MmToUnity conversion. The dot mesh is a unit sphere, so the
// local scale is simply the size in metres: diameter across X/Z, dot height along Y (which is
// the vertical axis of the glue point frame the dot is parented to).
public class AdhesiveDot : MonoBehaviour
{
    [Header("Size (mm)")]
    public float diameterMm = 1.0f;
    public float heightMm = 0.5f;

    [Header("Appearance")]
    // Seconds the dot takes to grow from nothing to its final size when it appears.
    // Set to 0 to make it pop up instantly.
    public float growDuration = 0.25f;

    private const float MmToUnity = 0.001f;

    // Never scale to (near) zero — a dot that small is invisible and would look like the
    // dispensing silently failed.
    private const float MinSizeMm = 0.01f;

    private Vector3 targetScale;
    private float growTimer;

    private void Awake()
    {
        // Runs during Instantiate, before the spawner gets a chance to call SetSize, so the
        // inspector defaults are applied here and immediately overridden by SetSize.
        ApplySize();
    }

    // Set the final size of the dot in millimetres and (re)start the grow animation.
    public void SetSize(float diameter, float height)
    {
        diameterMm = diameter;
        heightMm = height;
        ApplySize();
    }

    private void ApplySize()
    {
        float d = Mathf.Max(diameterMm, MinSizeMm) * MmToUnity;
        float h = Mathf.Max(heightMm, MinSizeMm) * MmToUnity;
        targetScale = new Vector3(d, h, d);

        growTimer = 0f;
        transform.localScale = growDuration > 0f ? Vector3.zero : targetScale;
    }

    // Grow the dot to its final size. Once it has arrived this is a no-op for the rest of
    // the dot's life.
    private void Update()
    {
        if (growTimer >= growDuration) return;

        growTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(growTimer / growDuration));
        transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
    }
}
