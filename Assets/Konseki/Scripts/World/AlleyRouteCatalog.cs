using UnityEngine;

namespace Konseki
{
    public static class AlleyRouteCatalog
    {
        public static Vector3[] CreateTargetRoute()
        {
            return new[]
            {
                new Vector3(0.5f, 0.05f, 12f),
                new Vector3(0.2f, 0.05f, 23f),
                new Vector3(4.5f, 0.05f, 26.65f),
                new Vector3(12.8f, 0.05f, 26.65f),
                new Vector3(13.2f, 0.05f, 38f),
                new Vector3(13.2f, 0.05f, 51.7f),
                new Vector3(4.5f, 0.05f, 51.7f),
                new Vector3(0f, 0.05f, 52.5f),
                new Vector3(0f, 0.05f, 57f),
                new Vector3(-4.5f, 0.05f, 60.05f),
                new Vector3(-12.8f, 0.05f, 60.05f),
                new Vector3(-13.2f, 0.05f, 68.5f),
                new Vector3(-13.2f, 0.05f, 76.75f),
                new Vector3(-4.5f, 0.05f, 76.75f),
                new Vector3(0f, 0.05f, 78f),
                new Vector3(0.2f, 0.05f, 84f)
            };
        }
    }
}
