# TelecentricDepthOfField Docs

This component applies a post-process blur and drives blur intensity from camera `world Y` distance.

## Distance To Blur Formula

1. `distanceY = abs(cameraY - fullSharpnessY)`
2. No-blur dead zone:
   - if `distanceY <= maxDepthDifference`, then `targetBlurRadius = 0`
3. Outside dead zone (linear, uncapped):
   - `effectiveDistance = distanceY - maxDepthDifference`
   - `targetBlurRadius = effectiveDistance * blurIncreasePerY`
4. `currentBlurRadius` is smoothed (if `blurSmoothSpeed > 0`)
5. Shader input:
   - `_MaxBlurRadius = currentBlurRadius * blurStrengthMultiplier`
   - `_BlurAmount = 1` when blur is active, else `0`

## Inspector Parameters

- `dofMaterial`
  Material using `Custom/TelecentricDepthOfField`.

- `maxDepthDifference`
  Half-width of the no-blur zone around `fullSharpnessY`.
  Inside `fullSharpnessY ± maxDepthDifference`, blur is zero.

- `blurIncreasePerY`
  Linear slope of blur radius per 1.0 Y distance outside the dead zone.
  Bigger value means blur grows faster when distance increases.

- `fullSharpnessY`
  Camera world Y that gives full sharpness (minimum blur).

- `blurSmoothSpeed`
  Temporal smoothing speed.
  `0` = immediate response, larger = smoother but slower.

- `blurStrengthMultiplier`
  Additional global multiplier.
  `1` normal, `4-8` very strong.

- `showDebugInfo`
  Shows on-screen debug panel during Play mode.

- `debugInfoPosition`
  Top-left pixel position of debug panel.
