using UnityEngine;
using System.Collections;

/// <summary>
/// Telecentric post-process blur driven by camera world Y distance.
///
/// Mapping:
/// 1) distanceY = abs(cameraY - fullSharpnessY)
/// 2) no-blur zone: distanceY <= maxDepthDifference => blur radius = 0
/// 3) outside zone: radius grows linearly without max distance cap
/// 4) targetRadius = max(0, distanceY - maxDepthDifference) * blurIncreasePerY
/// 5) optional temporal smoothing via blurSmoothSpeed
/// 6) shader receives boosted radius
/// </summary>
public class TelecentricDepthOfField : MonoBehaviour
{
    [Header("Material")]
    public Material dofMaterial;

    [Header("Base Blur Settings")]
    [Tooltip("Abstandsbereich um die Schärfe Ebene")]
    public float maxDepthDifference = 0.01f;

    [Tooltip("Linear blur slope: blur radius increase per 1.0 Y distance")]
    public float blurIncreasePerY = 1500.0f;

    [Header("Distance To Blur Mapping")]
    [Tooltip("World Y at full sharpness (distance 0 => blur 0)")]
    public float fullSharpnessY = 1.31797400f;

    [Tooltip("Temporal smoothing speed for blur (0 = no smoothing)")]
    public float blurSmoothSpeed = 5.0f;

    [Tooltip("Global blur strength boost (1 = normal, >1 = stronger)")]
    public float blurStrengthMultiplier = 8.0f;

    [Header("Debug Overlay")]
    [Tooltip("Show debug values on the Game screen")]
    public bool showDebugInfo = false;

    [Tooltip("Top-left position of debug text")]
    public Vector2 debugInfoPosition = new Vector2(12f, 12f);

    float currentBlurRadius;
    float debugCurrentY;
    float debugParentY;
    float debugDistanceToSharpY;
    float debugBlurVisual01;
    float debugTargetBlurRadius;

    void OnValidate()
    {
        maxDepthDifference = Mathf.Max(0.0001f, maxDepthDifference);
        blurIncreasePerY = Mathf.Clamp(blurIncreasePerY, 0.0f, 50000.0f);
        blurSmoothSpeed = Mathf.Max(0.0f, blurSmoothSpeed);
        blurStrengthMultiplier = Mathf.Clamp(blurStrengthMultiplier, 0.1f, 20.0f);
        debugInfoPosition.x = Mathf.Max(0.0f, debugInfoPosition.x);
        debugInfoPosition.y = Mathf.Max(0.0f, debugInfoPosition.y);
    }

    IEnumerator Start()
    {
        yield return null;
        currentBlurRadius = GetTargetBlurRadius();
    }

    void Update()
    {
        if (dofMaterial == null)
        {
            return;
        }

        debugCurrentY = transform.position.y;
        debugParentY = transform.parent != null ? transform.parent.position.y : float.NaN;
        debugDistanceToSharpY = Mathf.Abs(debugCurrentY - fullSharpnessY);
        debugTargetBlurRadius = CalculateTargetBlurRadiusFromDistance(debugDistanceToSharpY);
        debugBlurVisual01 = debugTargetBlurRadius > 0.0f
            ? debugTargetBlurRadius / (debugTargetBlurRadius + 1.0f)
            : 0.0f;

        if (blurSmoothSpeed <= 0.0f)
        {
            currentBlurRadius = debugTargetBlurRadius;
        }
        else
        {
            float t = 1.0f - Mathf.Exp(-blurSmoothSpeed * Time.deltaTime);
            currentBlurRadius = Mathf.Lerp(currentBlurRadius, debugTargetBlurRadius, t);
        }

        float boostedRadius = currentBlurRadius * blurStrengthMultiplier;
        dofMaterial.SetFloat("_MaxDepthDiff", maxDepthDifference);
        dofMaterial.SetFloat("_MaxBlurRadius", boostedRadius);
        dofMaterial.SetFloat("_BlurAmount", boostedRadius > 0.0001f ? 1.0f : 0.0f);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (dofMaterial == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        Graphics.Blit(src, dest, dofMaterial);
    }

    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying)
        {
            return;
        }

        Rect boxRect = new Rect(debugInfoPosition.x, debugInfoPosition.y, 640f, 140f);
        GUI.Box(boxRect, "Telecentric DOF Debug");

        string parentYText = float.IsNaN(debugParentY) ? "none" : debugParentY.ToString("F6");
        string debugText =
            $"cameraY: {debugCurrentY:F8}\n" +
            $"parentY: {parentYText}\n" +
            $"fullSharpnessY: {fullSharpnessY:F8}\n" +
            $"distanceToSharpY: {debugDistanceToSharpY:F8}\n" +
            $"blurVisual01: {debugBlurVisual01:F3}\n" +
            $"targetBlurRadius: {debugTargetBlurRadius:F3}, currentBlurRadius: {currentBlurRadius:F3}\n" +
            $"blurStrengthMultiplier: {blurStrengthMultiplier:F2}";

        GUI.Label(
            new Rect(boxRect.x + 8f, boxRect.y + 22f, boxRect.width - 16f, boxRect.height - 30f),
            debugText
        );
    }

    float GetTargetBlurRadius()
    {
        float distanceToSharpY = Mathf.Abs(transform.position.y - fullSharpnessY);
        return CalculateTargetBlurRadiusFromDistance(distanceToSharpY);
    }

    float CalculateTargetBlurRadiusFromDistance(float distanceToSharpY)
    {
        float effectiveDistance = Mathf.Max(0.0f, distanceToSharpY - maxDepthDifference);
        return effectiveDistance * blurIncreasePerY;
    }
}
