using UnityEngine;
using System.Collections;


public class TelecentricDepthOfField : MonoBehaviour
{
    public Material dofMaterial;

    [Tooltip("Abstandsbereich um die Schärfe Ebene")]
    public float maxDepthDifference = 0.02f;

    [Tooltip("Maximaler Unschärfe Radius in Pixeln")]
    public float maxBlurRadius = 2.0f;

    Camera cam;

    void OnValidate()
    {
        maxDepthDifference = Mathf.Max(0.0001f, maxDepthDifference);
        maxBlurRadius = Mathf.Clamp(maxBlurRadius, 0.0f, 10.0f);
    }

    IEnumerator Start()
    {
        // Einen Frame warten, falls ein anderes Skript die Kamera konfiguriert
        yield return null;

        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;

        // Fokus Ebene liegt im Ursprung des Kamera Raums
        dofMaterial.SetFloat("_FocusZ", 0.0f);
    }

    void Update()
    {
        if (dofMaterial == null) return;

        dofMaterial.SetFloat("_MaxDepthDiff", maxDepthDifference);
        dofMaterial.SetFloat("_MaxBlurRadius", maxBlurRadius);
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
}