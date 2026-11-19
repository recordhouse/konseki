using UnityEngine;
using UnityEngine.Rendering;

namespace Konseki
{
    // Serialized entry point used by the editor baker and the lightweight runtime loader.
    [DisallowMultipleComponent]
    public sealed class BakedAlleyLevel : MonoBehaviour
    {
        [SerializeField] private int bakeVersion;
        [SerializeField] private string sourceSignature;
        [SerializeField] private AlleyVariant variant;
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private TargetWalker target;

        [Header("Captured atmosphere")]
        [SerializeField] private bool fogEnabled;
        [SerializeField] private FogMode fogMode;
        [SerializeField] private float fogDensity;
        [SerializeField] private Color fogColor;
        [SerializeField] private AmbientMode ambientMode;
        [SerializeField] private Color ambientSky;
        [SerializeField] private Color ambientEquator;
        [SerializeField] private Color ambientGround;
        [SerializeField] private float ambientIntensity;
        [SerializeField] private Vector3 gravity;
        [SerializeField] private float shadowDistance;

        public int BakeVersion => bakeVersion;
        public string SourceSignature => sourceSignature;
        public AlleyVariant Variant => variant;

        public static string ResourcePath(AlleyVariant alleyVariant)
        {
            return $"Levels/{alleyVariant}";
        }

        public void ConfigureForBake(
            int version,
            string signature,
            AlleyVariant alleyVariant,
            Transform environment,
            NightAlleyWorldBuilder.BuildResult result)
        {
            bakeVersion = version;
            sourceSignature = signature;
            variant = alleyVariant;
            environmentRoot = environment;
            player = result.Player;
            target = result.Target;

            fogEnabled = RenderSettings.fog;
            fogMode = RenderSettings.fogMode;
            fogDensity = RenderSettings.fogDensity;
            fogColor = RenderSettings.fogColor;
            ambientMode = RenderSettings.ambientMode;
            ambientSky = RenderSettings.ambientSkyColor;
            ambientEquator = RenderSettings.ambientEquatorColor;
            ambientGround = RenderSettings.ambientGroundColor;
            ambientIntensity = RenderSettings.ambientIntensity;
            gravity = Physics.gravity;
            shadowDistance = QualitySettings.shadowDistance;
        }

        public bool Matches(int version, string signature)
        {
            return bakeVersion == version && sourceSignature == signature;
        }

        public static bool TryInstantiate(Transform parent, out NightAlleyWorldBuilder.BuildResult result)
        {
            result = null;
            AlleyVariant current = AlleyVariantInfo.Current;
            GameObject prefab = Resources.Load<GameObject>(ResourcePath(current));
            if (prefab == null)
            {
                return false;
            }

            GameObject instance = Instantiate(prefab, parent);
            instance.name = "Baked World";
            BakedAlleyLevel level = instance.GetComponent<BakedAlleyLevel>();
            if (level == null || level.player == null || level.target == null)
            {
                Destroy(instance);
                return false;
            }

            level.ApplyRuntimeSettings();
            result = new NightAlleyWorldBuilder.BuildResult
            {
                Player = level.player,
                Target = level.target,
                Variant = level.variant
            };
            return true;
        }

        private void ApplyRuntimeSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fogColor;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            Physics.gravity = gravity;
            QualitySettings.shadowDistance = shadowDistance;

            if (environmentRoot != null)
            {
                NightAlleyWorldBuilder.CombineStaticGeometry(environmentRoot);
            }
        }
    }
}
