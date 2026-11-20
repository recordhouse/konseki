using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Konseki.EditorTools
{
    public static class VSCodeUnitySetup
    {
        private const string VSCodeAppPath = "/Applications/Visual Studio Code.app";

        public static void Configure()
        {
            EditorPrefs.SetString("kScriptsDefaultApp", VSCodeAppPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Type syncType = typeof(Editor).Assembly.GetType("UnityEditor.SyncVS");
            MethodInfo syncMethod = syncType?.GetMethod(
                "SyncSolution",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (syncMethod == null)
            {
                Debug.LogError("[Konseki] Unity solution synchronization API was not found.");
                EditorApplication.Exit(1);
                return;
            }

            syncMethod.Invoke(null, null);
            Debug.Log("[Konseki] Visual Studio Code selected and Unity project files synchronized.");
            EditorApplication.Exit(0);
        }
    }
}
