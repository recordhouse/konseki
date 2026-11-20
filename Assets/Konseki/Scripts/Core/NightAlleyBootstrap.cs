using UnityEngine;
using UnityEngine.SceneManagement;

namespace Konseki
{
    public sealed class NightAlleyBootstrap : MonoBehaviour
    {
        private static bool isBuilding;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapExists()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || FindObjectOfType<NightAlleyBootstrap>() != null)
            {
                return;
            }

            new GameObject("Konseki Prototype").AddComponent<NightAlleyBootstrap>();
        }

        private void Awake()
        {
            if (isBuilding || transform.Find("Generated World") != null)
            {
                return;
            }

            isBuilding = true;
            try
            {
                gameObject.AddComponent<PerformanceGovernor>();
                Transform generatedWorld = new GameObject("Generated World").transform;
                generatedWorld.SetParent(transform);

                NightAlleyWorldBuilder.BuildResult result;
                if (!BakedAlleyLevel.TryInstantiate(generatedWorld, out result))
                {
                    result = NightAlleyWorldBuilder.Build(generatedWorld);
                }

                TailGameManager manager = gameObject.AddComponent<TailGameManager>();
                manager.Configure(result.Player, result.Target);

                TailHud hud = gameObject.AddComponent<TailHud>();
                hud.Configure(manager);
            }
            finally
            {
                isBuilding = false;
            }
        }
    }
}
