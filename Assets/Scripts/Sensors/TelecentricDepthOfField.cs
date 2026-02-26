using UnityEngine;
using System.Collections;


/// <summary>
/// Telecentric post-process blur driven by camera world Y distance.
///
/// Mapping:
/// 1) distanceY = abs(cameraY - fullSharpnessY)
/// 2) optional quantization to input step size
/// 3) no-blur zone: distanceY <= maxDepthDifference => blur radius = 0
/// 4) outside zone: radius grows linearly without max distance cap
/// 5) targetRadius = max(0, distanceY - maxDepthDifference) * blurIncreasePerY
/// 6) optional temporal smoothing via blurSmoothSpeed
/// 7) shader receives boosted radius
/// </summary>
public class TelecentricDepthOfField : MonoBehaviour
{
    [Header("Material")]
    public Material dofMaterial;

    [Header("Base Blur Settings")]
    [Tooltip("Abstandsbereich um die Schärfe Ebene")]
    public float maxDepthDifference = 0.01f;

    [Tooltip("Linear blur slope: blur radius increase per 1.0 Y distance")]
    public float blurIncreasePerY = 100.0f;

    [Header("Distance To Blur Mapping")]
    [Tooltip("World Y at full sharpness (distance 0 => blur 0)")]
    public float fullSharpnessY = 1.31797400f;

    [Tooltip("Temporal smoothing speed for blur (0 = no smoothing)")]
    public float blurSmoothSpeed = 100.0f;

    [Tooltip("Global blur strength boost (1 = normal, >1 = stronger)")]
    public float blurStrengthMultiplier = 10.0f;
    
    public bool quantizeDistanceToInputStep = false;

    [Header("Debug Overlay")]
    [Tooltip("Show debug values on the Game screen")]
    public bool showDebugInfo = false;

    [Tooltip("Top-left position of debug text")]
    public Vector2 debugInfoPosition = new Vector2(12f, 12f);

    Camera cam;

    // Input step size in world Y units, used for optional quantization to match controller precision.
    float yPerInputUnit = 0.001f;
    float movementDetectThresholdY = 0.00001f;

    float currentBlurRadius;
    float debugCurrentY;
    float debugParentY;
    float debugSourceY;
    float debugMovementDeltaY;
    bool debugMovementDetected;
    float debugRawDistanceToSharpY;
    float debugDistanceToSharpY;
    float debugDistanceInInputUnits;
    float debugBlurVisual01;
    float debugTargetBlurRadius;
    float lastSourceY;
    bool hasLastSourceY;

    void OnValidate()
    {
        maxDepthDifference = Mathf.Max(0.0001f, maxDepthDifference);
        blurIncreasePerY = Mathf.Clamp(blurIncreasePerY, 0.0f, 50000.0f);
        blurSmoothSpeed = Mathf.Max(0.0f, blurSmoothSpeed);
        blurStrengthMultiplier = Mathf.Clamp(blurStrengthMultiplier, 0.1f, 10.0f);
        yPerInputUnit = Mathf.Max(0.0000001f, yPerInputUnit);
        movementDetectThresholdY = Mathf.Max(0.0f, movementDetectThresholdY);
        debugInfoPosition.x = Mathf.Max(0.0f, debugInfoPosition.x);
        debugInfoPosition.y = Mathf.Max(0.0f, debugInfoPosition.y);
    }

    IEnumerator Start()
    {
        // Einen Frame warten, falls ein anderes Skript die Kamera konfiguriert
        yield return null;

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.depthTextureMode |= DepthTextureMode.Depth;
        }

