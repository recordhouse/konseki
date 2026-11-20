using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konseki.EditorTools
{
    public static class CartoonTargetCharacterBuilder
    {
        private const string ShaderPath = "Assets/Konseki/Resources/Shaders/CartoonTarget.shader";
        private const string GeneratedFolder = "Assets/Konseki/Generated/CartoonTarget";
        private const string MaterialFolder = GeneratedFolder + "/Materials";
        private const string MeshFolder = GeneratedFolder + "/Meshes";
        private const string ModelPath = "Assets/Konseki/Models/CartoonTarget/CartoonTarget.fbx";
        private const string PrefabPath = "Assets/Konseki/Resources/Characters/TargetWoman3D.prefab";

        private sealed class Palette
        {
            public Material Skin;
            public Material Hair;
            public Material Blouse;
            public Material Navy;
            public Material Skirt;
            public Material Shoe;
            public Material EyeWhite;
            public Material EyeDark;
            public Material Blush;
            public Material Lip;
        }

        private sealed class Rig
        {
            public Transform Body;
            public Transform Torso;
            public Transform Skirt;
            public Transform Head;
            public Transform Hair;
            public Transform LeftUpperArm;
            public Transform RightUpperArm;
            public Transform LeftForearm;
            public Transform RightForearm;
            public Transform LeftThigh;
            public Transform RightThigh;
            public Transform LeftCalf;
            public Transform RightCalf;
            public Transform LeftFoot;
            public Transform RightFoot;
        }

        [MenuItem("Konseki/Characters/Build Cartoon 3D Target Woman")]
        public static void BuildFromMenu()
        {
            BuildCharacter(true);
        }

        public static void SetupFromCommandLine()
        {
            BuildCharacter(false);
        }

        [MenuItem("Konseki/Characters/Capture Cartoon Target Preview")]
        public static void CapturePreviewFromMenu()
        {
            CapturePreview(
                "/tmp/konseki-cartoon-target-front.png",
                "/tmp/konseki-cartoon-target-back.png",
                "/tmp/konseki-cartoon-target-side.png");
        }

        public static void CapturePreviewFromCommandLine()
        {
            CapturePreview(
                "/tmp/konseki-cartoon-target-front.png",
                "/tmp/konseki-cartoon-target-back.png",
                "/tmp/konseki-cartoon-target-side.png");
        }

        private static void BuildCharacter(bool fromMenu)
        {
            EnsureFolder(GeneratedFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder("Assets/Konseki/Resources/Characters");
            PrepareImportedModel();

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                throw new InvalidOperationException("CartoonTarget Shader를 찾지 못했습니다.");
            }

            Palette palette = CreatePalette(shader);
            GameObject root = new GameObject("TargetWoman3D");
            try
            {
                GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                Rig rig;
                if (importedModel != null)
                {
                    rig = BuildImportedModel(root.transform, importedModel, palette);
                }
                else
                {
                    Debug.LogWarning("[Konseki] Blender FBX를 찾지 못해 Unity 절차 모델을 사용합니다.");
                    Mesh skirtMesh = CreateOrUpdateMesh(
                        MeshFolder + "/PleatedSkirt.asset",
                        CreatePleatedSkirtMesh(16, 0.265f, 0.19f, 0.365f, 0.275f, 0.345f));
                    Mesh torsoMesh = CreateOrUpdateMesh(
                        MeshFolder + "/BlouseTorso.asset",
                        CreateStylizedTorsoMesh(24));
                    rig = BuildModel(root.transform, palette, skirtMesh, torsoMesh);
                    OptimizeRigGeometry(rig);
                }

                CartoonTargetAnimator characterAnimator = root.AddComponent<CartoonTargetAnimator>();
                characterAnimator.Configure(
                    root.transform,
                    rig.Body,
                    rig.Torso,
                    rig.Skirt,
                    rig.Head,
                    rig.Hair,
                    rig.LeftUpperArm,
                    rig.RightUpperArm,
                    rig.LeftForearm,
                    rig.RightForearm,
                    rig.LeftThigh,
                    rig.RightThigh,
                    rig.LeftCalf,
                    rig.RightCalf,
                    rig.LeftFoot,
                    rig.RightFoot);

                bool saved;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out saved);
                if (!saved)
                {
                    throw new InvalidOperationException("Cartoon TargetWoman3D Prefab 저장에 실패했습니다.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidatePrefab();
            NightAlleyPrefabBaker.BakeAllFromMenu();
            Debug.Log(fromMenu
                ? "[Konseki] 참고 이미지 기반 Cartoon Rendering 3D 타깃을 생성했습니다."
                : "[Konseki] CARTOON_TARGET_CHARACTER_READY");
        }

        private static Palette CreatePalette(Shader shader)
        {
            return new Palette
            {
                Skin = CreateOrUpdateMaterial(
                    "Skin",
                    shader,
                    new Color(0.91f, 0.68f, 0.60f),
                    new Color(0.56f, 0.40f, 0.49f),
                    new Color(0.95f, 0.72f, 0.74f),
                    0.16f,
                    0.0035f),
                Hair = CreateOrUpdateMaterial(
                    "Hair",
                    shader,
                    new Color(0.12f, 0.075f, 0.095f),
                    new Color(0.32f, 0.22f, 0.34f),
                    new Color(0.32f, 0.22f, 0.42f),
                    0.22f,
                    0.0045f),
                Blouse = CreateOrUpdateMaterial(
                    "Blouse",
                    shader,
                    new Color(0.91f, 0.89f, 0.89f),
                    new Color(0.44f, 0.48f, 0.62f),
                    new Color(0.70f, 0.78f, 1f),
                    0.12f,
                    0.004f),
                Navy = CreateOrUpdateMaterial(
                    "Navy",
                    shader,
                    new Color(0.13f, 0.19f, 0.42f),
                    new Color(0.55f, 0.58f, 0.82f),
                    new Color(0.42f, 0.56f, 0.92f),
                    0.24f,
                    0.0045f),
                Skirt = CreateOrUpdateMaterial(
                    "Skirt Navy",
                    shader,
                    new Color(0.12f, 0.16f, 0.38f),
                    new Color(0.78f, 0.80f, 0.96f),
                    new Color(0.56f, 0.68f, 1f),
                    0.24f,
                    0.0045f),
                Shoe = CreateOrUpdateMaterial(
                    "Shoe",
                    shader,
                    new Color(0.055f, 0.045f, 0.065f),
                    new Color(0.22f, 0.18f, 0.30f),
                    new Color(0.48f, 0.43f, 0.62f),
                    0.25f,
                    0.0045f),
                EyeWhite = CreateOrUpdateMaterial(
                    "Eye White",
                    shader,
                    new Color(0.94f, 0.94f, 0.91f),
                    new Color(0.62f, 0.65f, 0.76f),
                    Color.white,
                    0.08f,
                    0.0015f),
                EyeDark = CreateOrUpdateMaterial(
                    "Eye Dark",
                    shader,
                    new Color(0.045f, 0.027f, 0.04f),
                    new Color(0.15f, 0.10f, 0.20f),
                    new Color(0.38f, 0.24f, 0.44f),
                    0.24f,
                    0.0015f),
                Blush = CreateOrUpdateMaterial(
                    "Blush",
                    shader,
                    new Color(0.90f, 0.38f, 0.45f),
                    new Color(0.58f, 0.24f, 0.32f),
                    new Color(1f, 0.66f, 0.72f),
                    0.18f,
                    0.001f),
                Lip = CreateOrUpdateMaterial(
                    "Lip",
                    shader,
                    new Color(0.55f, 0.20f, 0.26f),
                    new Color(0.34f, 0.16f, 0.24f),
                    new Color(0.90f, 0.43f, 0.52f),
                    0.25f,
                    0.0015f)
            };
        }

        private static Material CreateOrUpdateMaterial(
            string name,
            Shader shader,
            Color baseColor,
            Color shadowColor,
            Color rimColor,
            float rimStrength,
            float outlineWidth)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_Color", baseColor);
            material.SetColor("_ShadowColor", shadowColor);
            material.SetColor("_RimColor", rimColor);
            material.SetColor("_OutlineColor", new Color(0.025f, 0.018f, 0.045f, 1f));
            material.SetFloat("_RimStrength", rimStrength);
            material.SetFloat("_RimPower", 3.2f);
            material.SetFloat("_ShadowThreshold", 0.03f);
            material.SetFloat("_ShadowSoftness", 0.065f);
            material.SetFloat("_OutlineWidth", outlineWidth);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void PrepareImportedModel()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null && File.Exists(ModelPath))
            {
                AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            }

            if (importer == null)
            {
                return;
            }

            bool requiresReimport =
                importer.animationType != ModelImporterAnimationType.Generic ||
                importer.importAnimation ||
                importer.importBlendShapes ||
                importer.importCameras ||
                importer.importLights ||
                importer.isReadable ||
                importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard ||
                importer.materialLocation != ModelImporterMaterialLocation.InPrefab ||
                importer.meshCompression != ModelImporterMeshCompression.Medium ||
                !importer.optimizeMeshPolygons ||
                !importer.optimizeMeshVertices;

            if (!requiresReimport)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.SaveAndReimport();
        }

        private static Rig BuildImportedModel(Transform root, GameObject sourceModel, Palette palette)
        {
            Rig rig = new Rig();
            Transform visual = CreateTransform("Cartoon Visual", root, Vector3.zero, Quaternion.identity);
            rig.Body = CreateTransform("Body Motion", visual, Vector3.zero, Quaternion.identity);

            GameObject model = PrefabUtility.InstantiatePrefab(sourceModel, rig.Body) as GameObject;
            if (model == null)
            {
                throw new InvalidOperationException("Blender CartoonTarget FBX 인스턴스 생성에 실패했습니다.");
            }

            model.name = "Blender Cartoon Adult Woman";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            Animator[] importedAnimators = model.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < importedAnimators.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(importedAnimators[i]);
            }

            ApplyImportedMaterials(model, palette);
            NormalizeImportedCharacter(model, 1.80f);

            rig.Torso = FindRequiredTransform(model.transform, "Chest");
            rig.Skirt = FindRequiredTransform(model.transform, "Skirt");
            rig.Head = FindRequiredTransform(model.transform, "Head");
            rig.Hair = FindRequiredTransform(model.transform, "Hair");
            rig.LeftUpperArm = FindRequiredTransform(model.transform, "LeftUpperArm");
            rig.RightUpperArm = FindRequiredTransform(model.transform, "RightUpperArm");
            rig.LeftForearm = FindRequiredTransform(model.transform, "LeftLowerArm");
            rig.RightForearm = FindRequiredTransform(model.transform, "RightLowerArm");
            rig.LeftThigh = FindRequiredTransform(model.transform, "LeftUpperLeg");
            rig.RightThigh = FindRequiredTransform(model.transform, "RightUpperLeg");
            rig.LeftCalf = FindRequiredTransform(model.transform, "LeftLowerLeg");
            rig.RightCalf = FindRequiredTransform(model.transform, "RightLowerLeg");
            rig.LeftFoot = FindRequiredTransform(model.transform, "LeftFoot");
            rig.RightFoot = FindRequiredTransform(model.transform, "RightFoot");
            return rig;
        }

        private static void ApplyImportedMaterials(GameObject model, Palette palette)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Blender CartoonTarget FBX에 Renderer가 없습니다.");
            }

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] imported = renderer.sharedMaterials;
                Material[] replacements = new Material[Mathf.Max(1, imported.Length)];
                for (int materialIndex = 0; materialIndex < replacements.Length; materialIndex++)
                {
                    string materialName = imported.Length > materialIndex && imported[materialIndex] != null
                        ? imported[materialIndex].name
                        : string.Empty;
                    replacements[materialIndex] = ResolveImportedMaterial(
                        materialName,
                        renderer.gameObject.name,
                        palette);
                }

                renderer.sharedMaterials = replacements;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null)
                {
                    skinned.updateWhenOffscreen = false;
                    skinned.skinnedMotionVectors = false;
                }
            }
        }

        private static Material ResolveImportedMaterial(
            string materialName,
            string rendererName,
            Palette palette)
        {
            string key = (materialName + " " + rendererName).ToLowerInvariant();
            if (key.Contains("eye white") || key.Contains("eyewhite") || key.Contains("highlight")) return palette.EyeWhite;
            if (key.Contains("blush")) return palette.Blush;
            if (key.Contains("lip") || key.Contains("mouth")) return palette.Lip;
            if (key.Contains("iris") || key.Contains("eye dark") || key.Contains("eyedark") || key.Contains("pupil")) return palette.EyeDark;
            if (key.Contains("hair") || key.Contains("eyebrow")) return palette.Hair;
            if (key.Contains("blouse")) return palette.Blouse;
            if (key.Contains("skirt")) return palette.Skirt;
            if (key.Contains("navy") || key.Contains("sock") || key.Contains("collar") || key.Contains("ribbon")) return palette.Navy;
            if (key.Contains("shoe") || key.Contains("loafer") || key.Contains("sole")) return palette.Shoe;
            if (key.Contains("skin") || key.Contains("face") || key.Contains("neck") || key.Contains("hand") || key.Contains("leg")) return palette.Skin;
            Debug.LogWarning($"[Konseki] 알 수 없는 CartoonTarget 재질 '{materialName}' ({rendererName})을 Skin으로 대체합니다.");
            return palette.Skin;
        }

        private static void NormalizeImportedCharacter(GameObject model, float targetHeight)
        {
            Bounds bounds = CalculateRendererBounds(model);
            if (bounds.size.y <= 0.001f)
            {
                throw new InvalidOperationException("Blender CartoonTarget FBX bounds가 비어 있습니다.");
            }

            float scale = targetHeight / bounds.size.y;
            model.transform.localScale = Vector3.one * scale;
            bounds = CalculateRendererBounds(model);
            model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        private static Bounds CalculateRendererBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(model.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Transform FindRequiredTransform(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            throw new InvalidOperationException($"Blender CartoonTarget 뼈 '{name}'을 찾지 못했습니다.");
        }

        private static Rig BuildModel(Transform root, Palette palette, Mesh skirtMesh, Mesh torsoMesh)
        {
            Rig rig = new Rig();
            Transform visual = CreateTransform("Cartoon Visual", root, Vector3.zero, Quaternion.identity);
            visual.localScale = Vector3.one * 0.90f;
            visual.localPosition = new Vector3(0f, 0.115f, 0f);
            rig.Body = CreateTransform("Body Motion", visual, Vector3.zero, Quaternion.identity);

            CreateSphere("Hips", rig.Body, new Vector3(0f, 0.995f, -0.005f), new Vector3(0.50f, 0.27f, 0.33f), palette.Skin);
            CreateSphere("Waist", rig.Body, new Vector3(0f, 1.115f, 0f), new Vector3(0.39f, 0.24f, 0.27f), palette.Blouse);

            rig.Skirt = CreateTransform("Skirt Pivot", rig.Body, new Vector3(0f, 1.15f, -0.005f), Quaternion.identity);
            CreateMeshObject("Pleated navy skirt", rig.Skirt, Vector3.zero, Vector3.one, Quaternion.identity, skirtMesh, palette.Navy);
            CreateCube("Waist band", rig.Skirt, new Vector3(0f, -0.012f, 0f), new Vector3(0.57f, 0.045f, 0.41f), palette.Navy);

            rig.Torso = CreateTransform("Torso Pivot", rig.Body, new Vector3(0f, 1.075f, 0f), Quaternion.identity);
            CreateMeshObject("Tailored blouse torso", rig.Torso, new Vector3(0f, 0f, 0f), Vector3.one, Quaternion.identity, torsoMesh, palette.Blouse);
            CreateCylinder("Navy neck trim", rig.Torso, new Vector3(0f, 0.49f, 0.005f), new Vector3(0.22f, 0.035f, 0.18f), palette.Navy);
            CreateCube("Back sailor collar", rig.Torso, new Vector3(0f, 0.385f, -0.174f), new Vector3(0.36f, 0.18f, 0.025f), palette.Navy);
            CreateCube("Left V collar", rig.Torso, new Vector3(-0.07f, 0.385f, 0.185f), new Vector3(0.17f, 0.026f, 0.022f), palette.Navy, Quaternion.Euler(0f, 0f, 37f));
            CreateCube("Right V collar", rig.Torso, new Vector3(0.07f, 0.385f, 0.185f), new Vector3(0.17f, 0.026f, 0.022f), palette.Navy, Quaternion.Euler(0f, 0f, -37f));
            CreateSphere("Left ribbon", rig.Torso, new Vector3(-0.075f, 0.31f, 0.205f), new Vector3(0.15f, 0.085f, 0.045f), palette.Navy, Quaternion.Euler(0f, 0f, 18f));
            CreateSphere("Right ribbon", rig.Torso, new Vector3(0.075f, 0.31f, 0.205f), new Vector3(0.15f, 0.085f, 0.045f), palette.Navy, Quaternion.Euler(0f, 0f, -18f));
            CreateSphere("Ribbon knot", rig.Torso, new Vector3(0f, 0.31f, 0.225f), new Vector3(0.075f, 0.07f, 0.055f), palette.Navy);

            BuildArm(rig.Torso, true, palette, out rig.LeftUpperArm, out rig.LeftForearm);
            BuildArm(rig.Torso, false, palette, out rig.RightUpperArm, out rig.RightForearm);
            BuildLeg(visual, true, palette, out rig.LeftThigh, out rig.LeftCalf, out rig.LeftFoot);
            BuildLeg(visual, false, palette, out rig.RightThigh, out rig.RightCalf, out rig.RightFoot);

            CreateCylinder("Neck", rig.Torso, new Vector3(0f, 0.54f, 0.005f), new Vector3(0.13f, 0.12f, 0.13f), palette.Skin);
            rig.Head = CreateTransform("Head Pivot", rig.Torso, new Vector3(0f, 0.64f, 0f), Quaternion.identity);
            BuildHead(rig.Head, palette, out rig.Hair);
            return rig;
        }

        private static void BuildArm(
            Transform torso,
            bool left,
            Palette palette,
            out Transform upperArm,
            out Transform forearm)
        {
            float side = left ? -1f : 1f;
            string label = left ? "Left" : "Right";
            upperArm = CreateTransform(
                label + " Upper Arm",
                torso,
                new Vector3(side * 0.305f, 0.405f, 0f),
                Quaternion.Euler(0f, 0f, side * 7f));

            CreateCapsule(label + " Sleeve", upperArm, new Vector3(0f, -0.085f, 0f), new Vector3(0.17f, 0.09f, 0.18f), palette.Blouse);
            CreateCylinder(label + " Sleeve Trim", upperArm, new Vector3(0f, -0.17f, 0f), new Vector3(0.15f, 0.022f, 0.16f), palette.Navy);
            CreateCapsule(label + " Upper Arm Skin", upperArm, new Vector3(0f, -0.275f, 0f), new Vector3(0.125f, 0.135f, 0.13f), palette.Skin);

            forearm = CreateTransform(label + " Forearm", upperArm, new Vector3(0f, -0.43f, 0f), Quaternion.identity);
            CreateCapsule(label + " Forearm Skin", forearm, new Vector3(0f, -0.17f, 0.01f), new Vector3(0.115f, 0.18f, 0.12f), palette.Skin);
            CreateSphere(label + " Hand", forearm, new Vector3(0f, -0.36f, 0.018f), new Vector3(0.105f, 0.16f, 0.09f), palette.Skin);
        }

        private static void BuildLeg(
            Transform visual,
            bool left,
            Palette palette,
            out Transform thigh,
            out Transform calf,
            out Transform foot)
        {
            float side = left ? -1f : 1f;
            string label = left ? "Left" : "Right";
            thigh = CreateTransform(label + " Thigh", visual, new Vector3(side * 0.165f, 0.985f, -0.005f), Quaternion.identity);
            CreateCapsule(label + " Thigh Skin", thigh, new Vector3(0f, -0.215f, 0f), new Vector3(0.205f, 0.225f, 0.22f), palette.Skin);

            calf = CreateTransform(label + " Calf", thigh, new Vector3(0f, -0.435f, 0f), Quaternion.identity);
            CreateCapsule(label + " Knee", calf, new Vector3(0f, -0.075f, 0f), new Vector3(0.16f, 0.105f, 0.17f), palette.Skin);
            CreateCapsule(label + " Lower Leg Skin", calf, new Vector3(0f, -0.17f, 0f), new Vector3(0.145f, 0.095f, 0.155f), palette.Skin);
            CreateCapsule(label + " Navy Sock", calf, new Vector3(0f, -0.315f, 0f), new Vector3(0.15f, 0.19f, 0.16f), palette.Navy);
            CreateCylinder(label + " Sock Top", calf, new Vector3(0f, -0.195f, 0f), new Vector3(0.16f, 0.023f, 0.17f), palette.Navy);

            foot = CreateTransform(label + " Foot", calf, new Vector3(0f, -0.47f, 0f), Quaternion.identity);
            CreateSphere(label + " Loafer", foot, new Vector3(0f, -0.042f, 0.075f), new Vector3(0.19f, 0.115f, 0.33f), palette.Shoe);
            CreateCube(label + " Sole", foot, new Vector3(0f, -0.092f, 0.075f), new Vector3(0.195f, 0.035f, 0.335f), palette.Shoe);
            CreateCube(label + " Strap", foot, new Vector3(0f, -0.005f, 0.065f), new Vector3(0.195f, 0.028f, 0.085f), palette.Shoe);
        }

        private static void BuildHead(Transform head, Palette palette, out Transform hairPivot)
        {
            CreateSphere("Hair cap", head, new Vector3(0f, 0.025f, -0.025f), new Vector3(0.39f, 0.43f, 0.36f), palette.Hair);
            CreateSphere("Face", head, new Vector3(0f, 0f, 0.055f), new Vector3(0.335f, 0.37f, 0.315f), palette.Skin);
            CreateSphere("Left ear", head, new Vector3(-0.17f, 0f, 0.015f), new Vector3(0.065f, 0.10f, 0.055f), palette.Skin);
            CreateSphere("Right ear", head, new Vector3(0.17f, 0f, 0.015f), new Vector3(0.065f, 0.10f, 0.055f), palette.Skin);

            CreateSphere("Left eye white", head, new Vector3(-0.073f, 0.035f, 0.207f), new Vector3(0.078f, 0.042f, 0.016f), palette.EyeWhite, Quaternion.Euler(0f, 0f, -7f));
            CreateSphere("Right eye white", head, new Vector3(0.073f, 0.035f, 0.207f), new Vector3(0.078f, 0.042f, 0.016f), palette.EyeWhite, Quaternion.Euler(0f, 0f, 7f));
            CreateSphere("Left pupil", head, new Vector3(-0.068f, 0.035f, 0.217f), new Vector3(0.025f, 0.033f, 0.012f), palette.EyeDark);
            CreateSphere("Right pupil", head, new Vector3(0.068f, 0.035f, 0.217f), new Vector3(0.025f, 0.033f, 0.012f), palette.EyeDark);
            CreateCube("Left eyebrow", head, new Vector3(-0.075f, 0.098f, 0.205f), new Vector3(0.09f, 0.014f, 0.016f), palette.Hair, Quaternion.Euler(0f, 0f, -6f));
            CreateCube("Right eyebrow", head, new Vector3(0.075f, 0.098f, 0.205f), new Vector3(0.09f, 0.014f, 0.016f), palette.Hair, Quaternion.Euler(0f, 0f, 6f));
            CreateSphere("Nose", head, new Vector3(0f, -0.015f, 0.216f), new Vector3(0.032f, 0.046f, 0.025f), palette.Skin);
            CreateCube("Smile", head, new Vector3(0.015f, -0.085f, 0.214f), new Vector3(0.072f, 0.010f, 0.010f), palette.Lip, Quaternion.Euler(0f, 0f, -5f));

            CreateCapsule("Left bang", head, new Vector3(-0.095f, 0.125f, 0.145f), new Vector3(0.065f, 0.12f, 0.045f), palette.Hair, Quaternion.Euler(0f, 0f, -22f));
            CreateCapsule("Center bang", head, new Vector3(-0.018f, 0.145f, 0.17f), new Vector3(0.052f, 0.11f, 0.042f), palette.Hair, Quaternion.Euler(0f, 0f, -8f));
            CreateCapsule("Right bang", head, new Vector3(0.088f, 0.12f, 0.145f), new Vector3(0.06f, 0.12f, 0.045f), palette.Hair, Quaternion.Euler(0f, 0f, 22f));

            hairPivot = CreateTransform("Long Hair Motion", head, new Vector3(0f, 0.11f, -0.145f), Quaternion.identity);
            CreateCapsule("Center long hair", hairPivot, new Vector3(0f, -0.43f, -0.035f), new Vector3(0.31f, 0.47f, 0.18f), palette.Hair);
            CreateCapsule("Left long hair", hairPivot, new Vector3(-0.14f, -0.40f, -0.005f), new Vector3(0.17f, 0.43f, 0.16f), palette.Hair, Quaternion.Euler(0f, 0f, -7f));
            CreateCapsule("Right long hair", hairPivot, new Vector3(0.14f, -0.40f, -0.005f), new Vector3(0.17f, 0.43f, 0.16f), palette.Hair, Quaternion.Euler(0f, 0f, 7f));
            CreateSphere("Hair soft tip", hairPivot, new Vector3(0f, -0.83f, -0.02f), new Vector3(0.31f, 0.18f, 0.17f), palette.Hair);
        }

        private static Mesh CreateStylizedTorsoMesh(int segments)
        {
            float[] heights = { 0f, 0.10f, 0.26f, 0.42f, 0.52f };
            float[] radiiX = { 0.20f, 0.205f, 0.255f, 0.285f, 0.255f };
            float[] radiiZ = { 0.135f, 0.14f, 0.17f, 0.18f, 0.15f };
            float[] centerZ = { 0f, 0f, 0.008f, 0.018f, 0.006f };
            int ringCount = heights.Length;
            List<Vector3> vertices = new List<Vector3>(segments * ringCount + 2);
            List<int> triangles = new List<int>(segments * (ringCount - 1) * 6 + segments * 6);

            for (int ring = 0; ring < ringCount; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = Mathf.PI * 2f * i / segments;
                    vertices.Add(new Vector3(
                        Mathf.Sin(angle) * radiiX[ring],
                        heights[ring],
                        Mathf.Cos(angle) * radiiZ[ring] + centerZ[ring]));
                }
            }

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                int currentRing = ring * segments;
                int nextRing = (ring + 1) * segments;
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    triangles.Add(currentRing + i);
                    triangles.Add(nextRing + next);
                    triangles.Add(nextRing + i);
                    triangles.Add(currentRing + i);
                    triangles.Add(currentRing + next);
                    triangles.Add(nextRing + next);
                }
            }

            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, heights[0], centerZ[0]));
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, heights[ringCount - 1], centerZ[ringCount - 1]));
            int topRingStart = (ringCount - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(bottomCenter);
                triangles.Add(next);
                triangles.Add(i);
                triangles.Add(topCenter);
                triangles.Add(topRingStart + i);
                triangles.Add(topRingStart + next);
            }

            Mesh mesh = new Mesh { name = "Tailored Blouse Torso" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreatePleatedSkirtMesh(
            int segments,
            float topRadiusX,
            float topRadiusZ,
            float bottomRadiusX,
            float bottomRadiusZ,
            float height)
        {
            List<Vector3> vertices = new List<Vector3>(segments * 2 + 2);
            List<int> triangles = new List<int>(segments * 12);
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                float wave = i % 2 == 0 ? 1f : 0.88f;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);
                vertices.Add(new Vector3(sin * topRadiusX, 0f, cos * topRadiusZ));
                vertices.Add(new Vector3(sin * bottomRadiusX * wave, -height, cos * bottomRadiusZ * wave));
            }

            int topCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -height, 0f));

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int top = i * 2;
                int bottom = top + 1;
                int nextTop = next * 2;
                int nextBottom = nextTop + 1;

                triangles.Add(top);
                triangles.Add(nextTop);
                triangles.Add(bottom);
                triangles.Add(nextTop);
                triangles.Add(nextBottom);
                triangles.Add(bottom);

                triangles.Add(topCenter);
                triangles.Add(nextTop);
                triangles.Add(top);
                triangles.Add(bottomCenter);
                triangles.Add(bottom);
                triangles.Add(nextBottom);
            }

            Mesh mesh = new Mesh { name = "Pleated Skirt" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOrUpdateMesh(string path, Mesh generated)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void OptimizeRigGeometry(Rig rig)
        {
            CombineDirectGeometry(rig.Body);
            CombineDirectGeometry(rig.Torso);
            CombineDirectGeometry(rig.Skirt);
            CombineDirectGeometry(rig.Head);
            CombineDirectGeometry(rig.Hair);
            CombineDirectGeometry(rig.LeftUpperArm);
            CombineDirectGeometry(rig.RightUpperArm);
            CombineDirectGeometry(rig.LeftForearm);
            CombineDirectGeometry(rig.RightForearm);
            CombineDirectGeometry(rig.LeftThigh);
            CombineDirectGeometry(rig.RightThigh);
            CombineDirectGeometry(rig.LeftCalf);
            CombineDirectGeometry(rig.RightCalf);
            CombineDirectGeometry(rig.LeftFoot);
            CombineDirectGeometry(rig.RightFoot);
        }

        private static void CombineDirectGeometry(Transform pivot)
        {
            if (pivot == null)
            {
                return;
            }

            Dictionary<Material, List<MeshFilter>> groups = new Dictionary<Material, List<MeshFilter>>();
            for (int i = 0; i < pivot.childCount; i++)
            {
                Transform child = pivot.GetChild(i);
                MeshFilter filter = child.GetComponent<MeshFilter>();
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (filter == null || filter.sharedMesh == null || renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                List<MeshFilter> filters;
                if (!groups.TryGetValue(renderer.sharedMaterial, out filters))
                {
                    filters = new List<MeshFilter>();
                    groups.Add(renderer.sharedMaterial, filters);
                }

                filters.Add(filter);
            }

            foreach (KeyValuePair<Material, List<MeshFilter>> group in groups)
            {
                if (group.Value.Count < 2)
                {
                    continue;
                }

                CombineInstance[] instances = new CombineInstance[group.Value.Count];
                for (int i = 0; i < group.Value.Count; i++)
                {
                    MeshFilter filter = group.Value[i];
                    instances[i] = new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        transform = pivot.worldToLocalMatrix * filter.transform.localToWorldMatrix
                    };
                }

                string combinedName = $"{pivot.name} - {group.Key.name}";
                Mesh combined = new Mesh
                {
                    name = combinedName,
                    indexFormat = IndexFormat.UInt32
                };
                combined.CombineMeshes(instances, true, true, false);
                combined.RecalculateBounds();
                Mesh persisted = CreateOrUpdateMesh(
                    $"{MeshFolder}/{SafeAssetName(combinedName)}.asset",
                    combined);

                CreateMeshObject(
                    combinedName,
                    pivot,
                    Vector3.zero,
                    Vector3.one,
                    Quaternion.identity,
                    persisted,
                    group.Key);

                for (int i = 0; i < group.Value.Count; i++)
                {
                    UnityEngine.Object.DestroyImmediate(group.Value[i].gameObject);
                }
            }
        }

        private static string SafeAssetName(string value)
        {
            char[] characters = value.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                char character = characters[i];
                if (!char.IsLetterOrDigit(character) && character != ' ' && character != '-' && character != '_')
                {
                    characters[i] = '_';
                }
            }

            return new string(characters);
        }

        private static Transform CreateTransform(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = rotation;
            return gameObject.transform;
        }

        private static GameObject CreateSphere(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion? rotation = null)
        {
            return CreatePrimitive(name, PrimitiveType.Sphere, parent, position, scale, material, rotation ?? Quaternion.identity);
        }

        private static GameObject CreateCapsule(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion? rotation = null)
        {
            return CreatePrimitive(name, PrimitiveType.Capsule, parent, position, scale, material, rotation ?? Quaternion.identity);
        }

        private static GameObject CreateCylinder(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion? rotation = null)
        {
            return CreatePrimitive(name, PrimitiveType.Cylinder, parent, position, scale, material, rotation ?? Quaternion.identity);
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion? rotation = null)
        {
            return CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, material, rotation ?? Quaternion.identity);
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = rotation;
            gameObject.transform.localScale = scale;
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            ConfigureRenderer(gameObject.GetComponent<Renderer>(), material);
            return gameObject;
        }

        private static GameObject CreateMeshObject(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Mesh mesh,
            Material material)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = scale;
            gameObject.transform.localRotation = rotation;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            ConfigureRenderer(renderer, material);
            return gameObject;
        }

        private static void ConfigureRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void ValidatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            CartoonTargetAnimator animator = prefab == null ? null : prefab.GetComponent<CartoonTargetAnimator>();
            Renderer[] renderers = prefab == null ? Array.Empty<Renderer>() : prefab.GetComponentsInChildren<Renderer>(true);
            if (prefab == null || animator == null || !animator.IsConfigured ||
                renderers.Length < 20 || renderers.Length > 40)
            {
                throw new InvalidOperationException("Cartoon TargetWoman3D Prefab 핵심 구성 검증에 실패했습니다.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 0; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
                Material[] materials = renderers[i].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null || material.shader == null || material.shader.name != "Konseki/CartoonTarget")
                    {
                        throw new InvalidOperationException($"Cartoon Target 재질 검증에 실패했습니다: {renderers[i].name}");
                    }
                }
            }

            if (bounds.size.y < 1.65f || bounds.size.y > 2.05f || bounds.size.y < bounds.size.x * 1.5f)
            {
                throw new InvalidOperationException($"Cartoon Target 비율이 비정상입니다: bounds={bounds.size}");
            }

            ValidateWalkPose(prefab);
            Debug.Log($"[Konseki] Cartoon 3D 타깃 검증 완료: Renderer {renderers.Length}개, " +
                      $"bounds={bounds.size.x:0.00}x{bounds.size.y:0.00}x{bounds.size.z:0.00}m");
        }

        private static void ValidateWalkPose(GameObject prefab)
        {
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Cartoon Target 보행 검증 인스턴스 생성에 실패했습니다.");
                }

                CartoonTargetAnimator animator = instance.GetComponent<CartoonTargetAnimator>();
                Transform leftKnee = FindRequiredTransform(instance.transform, "LeftLowerLeg");
                Transform rightKnee = FindRequiredTransform(instance.transform, "RightLowerLeg");
                animator.PreviewPose(0.25f, 1f);
                Vector3 leftForward = leftKnee.position;
                Vector3 rightBack = rightKnee.position;
                animator.PreviewPose(0.75f, 1f);
                Vector3 leftBack = leftKnee.position;
                Vector3 rightForward = rightKnee.position;

                float kneeTravel = Vector3.Distance(leftForward, leftBack) +
                                   Vector3.Distance(rightBack, rightForward);
                if (kneeTravel < 0.20f)
                {
                    throw new InvalidOperationException($"Cartoon Target 보행 관절이 움직이지 않습니다: travel={kneeTravel:0.000}m");
                }

                Debug.Log($"[Konseki] Cartoon 보행 검증 완료: 양쪽 무릎 이동 합계 {kneeTravel:0.00}m");
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        private static void CapturePreview(string frontPath, string backPath, string sidePath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("미리보기용 Cartoon TargetWoman3D Prefab을 찾지 못했습니다.");
            }

            GameObject character = null;
            GameObject cameraObject = null;
            GameObject keyLightObject = null;
            GameObject fillLightObject = null;
            GameObject ground = null;
            Material groundMaterial = null;
            RenderTexture renderTexture = null;
            Texture2D screenshot = null;

            try
            {
                character = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (character == null)
                {
                    throw new InvalidOperationException("Cartoon Target 미리보기 인스턴스 생성에 실패했습니다.");
                }

                character.hideFlags = HideFlags.HideAndDontSave;
                character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                CartoonTargetAnimator animator = character.GetComponent<CartoonTargetAnimator>();
                animator.PreviewPose(0.25f, 1f);

                cameraObject = new GameObject("Cartoon target preview camera") { hideFlags = HideFlags.HideAndDontSave };
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f);
                camera.fieldOfView = 28f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 30f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                keyLightObject = new GameObject("Cartoon target key light") { hideFlags = HideFlags.HideAndDontSave };
                Light keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = Color.white;
                keyLight.intensity = 0.85f;
                keyLightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

                fillLightObject = new GameObject("Cartoon target fill light") { hideFlags = HideFlags.HideAndDontSave };
                Light fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = Color.white;
                fillLight.intensity = 0.28f;
                fillLightObject.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Cartoon target preview ground";
                ground.hideFlags = HideFlags.HideAndDontSave;
                ground.transform.position = new Vector3(0f, -0.13f, 0f);
                ground.transform.localScale = new Vector3(7f, 0.08f, 7f);
                Shader standard = Shader.Find("Standard");
                groundMaterial = new Material(standard) { color = new Color(0.09f, 0.11f, 0.16f) };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                renderTexture = new RenderTexture(720, 900, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 2,
                    hideFlags = HideFlags.HideAndDontSave
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                screenshot = new Texture2D(720, 900, TextureFormat.RGB24, false);

                RenderPreview(camera, renderTexture, screenshot, new Vector3(2.5f, 1.25f, 4.2f), frontPath);
                RenderPreview(camera, renderTexture, screenshot, new Vector3(-2.5f, 1.25f, -4.2f), backPath);
                RenderPreview(camera, renderTexture, screenshot, new Vector3(4.2f, 1.25f, -0.7f), sidePath);
                Debug.Log($"[Konseki] CARTOON_TARGET_PREVIEW_READY front={frontPath}, back={backPath}, side={sidePath}");
            }
            finally
            {
                RenderTexture.active = null;
                if (screenshot != null) UnityEngine.Object.DestroyImmediate(screenshot);
                if (renderTexture != null) UnityEngine.Object.DestroyImmediate(renderTexture);
                if (groundMaterial != null) UnityEngine.Object.DestroyImmediate(groundMaterial);
                if (ground != null) UnityEngine.Object.DestroyImmediate(ground);
                if (fillLightObject != null) UnityEngine.Object.DestroyImmediate(fillLightObject);
                if (keyLightObject != null) UnityEngine.Object.DestroyImmediate(keyLightObject);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (character != null) UnityEngine.Object.DestroyImmediate(character);
            }
        }

        private static void RenderPreview(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D screenshot,
            Vector3 cameraPosition,
            string path)
        {
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(new Vector3(0f, 0.93f, 0f));
            camera.Render();
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0, false);
            screenshot.Apply(false, false);
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            if (separator <= 0)
            {
                throw new InvalidOperationException($"Unity 자산 폴더 경로가 잘못되었습니다: {path}");
            }

            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
