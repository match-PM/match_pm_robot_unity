using UnityEngine;
using System.Collections.Generic;

public class ShowArticulationNames : MonoBehaviour
{
    private ArticulationBody[] articulationBodies;
    private GUIStyle labelStyle;
    private Texture2D backgroundTex;
    private Texture2D lineTex;

    void Start()
    {
        articulationBodies = FindObjectsOfType<ArticulationBody>();
    }

    void OnGUI()
    {
        // Setup label style and background
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 14;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;

            // Semi-transparent black
            backgroundTex = new Texture2D(1, 1);
            backgroundTex.SetPixel(0, 0, new Color(0, 0, 0, 0.85f));
            backgroundTex.Apply();
            labelStyle.normal.background = backgroundTex;

            // Line texture (solid white)
            lineTex = new Texture2D(1, 1);
            lineTex.SetPixel(0, 0, Color.cyan); // cyan for visibility
            lineTex.Apply();
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        List<Rect> usedRects = new List<Rect>();
        float offsetDistance = 40f;

        foreach (var ab in articulationBodies)
        {
            // Check if the ArticulationBody is active and has a valid transform
            if (ab == null || !ab.gameObject.activeInHierarchy || ab.transform == null)
                continue;
            Vector3 worldPos = ab.transform.position;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0)
            {
                string label = ab.gameObject.name;

                // Dynamically calculate label size
                Vector2 labelSize = labelStyle.CalcSize(new GUIContent(label));
                labelSize.x += 16; // margin
                labelSize.y += 8;

                float x = screenPos.x - labelSize.x / 2;
                float y = Screen.height - screenPos.y - labelSize.y / 2;
                Vector2 labelPos = new Vector2(x, y);

                // Fan-out logic to avoid overlapping
                int attempt = 0;
                while (attempt < 8)
                {
                    bool overlap = false;
                    foreach (var rect in usedRects)
                    {
                        Rect labelRectTest = new Rect(labelPos, labelSize);
                        if (labelRectTest.Overlaps(rect))
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (!overlap) break;
                    float angle = (Mathf.PI * 2f / 8f) * attempt;
                    labelPos.x = screenPos.x + Mathf.Cos(angle) * offsetDistance - labelSize.x / 2;
                    labelPos.y = (Screen.height - screenPos.y) + Mathf.Sin(angle) * offsetDistance - labelSize.y / 2;
                    attempt++;
                }

                Rect labelRect = new Rect(labelPos, labelSize);
                usedRects.Add(labelRect);

                // Draw connecting line (before label so it doesn't overlap text)
                DrawLine(new Vector2(screenPos.x, Screen.height - screenPos.y),
                         new Vector2(labelPos.x + labelSize.x / 2, labelPos.y + labelSize.y / 2),
                         lineTex, 3f);

                // Shadow
                var shadowStyle = new GUIStyle(labelStyle);
                shadowStyle.normal.textColor = Color.black;
                Rect shadowRect = new Rect(labelRect.x + 2, labelRect.y + 2, labelRect.width, labelRect.height);
                GUI.Label(shadowRect, label, shadowStyle);

                // Label
                GUI.Label(labelRect, label, labelStyle);
            }
        }
    }

    // Draw a line in OnGUI with thickness and color
    void DrawLine(Vector2 pointA, Vector2 pointB, Texture2D tex, float width)
    {
        Matrix4x4 savedMatrix = GUI.matrix;
        Color savedColor = GUI.color;
        Vector2 d = pointB - pointA;
        float angle = Mathf.Rad2Deg * Mathf.Atan2(d.y, d.x);
        float length = d.magnitude;

        GUI.color = Color.cyan; // Or Color.yellow, etc.
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y - width / 2, length, width), tex);
        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }
}