        currentBlurRadius = GetTargetBlurRadius();
        lastSourceY = transform.position.y;
        hasLastSourceY = true;
    }

    void Update()
    {
        if (dofMaterial == null) return;

        debugCurrentY = transform.position.y;
        debugParentY = transform.parent != null ? transform.parent.position.y : float.NaN;
        debugSourceY = debugCurrentY;

        if (!hasLastSourceY)
        {
            lastSourceY = debugSourceY;
            hasLastSourceY = true;
        }

        debugMovementDeltaY = Mathf.Abs(debugSourceY - lastSourceY);
        debugMovementDetected = debugMovementDeltaY >= movementDetectThresholdY;
        lastSourceY = debugSourceY;

        debugRawDistanceToSharpY = Mathf.Abs(debugSourceY - fullSharpnessY);
        debugDistanceToSharpY = GetDistanceUsedForBlur(debugRawDistanceToSharpY);
        debugDistanceInInputUnits = debugRawDistanceToSharpY / yPerInputUnit;
        debugTargetBlurRadius = CalculateTargetBlurRadiusFromDistance(debugDistanceToSharpY);
        debugBlurVisual01 = debugTargetBlurRadius > 0.0f
            ? debugTargetBlurRadius / (debugTargetBlurRadius + 1.0f)
            : 0.0f;

        float targetBlurRadius = debugTargetBlurRadius;
        if (blurSmoothSpeed <= 0.0f)
        {
            currentBlurRadius = targetBlurRadius;
        }
        else
        {
            float t = 1.0f - Mathf.Exp(-blurSmoothSpeed * Time.deltaTime);
            currentBlurRadius = Mathf.Lerp(currentBlurRadius, targetBlurRadius, t);
        }

        float boostedRadius = currentBlurRadius * blurStrengthMultiplier;
        dofMaterial.SetFloat("_MaxDepthDiff", maxDepthDifference);
        dofMaterial.SetFloat("_MaxBlurRadius", boostedRadius);
        float blurAmount = boostedRadius > 0.0001f ? 1.0f : 0.0f;
        dofMaterial.SetFloat("_BlurAmount", blurAmount);
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

        Rect boxRect = new Rect(debugInfoPosition.x, debugInfoPosition.y, 690f, 184f);
        GUI.Box(boxRect, "Telecentric DOF Debug");

        string parentYText = float.IsNaN(debugParentY) ? "none" : debugParentY.ToString("F6");
        float yStep1 = yPerInputUnit;
        float yStep01 = yPerInputUnit * 0.1f;
        float yStep001 = yPerInputUnit * 0.01f;
        string debugText =
            $"cameraY: {debugCurrentY:F8}\n" +
            $"parentY: {parentYText}\n" +
            $"sourceY: {debugSourceY:F8}\n" +
            $"fullSharpnessY: {fullSharpnessY:F8}\n" +
            $"deltaY(frame): {debugMovementDeltaY:F8}, detected: {debugMovementDetected}\n" +
            $"distanceToSharpY(raw): {debugRawDistanceToSharpY:F8}\n" +
            $"distanceToSharpY(used): {debugDistanceToSharpY:F8}\n" +
            $"distanceInInputUnits: {debugDistanceInInputUnits:F4}\n" +
            $"step scale: 1 -> {yStep1:F8} Y, 0.1 -> {yStep01:F8} Y, 0.01 -> {yStep001:F8} Y\n" +
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
        // Linear, uncapped distance mapping from Y difference to blur radius.
        float distanceToSharpY = Mathf.Abs(transform.position.y - fullSharpnessY);
        distanceToSharpY = GetDistanceUsedForBlur(distanceToSharpY);
        return CalculateTargetBlurRadiusFromDistance(distanceToSharpY);
    }

    float GetDistanceUsedForBlur(float rawDistanceToSharpY)
    {
        if (!quantizeDistanceToInputStep)
        {
            return rawDistanceToSharpY;
        }

        // Snap distance to discrete input steps to match controller precision.
        float stepCount = Mathf.Round(rawDistanceToSharpY / yPerInputUnit);
        return stepCount * yPerInputUnit;
    }

    float CalculateTargetBlurRadiusFromDistance(float distanceToSharpY)
    {
        // Full sharpness dead zone around focus line: |distance| <= maxDepthDifference.
        float effectiveDistance = Mathf.Max(0.0f, distanceToSharpY - maxDepthDifference);
        return effectiveDistance * blurIncreasePerY;
    }
}
