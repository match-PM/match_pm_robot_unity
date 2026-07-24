using UnityEngine;

// Keeps an adhesive bead locked onto a target object even across scene rebuilds.
//
// UnityComponentSpawner destroys and recreates every "spawned" object whenever the
// assembly scene changes (e.g. when a component is gripped and re-parented). A bead
// parented into that hierarchy would be destroyed together with its component. To
// survive, the bead lives at the scene root (never tagged "spawned") and re-locates
// its target by name each frame, reapplying the local offset captured at attach time.
// When the target is destroyed and rebuilt under the same name, the follower simply
// re-acquires it, so the bead appears to stay glued to the moving component.
public class BeadFollower : MonoBehaviour
{
    // Name of the target GameObject to follow (the component that owns the dispense
    // frame). GameObject.Find resolves this regardless of where the component is
    // re-parented, so it keeps working after a grip moves the component under the gripper.
    public string targetName;

    // Bead pose expressed in the target's local space, captured when the bead was
    // attached. Applied every frame to keep the bead rigidly fixed to the target.
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;

    private Transform target;

    // Re-acquire the target if we don't currently have a live one. Unity's overloaded
    // equality reports a destroyed object as == null, so a scene rebuild automatically
    // forces a fresh lookup here on the next frame.
    private void AcquireTarget()
    {
        if (string.IsNullOrEmpty(targetName)) return;
        GameObject go = GameObject.Find(targetName);
        target = go != null ? go.transform : null;
    }

    // LateUpdate so we follow after the target has been moved for this frame.
    private void LateUpdate()
    {
        if (target == null)
            AcquireTarget();

        // Target not in the scene right now (e.g. mid-rebuild) — leave the bead where
        // it is and try again next frame rather than snapping it to the origin.
        if (target == null)
            return;

        transform.SetPositionAndRotation(
            target.TransformPoint(localPosition),
            target.rotation * localRotation);
    }
}
