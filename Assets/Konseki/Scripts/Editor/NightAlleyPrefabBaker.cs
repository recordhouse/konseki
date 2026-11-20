using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Konseki.EditorTools
{
    [InitializeOnLoad]
    public static class NightAlleyPrefabBaker
    {
        public const int BakeVersion = 2;

        private const string LevelsFolder = "Assets/Konseki/Resources/Levels";
        private const string GeneratedAssetsFolder = "Assets/Konseki/Generated/BakedAssets";

        private static readonly AlleyVariant[] Variants =
        {
            AlleyVariant.QuietResidential
        };

        private static readonly string[] SourceAssets =
        {
            "Assets/Konseki/Scripts/World/NightAlleyWorldBuilder.cs",
            "Assets/Konseki/Scripts/World/BakedAlleyLevel.cs",
            "Assets/Konseki/Scripts/World/AlleyRouteCatalog.cs",
            "Assets/Konseki/Scripts/World/PixelArtRenderer.cs",
            "Assets/Konseki/Scripts/World/FlickeringLight.cs",
            "Assets/Konseki/Scripts/NPC/TargetWalker.cs",
            "Assets/Konseki/Scripts/Player/FirstPersonController.cs",
            "Assets/Konseki/Resources/Shaders/PixelArt.shader",
            "Assets/Konseki/Resources/PixelArt/quiet-residential-wall.png"
        };

        private static bool isBaking;
        private static bool autoBakeQueued;

        static NightAlleyPrefabBaker()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            QueueAutoBake();
        }

        [MenuItem("Konseki/Level/Bake Quiet Residential Prefab")]
        public static void BakeAllFromMenu()
        {
            BakeAll(true);
        }

        [MenuItem("Konseki/Level/Bake Quiet Residential Prefab", true)]
        private static bool ValidateBakeAllFromMenu()
        {
            return !isBaking && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                QueueAutoBake();
            }
        }

        private static void QueueAutoBake()
        {
            if (autoBakeQueued || Application.isBatchMode)
            {
                return;
            }

            autoBakeQueued = true;
            EditorApplication.delayCall += AutoBakeIfNeeded;
        }

        private static void AutoBakeIfNeeded()
        {
            autoBakeQueued = false;
            if (isBaking || Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueAutoBake();
                return;
            }

            string signature = ComputeSourceSignature();
            if (NeedsBake(signature))
            {
                BakeAll(false);
            }
        }

        private static bool NeedsBake(string signature)
        {
            for (int i = 0; i < Variants.Length; i++)
            {
                string prefabPath = PrefabPath(Variants[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                BakedAlleyLevel marker = prefab == null ? null : prefab.GetComponent<BakedAlleyLevel>();
                if (marker == null || !marker.Matches(BakeVersion, signature))
                {
                    return true;
                }
            }

            return false;
        }

        private static void BakeAll(bool userInitiated)
        {
            if (isBaking || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            isBaking = true;
            try
            {
                EnsureFolder(LevelsFolder);
                EnsureFolder(GeneratedAssetsFolder);
                string signature = ComputeSourceSignature();

                for (int i = 0; i < Variants.Length; i++)
                {
                    BakeVariant(Variants[i], signature);
                }

                AssetDatabase.SaveAssets();
                ValidateBakedPrefabs(signature);
                AssetDatabase.Refresh();
                Debug.Log(userInitiated
                    ? "[Konseki] 고요한 주택가 Prefab을 다시 생성했습니다."
                    : "[Konseki] 소스 변경을 감지해 고요한 주택가 Prefab을 자동 생성했습니다.");
            }
            finally
            {
                isBaking = false;
            }
        }

        private static void BakeVariant(AlleyVariant variant, string signature)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            bool ownsBakeScene = !Application.isBatchMode;
            Scene bakeScene = ownsBakeScene
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)
                : previousScene;
            GameObject root = null;

            try
            {
                if (ownsBakeScene && !SceneManager.SetActiveScene(bakeScene))
                {
                    throw new System.InvalidOperationException("임시 베이크 씬을 활성화하지 못했습니다.");
                }

                root = new GameObject($"NightAlley_{variant}");

                NightAlleyWorldBuilder.BuildResult result = NightAlleyWorldBuilder.Build(root.transform, variant, false);
                Transform environment = root.transform.childCount > 0 ? root.transform.GetChild(0) : null;
                BakedAlleyLevel marker = root.AddComponent<BakedAlleyLevel>();
                marker.ConfigureForBake(BakeVersion, signature, variant, environment, result);

                PersistRendererAssets(root, variant);
                bool saved;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(variant), out saved);
                if (!saved)
                {
                    throw new System.InvalidOperationException($"{variant} Prefab 저장에 실패했습니다.");
                }
            }
            finally
            {
                if (!ownsBakeScene && root != null)
                {
                    Object.DestroyImmediate(root);
                }

                if (previousScene.IsValid())
                {
                    SceneManager.SetActiveScene(previousScene);
                }

                if (ownsBakeScene)
                {
                    EditorSceneManager.CloseScene(bakeScene, true);
                }
            }
        }

        private static void PersistRendererAssets(GameObject root, AlleyVariant variant)
        {
            string variantFolder = $"{GeneratedAssetsFolder}/{variant}";
            string materialsFolder = $"{variantFolder}/Materials";
            string texturesFolder = $"{variantFolder}/Textures";
            EnsureFolder(variantFolder);
            EnsureFolder(materialsFolder);
            EnsureFolder(texturesFolder);

            Dictionary<Material, Material> materialMap = new Dictionary<Material, Material>();
            Dictionary<Texture, Texture> textureMap = new Dictionary<Texture, Texture>();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null || EditorUtility.IsPersistent(material))
                    {
                        continue;
                    }

                    Material persisted;
                    if (!materialMap.TryGetValue(material, out persisted))
                    {
                        persisted = PersistMaterial(material, materialsFolder, texturesFolder, textureMap);
                        materialMap.Add(material, persisted);
                    }

                    materials[materialIndex] = persisted;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private static Material PersistMaterial(
            Material material,
            string materialsFolder,
            string texturesFolder,
            Dictionary<Texture, Texture> textureMap)
        {
            PersistMaterialTextures(material, texturesFolder, textureMap);
            string path = $"{materialsFolder}/{SafeAssetName(material.name)}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);
                return material;
            }

            EditorUtility.CopySerialized(material, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void PersistMaterialTextures(Material material, string texturesFolder, Dictionary<Texture, Texture> textureMap)
        {
            Shader shader = material.shader;
            if (shader == null)
            {
                return;
            }

            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    continue;
                }

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture texture = material.GetTexture(propertyName);
                if (texture == null || EditorUtility.IsPersistent(texture))
                {
                    continue;
                }

                Texture persisted;
                if (!textureMap.TryGetValue(texture, out persisted))
                {
                    persisted = PersistTexture(texture, texturesFolder);
                    textureMap.Add(texture, persisted);
                }

                material.SetTexture(propertyName, persisted);
            }
        }

        private static Texture PersistTexture(Texture texture, string texturesFolder)
        {
            string path = $"{texturesFolder}/{SafeAssetName(texture.name)}.asset";
            Texture existing = AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(texture, path);
                return texture;
            }

            EditorUtility.CopySerialized(texture, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static string ComputeSourceSignature()
        {
            StringBuilder builder = new StringBuilder(SourceAssets.Length * 40);
            for (int i = 0; i < SourceAssets.Length; i++)
            {
                builder.Append(AssetDatabase.GetAssetDependencyHash(SourceAssets[i]));
                builder.Append('|');
            }

            return builder.ToString();
        }

        private static void ValidateBakedPrefabs(string signature)
        {
            int rendererCount = 0;
            for (int i = 0; i < Variants.Length; i++)
            {
                AlleyVariant variant = Variants[i];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(variant));
                BakedAlleyLevel marker = prefab == null ? null : prefab.GetComponent<BakedAlleyLevel>();
                FirstPersonController player = prefab == null ? null : prefab.GetComponentInChildren<FirstPersonController>(true);
                TargetWalker target = prefab == null ? null : prefab.GetComponentInChildren<TargetWalker>(true);

                if (prefab == null || marker == null || player == null || target == null || !marker.Matches(BakeVersion, signature))
                {
                    throw new System.InvalidOperationException($"{variant} Prefab의 핵심 참조 검증에 실패했습니다.");
                }

                SerializedObject targetData = new SerializedObject(target);
                SerializedProperty route = targetData.FindProperty("route");
                if (route == null || !route.isArray || route.arraySize < 2)
                {
                    throw new System.InvalidOperationException($"{variant} Prefab에 타깃 경로가 저장되지 않았습니다.");
                }

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                rendererCount += renderers.Length;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Material[] materials = renderers[rendererIndex].sharedMaterials;
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        Material material = materials[materialIndex];
                        if (material == null || !EditorUtility.IsPersistent(material))
                        {
                            throw new System.InvalidOperationException($"{variant} Prefab에 저장되지 않은 재질 참조가 있습니다.");
                        }
                    }
                }
            }

            Debug.Log($"[Konseki] Prefab 검증 완료: 고요한 주택가, Renderer {rendererCount}개.");
        }

        private static string PrefabPath(AlleyVariant variant)
        {
            return $"{LevelsFolder}/{variant}.prefab";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string SafeAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Generated Asset";
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(char.IsLetterOrDigit(character) || character == ' ' || character == '-' || character == '_'
                    ? character
                    : '_');
            }

            return builder.ToString();
        }
    }
}
