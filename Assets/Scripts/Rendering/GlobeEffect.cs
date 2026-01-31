using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Controls the globe wrapping visual effect that makes the map appear curved around a sphere
    /// </summary>
    public class GlobeEffect : MonoBehaviour
    {
        [Header("Globe Curvature Settings")]
        [Tooltip("Enable/disable the globe curvature effect")]
        public bool enableGlobeEffect = true;

        [Tooltip("Horizontal curvature strength (X-axis)")]
        [Range(0f, 0.01f)]
        public float curvatureX = 0.001f;

        [Tooltip("Vertical curvature strength (Y-axis) - main globe curve")]
        [Range(0f, 0.01f)]
        public float curvatureY = 0.002f;

        [Tooltip("Virtual globe radius")]
        [Range(10f, 200f)]
        public float globeRadius = 50f;

        [Tooltip("Globe center Y offset")]
        [Range(-50f, 0f)]
        public float globeCenterY = -20f;

        private Material globeMaterial;
        private HexRenderer hexRenderer;

        private void Start()
        {
            hexRenderer = GetComponent<HexRenderer>();
            if (hexRenderer != null)
            {
                Debug.Log("[GlobeEffect] Initializing globe curvature effect");
                ApplyGlobeEffect();
            }
            else
            {
                Debug.LogWarning("[GlobeEffect] No HexRenderer found, globe effect disabled");
            }
        }

        private void Update()
        {
            // Allow runtime adjustment of globe parameters
            if (enableGlobeEffect && globeMaterial != null)
            {
                UpdateGlobeParameters();
            }
        }

        public void ApplyGlobeEffect()
        {
            if (!enableGlobeEffect || hexRenderer == null)
                return;

            // Try to find the globe warp shader
            Shader globeShader = Shader.Find("Custom/GlobeWarp");
            if (globeShader == null)
            {
                Debug.LogWarning("[GlobeEffect] Globe warp shader not found, using default");
                return;
            }

            // Create material with globe shader
            globeMaterial = new Material(globeShader);
            UpdateGlobeParameters();

            // Apply to hex renderer
            if (hexRenderer.seaMaterial != null)
            {
                ApplyShaderToMaterial(hexRenderer.seaMaterial);
            }
            if (hexRenderer.landMaterial != null)
            {
                ApplyShaderToMaterial(hexRenderer.landMaterial);
            }
            if (hexRenderer.harborMaterial != null)
            {
                ApplyShaderToMaterial(hexRenderer.harborMaterial);
            }

            Debug.Log("[GlobeEffect] Globe curvature applied successfully");
        }

        private void ApplyShaderToMaterial(Material material)
        {
            if (material == null) return;

            // Replace shader while keeping existing color
            Color originalColor = material.color;
            material.shader = globeMaterial.shader;
            material.color = originalColor;

            // Copy globe parameters
            material.SetFloat("_CurvatureX", curvatureX);
            material.SetFloat("_CurvatureY", curvatureY);
            material.SetFloat("_GlobeRadius", globeRadius);
            material.SetFloat("_GlobeCenterY", globeCenterY);
        }

        private void UpdateGlobeParameters()
        {
            if (globeMaterial == null) return;

            globeMaterial.SetFloat("_CurvatureX", curvatureX);
            globeMaterial.SetFloat("_CurvatureY", curvatureY);
            globeMaterial.SetFloat("_GlobeRadius", globeRadius);
            globeMaterial.SetFloat("_GlobeCenterY", globeCenterY);

            // Update all hex materials
            if (hexRenderer != null)
            {
                if (hexRenderer.seaMaterial != null)
                {
                    hexRenderer.seaMaterial.SetFloat("_CurvatureX", curvatureX);
                    hexRenderer.seaMaterial.SetFloat("_CurvatureY", curvatureY);
                    hexRenderer.seaMaterial.SetFloat("_GlobeRadius", globeRadius);
                    hexRenderer.seaMaterial.SetFloat("_GlobeCenterY", globeCenterY);
                }
                if (hexRenderer.landMaterial != null)
                {
                    hexRenderer.landMaterial.SetFloat("_CurvatureX", curvatureX);
                    hexRenderer.landMaterial.SetFloat("_CurvatureY", curvatureY);
                    hexRenderer.landMaterial.SetFloat("_GlobeRadius", globeRadius);
                    hexRenderer.landMaterial.SetFloat("_GlobeCenterY", globeCenterY);
                }
                if (hexRenderer.harborMaterial != null)
                {
                    hexRenderer.harborMaterial.SetFloat("_CurvatureX", curvatureX);
                    hexRenderer.harborMaterial.SetFloat("_CurvatureY", curvatureY);
                    hexRenderer.harborMaterial.SetFloat("_GlobeRadius", globeRadius);
                    hexRenderer.harborMaterial.SetFloat("_GlobeCenterY", globeCenterY);
                }
            }
        }

        public void ToggleGlobeEffect(bool enabled)
        {
            enableGlobeEffect = enabled;
            if (enabled)
            {
                ApplyGlobeEffect();
            }
            else
            {
                // Reset to standard shader
                ResetToStandardShader();
            }
        }

        private void ResetToStandardShader()
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null)
                standardShader = Shader.Find("Legacy Shaders/Diffuse");

            if (hexRenderer != null && standardShader != null)
            {
                if (hexRenderer.seaMaterial != null)
                    hexRenderer.seaMaterial.shader = standardShader;
                if (hexRenderer.landMaterial != null)
                    hexRenderer.landMaterial.shader = standardShader;
                if (hexRenderer.harborMaterial != null)
                    hexRenderer.harborMaterial.shader = standardShader;
            }
        }

        private void OnDestroy()
        {
            if (globeMaterial != null)
            {
                Destroy(globeMaterial);
            }
        }
    }
}
