using UnityEngine;

namespace Konseki
{
    public sealed class PixelRain : MonoBehaviour
    {
        private readonly Vector3 halfExtents = new Vector3(4.2f, 8f, 44f);
        private Transform[] drops;
        private float[] speeds;
        private float updateAccumulator;
        private const float UpdateInterval = 1f / 30f;

        public void Configure(Material material, int dropCount)
        {
            int count = Mathf.Clamp(dropCount, 32, 240);
            drops = new Transform[count];
            speeds = new float[count];

            System.Random random = new System.Random(1978);
            for (int i = 0; i < count; i++)
            {
                GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                drop.name = "Rain pixel";
                drop.transform.SetParent(transform, false);
                drop.transform.localPosition = RandomPosition(random);
                drop.transform.localScale = new Vector3(0.025f, Range(random, 0.26f, 0.62f), 0.025f);
                drop.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
                drop.GetComponent<Renderer>().sharedMaterial = material;

                Collider collider = drop.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                    if (Application.isPlaying)
                    {
                        Destroy(collider);
                    }
                    else
                    {
                        DestroyImmediate(collider);
                    }
                }

                drops[i] = drop.transform;
                speeds[i] = Range(random, 10f, 15f);
            }
        }

        private void Update()
        {
            if (drops == null)
            {
                return;
            }

            updateAccumulator += Time.deltaTime;
            if (updateAccumulator < UpdateInterval)
            {
                return;
            }

            float delta = updateAccumulator;
            updateAccumulator = 0f;
            for (int i = 0; i < drops.Length; i++)
            {
                Transform drop = drops[i];
                Vector3 position = drop.localPosition;
                position += new Vector3(-1.15f, -speeds[i], 0f) * delta;

                if (position.y < -halfExtents.y)
                {
                    position.y = halfExtents.y;
                    position.x += 1.3f;
                    if (position.x > halfExtents.x)
                    {
                        position.x -= halfExtents.x * 2f;
                    }
                }

                drop.localPosition = position;
            }
        }

        private Vector3 RandomPosition(System.Random random)
        {
            return new Vector3(
                Range(random, -halfExtents.x, halfExtents.x),
                Range(random, -halfExtents.y, halfExtents.y),
                Range(random, -halfExtents.z, halfExtents.z));
        }

        private static float Range(System.Random random, float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }
    }
}
