using UnityEngine;
using UnityEngine.SceneManagement;

namespace Konseki
{
    public sealed class AlleyVariantSwitcher : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchTo(AlleyVariant.ShowaIzakaya);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchTo(AlleyVariant.NeonYokocho);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwitchTo(AlleyVariant.QuietResidential);
            }
        }

        private static void SwitchTo(AlleyVariant variant)
        {
            if (AlleyVariantInfo.Current == variant)
            {
                return;
            }

            AlleyVariantInfo.Current = variant;
            SceneManager.LoadScene("NightAlley");
        }
    }
}
