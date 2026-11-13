
using UnityEngine;

public class FragmentGlow : MonoBehaviour
{
    [Header("Artefact Fragments")]
    public Renderer[] fragmentRenderers;

    [Header("Glow Settings")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(0.1f, 0.35f, 0.8f, 1f);

    [Range(0f, 3f)]
    public float glowIntensity = 0.5f;

    [Header("Pulsing Effect")]
    public bool pulseGlow = true;

    [Range(0f, 5f)]
    public float pulseSpeed = 1.5f;

    [Range(0f, 1f)]
    public float pulseAmount = 0.25f;

    private Material[][] fragmentMaterials;

    private static readonly int EmissionColor =
        Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        if (fragmentRenderers == null ||
            fragmentRenderers.Length == 0)
        {
            Debug.LogWarning(
                "FragmentGlow: No fragment renderers assigned.",
                this
            );
            return;
        }

        fragmentMaterials = new Material[fragmentRenderers.Length][];

        for (int i = 0; i < fragmentRenderers.Length; i++)
        {
            if (fragmentRenderers[i] == null)
                continue;

            fragmentMaterials[i] = fragmentRenderers[i].materials;

            foreach (Material mat in fragmentMaterials[i])
            {
                if (mat != null && mat.HasProperty(EmissionColor))
                {
                    mat.EnableKeyword("_EMISSION");

                    mat.SetColor(
                        EmissionColor,
                        glowColor * glowIntensity
                    );
                }
            }
        }
    }

    void Update()
    {
        if (fragmentMaterials == null)
            return;

        float intensity = glowIntensity;

        if (pulseGlow)
        {
            float pulse = (Mathf.Sin(
                Time.time * pulseSpeed
            ) + 1f) * 0.5f;

            intensity *= 1f - pulseAmount +
                         pulseAmount * pulse;
        }

        foreach (Material[] materials in fragmentMaterials)
        {
            if (materials == null)
                continue;

            foreach (Material mat in materials)
            {
                if (mat != null && mat.HasProperty(EmissionColor))
                {
                    mat.SetColor(
                        EmissionColor,
                        glowColor * intensity
                    );
                }
            }
        }
    }

    void OnDestroy()
    {
        if (fragmentMaterials == null)
            return;

        foreach (Material[] materials in fragmentMaterials)
        {
            if (materials == null)
                continue;

            foreach (Material mat in materials)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }
}
