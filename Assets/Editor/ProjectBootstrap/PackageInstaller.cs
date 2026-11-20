using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Konseki.EditorTools
{
    public static class PackageInstaller
    {
        private static readonly string[] PackagesToAdd =
        {
            "com.unity.ide.visualstudio@2.0.29"
        };

        private static readonly string[] UniVrmPackagesToAdd =
        {
            "https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.3",
            "https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.3"
        };

        private const double TimeoutSeconds = 600d;

        private static AddAndRemoveRequest request;
        private static double deadline;

        public static void InstallGameDevelopmentTooling()
        {
            Install(PackagesToAdd);
        }

        public static void InstallUniVrm()
        {
            Install(UniVrmPackagesToAdd);
        }

        private static void Install(string[] packages)
        {
            Debug.Log($"[Konseki] Installing packages: {string.Join(", ", packages)}");
            request = Client.AddAndRemove(packages, new string[0]);
            deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (request == null)
            {
                return;
            }

            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[Konseki] Timed out while installing Unity packages.");
                    EditorApplication.Exit(2);
                }

                return;
            }

            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
            {
                string resolved = string.Join(", ", request.Result.Select(package => $"{package.name}@{package.version}"));
                Debug.Log($"[Konseki] Resolved packages: {resolved}");
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError($"[Konseki] Package installation failed: {request.Error?.message}");
            EditorApplication.Exit(1);
        }
    }
}
