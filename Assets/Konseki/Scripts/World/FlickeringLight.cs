using UnityEngine;

namespace Konseki
{
    [RequireComponent(typeof(Light))]
    public sealed class FlickeringLight : MonoBehaviour
    {
        [SerializeField] private float amount = 0.12f;
        [SerializeField] private float speed = 2.2f;

        private Light cachedLight;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            cachedLight = GetComponent<Light>();
            baseIntensity = cachedLight.intensity;
            seed = transform.position.sqrMagnitude * 0.173f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
            cachedLight.intensity = baseIntensity * (1f + noise * amount);
        }
    }
}
