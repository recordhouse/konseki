using UnityEngine;

namespace Konseki
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class PerformanceGovernor : MonoBehaviour
    {
        private const int ForegroundFrameRate = 60;
        private const int BackgroundFrameRate = 15;
        private const int HighQualityIndex = 3;

        private void Awake()
        {
            Application.runInBackground = false;

            string[] qualityNames = QualitySettings.names;
            if (qualityNames.Length > 0)
            {
                int qualityIndex = Mathf.Min(HighQualityIndex, qualityNames.Length - 1);
                QualitySettings.SetQualityLevel(qualityIndex, true);
            }

            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 2;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.shadowDistance = 32f;
            Application.targetFrameRate = ForegroundFrameRate;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Application.targetFrameRate = hasFocus ? ForegroundFrameRate : BackgroundFrameRate;
        }

        private void OnApplicationPause(bool isPaused)
        {
            Application.targetFrameRate = isPaused ? BackgroundFrameRate : ForegroundFrameRate;
        }
    }
}
