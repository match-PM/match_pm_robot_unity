# TelecentricDepthOfField Docs

This component applies a post-process blur and drives blur intensity from camera `world Y` distance.

## Distance To Blur Formula

1. `distanceY = abs(cameraY - fullSharpnessY)`
<<<<<<< .merge_file_YwVEc8
2. No-blur dead zone:
   - if `distanceY <= maxDepthDifference`, then `targetBlurRadius = 0`
3. Outside dead zone (linear, uncapped):
   - `effectiveDistance = distanceY - maxDepthDifference`
   - `targetBlurRadius = effectiveDistance * blurIncreasePerY`
4. `currentBlurRadius` is smoothed (if `blurSmoothSpeed > 0`)
5. Shader input:
=======
2. Optional quantization:
   `distanceY = round(distanceY / yPerInputUnit) * yPerInputUnit`
3. No-blur dead zone:
   - if `distanceY <= maxDepthDifference`, then `targetBlurRadius = 0`
4. Outside dead zone (linear, uncapped):
   - `effectiveDistance = distanceY - maxDepthDifference`
   - `targetBlurRadius = effectiveDistance * blurIncreasePerY`
5. `currentBlurRadius` is smoothed (if `blurSmoothSpeed > 0`)
6. Shader input:
>>>>>>> .merge_file_IOTVuy
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

<<<<<<< .merge_file_YwVEc8
=======
- `yPerInputUnit`
  Mapping between your controller input step and Y movement.
  Example: if `10 -> 0.01`, then `1 -> 0.001` (default).

- `movementDetectThresholdY`
  Minimum per-frame Y delta considered motion in debug overlay.

- `quantizeDistanceToInputStep`
  If enabled, blur distance snaps to input steps for stable precision.

>>>>>>> .merge_file_IOTVuy
- `showDebugInfo`
  Shows on-screen debug panel during Play mode.

- `debugInfoPosition`
  Top-left pixel position of debug panel.
<<<<<<< .merge_file_YwVEc8
=======

## Strong Blur Preset

Use this when blur is hard to see (especially at high image resolution):

- `blurIncreasePerY = 1200`
- `blurStrengthMultiplier = 6`
- `maxDepthDifference = 0.002`
- `blurSmoothSpeed = 0`

## Notes For ROS / rqt

- Verify you are viewing the camera topic that this Unity camera publishes.
- If debug panel shows `targetBlurRadius/currentBlurRadius` changing but image looks unchanged, increase
  `blurIncreasePerY` and `blurStrengthMultiplier`.
>>>>>>> .merge_file_IOTVuy
