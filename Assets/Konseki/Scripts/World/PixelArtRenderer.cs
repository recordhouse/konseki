using UnityEngine;

namespace Konseki
{
    [RequireComponent(typeof(Camera))]
    public sealed class PixelArtRenderer : MonoBehaviour
    {
        [SerializeField, Range(240, 720)] private int verticalResolution = 480;
        [SerializeField, Range(12, 64)] private int colorSteps = 48;
        [SerializeField, Range(0f, 0.25f)] private float ditherStrength = 0.02f;
        [SerializeField, Range(0.5f, 1f)] private float resolutionScale = 0.65f;

        private Material pixelMaterial;

        public void Configure(int resolution, int paletteSteps, float dither)
        {
            verticalResolution = resolution;
            colorSteps = paletteSteps;
            ditherStrength = dither;
        }

        private void Awake()
        {
            Shader shader = Shader.Find("Hidden/Konseki/PixelArt");
            if (shader != null)
            {
                pixelMaterial = new Material(shader)
                {
                    name = "Konseki Pixel Art Post Effect",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (pixelMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int scaledHeight = Mathf.RoundToInt(source.height * resolutionScale);
            int height = Mathf.Clamp(Mathf.Max(verticalResolution, scaledHeight), 64, source.height);
            int width = Mathf.Max(64, Mathf.RoundToInt(height * source.width / (float)source.height));
            RenderTexture lowResolution = RenderTexture.GetTemporary(width, height, 0, source.format);
            lowResolution.filterMode = FilterMode.Point;
            lowResolution.wrapMode = TextureWrapMode.Clamp;

            Graphics.Blit(source, lowResolution);
            pixelMaterial.SetFloat("_ColorSteps", colorSteps);
            pixelMaterial.SetFloat("_DitherStrength", ditherStrength);
            Graphics.Blit(lowResolution, destination, pixelMaterial);
            RenderTexture.ReleaseTemporary(lowResolution);
        }

        private void OnDestroy()
        {
            if (pixelMaterial != null)
            {
                Destroy(pixelMaterial);
            }
        }
    }
}
