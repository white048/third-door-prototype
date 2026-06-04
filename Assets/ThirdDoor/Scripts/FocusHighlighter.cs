using UnityEngine;

public class FocusHighlighter : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color focusEmission = Color.white;
    [SerializeField] private float focusIntensity = 0.65f;

    private MaterialPropertyBlock propertyBlock;
    private bool focused;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            foreach (Material material in targetRenderer.materials)
            {
                material.EnableKeyword("_EMISSION");
            }
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    public void SetFocused(bool value)
    {
        if (focused == value)
        {
            return;
        }

        focused = value;
        Apply();
    }

    private void Apply()
    {
        if (renderers == null)
        {
            return;
        }

        Color emission = focused ? focusEmission * focusIntensity : Color.black;
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_EmissionColor", emission);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
