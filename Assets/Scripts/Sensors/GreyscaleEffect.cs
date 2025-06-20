using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class GreyscaleEffect : MonoBehaviour
{
    private Material effectMaterial;

    // Reference to the shader
    private static readonly string shaderName = "Custom/Greyscale";

    void Start()
    {
        Shader shader = Shader.Find(shaderName);
        if (shader != null)
        {
            effectMaterial = new Material(shader);
        }
        else
        {
            Debug.LogError("Shader not found: " + shaderName);
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (effectMaterial != null)
            Graphics.Blit(src, dest, effectMaterial);
        else
            Graphics.Blit(src, dest);
    }
}
