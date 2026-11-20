using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konseki.EditorTools
{
    public static class VrmTargetCharacterBuilder
    {
        private const string VrmSourcePath = "Assets/Konseki/ArtReferences/TargetWoman/Konseki.vrm";
        private const string PrefabPath = "Assets/Konseki/Resources/Characters/TargetWoman3D.prefab";

        [MenuItem("Konseki/Characters/Build Target From Konseki VRM")]
        public static void BuildFromMenu()
        {
            Build(true);
        }

        public static void SetupFromCommandLine()
        {
            Build(false);
        }

        private static void Build(bool fromMenu)
        {
            PrepareVrmImporter();

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(VrmSourcePath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Konseki.vrm을 GameObject로 불러오지 못했습니다. UniVRM 설치와 파일 경로를 확인하세요: {VrmSourcePath}");
            }

            EnsureFolder("Assets/Konseki/Resources/Characters");
            GameObject root = new GameObject("TargetWoman3D");
            try
            {
                Transform visual = CreateTransform("Konseki VRM Visual", root.transform);
                Transform bodyMotion = CreateTransform("Body Motion", visual);
                GameObject model = PrefabUtility.InstantiatePrefab(source, bodyMotion) as GameObject;
                if (model == null)
                {
                    throw new InvalidOperationException("Konseki.vrm 인스턴스 생성에 실패했습니다.");
                }

                model.name = "Konseki VRM";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                Animator humanoid = model.GetComponentInChildren<Animator>(true);
                ValidateHumanoid(humanoid);
                ConfigureAnimator(humanoid);
                ConfigureRenderers(model);
                AlignFeetToGround(model);

                Transform chest = BoneOrFallback(humanoid, HumanBodyBones.Chest, HumanBodyBones.Spine);
                Transform head = RequireBone(humanoid, HumanBodyBones.Head);
                Transform leftUpperArm = RequireBone(humanoid, HumanBodyBones.LeftUpperArm);
                Transform rightUpperArm = RequireBone(humanoid, HumanBodyBones.RightUpperArm);
                Transform leftLowerArm = RequireBone(humanoid, HumanBodyBones.LeftLowerArm);
                Transform rightLowerArm = RequireBone(humanoid, HumanBodyBones.RightLowerArm);
                Transform leftUpperLeg = RequireBone(humanoid, HumanBodyBones.LeftUpperLeg);
                Transform rightUpperLeg = RequireBone(humanoid, HumanBodyBones.RightUpperLeg);
                Transform leftLowerLeg = RequireBone(humanoid, HumanBodyBones.LeftLowerLeg);
                Transform rightLowerLeg = RequireBone(humanoid, HumanBodyBones.RightLowerLeg);
                Transform leftFoot = RequireBone(humanoid, HumanBodyBones.LeftFoot);
                Transform rightFoot = RequireBone(humanoid, HumanBodyBones.RightFoot);

                CartoonTargetAnimator walkAnimator = root.AddComponent<CartoonTargetAnimator>();
                walkAnimator.Configure(
                    root.transform,
                    bodyMotion,
                    chest,
                    null,
                    head,
                    null,
                    leftUpperArm,
                    rightUpperArm,
                    leftLowerArm,
                    rightLowerArm,
                    leftUpperLeg,
                    rightUpperLeg,
                    leftLowerLeg,
                    rightLowerLeg,
                    leftFoot,
                    rightFoot);
                walkAnimator.SetHumanoidTPoseCorrection(true);

                bool saved;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out saved);
                if (!saved)
                {
                    throw new InvalidOperationException("Konseki VRM 타겟 Prefab 저장에 실패했습니다.");
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
                ? "[Konseki] Konseki.vrm을 미행 타겟으로 교체했습니다."
                : "[Konseki] VRM_TARGET_CHARACTER_READY");
        }

        private static void PrepareVrmImporter()
        {
            AssetDatabase.ImportAsset(VrmSourcePath, ImportAssetOptions.ForceSynchronousImport);
            AssetImporter importer = AssetImporter.GetAtPath(VrmSourcePath);
            if (importer == null || importer.GetType().FullName != "UniVRM10.VrmScriptedImporter")
            {
                throw new InvalidOperationException(
                    $"Konseki.vrm 전용 UniVRM 임포터를 찾지 못했습니다: {importer?.GetType().FullName ?? "null"}");
            }

            SerializedObject importerData = new SerializedObject(importer);
            SerializedProperty renderPipeline = importerData.FindProperty("RenderPipeline");
            if (renderPipeline == null)
            {
                throw new InvalidOperationException("UniVRM RenderPipeline 임포트 설정을 찾지 못했습니다.");
            }

            const int builtInRenderPipeline = 1;
            if (renderPipeline.intValue != builtInRenderPipeline)
            {
                renderPipeline.intValue = builtInRenderPipeline;
                importerData.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }
            else
            {
                AssetDatabase.ImportAsset(
                    VrmSourcePath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }

        private static void ValidateHumanoid(Animator animator)
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            {
                throw new InvalidOperationException("Konseki.vrm에 유효한 Humanoid Avatar가 없습니다.");
            }
        }

        private static void ConfigureAnimator(Animator animator)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.runtimeAnimatorController = null;
        }

        private static void ConfigureRenderers(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Konseki.vrm에 Renderer가 없습니다.");
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = false;
                    skinned.skinnedMotionVectors = false;
                }
            }
        }

        private static void AlignFeetToGround(GameObject model)
        {
            Bounds bounds = CalculateBounds(model);
            model.transform.position += Vector3.up * -bounds.min.y;
            bounds = CalculateBounds(model);
            if (bounds.size.y < 1.2f || bounds.size.y > 2.3f)
            {
                throw new InvalidOperationException($"Konseki.vrm 캐릭터 높이가 비정상입니다: {bounds.size.y:0.00}m");
            }
        }

        private static void ValidatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("저장된 Konseki VRM 타겟 Prefab을 찾지 못했습니다.");
            }

            CartoonTargetAnimator walkAnimator = prefab.GetComponent<CartoonTargetAnimator>();
            Animator humanoid = prefab.GetComponentInChildren<Animator>(true);
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (walkAnimator == null || !walkAnimator.IsConfigured || renderers.Length == 0)
            {
                throw new InvalidOperationException("Konseki VRM 타겟 Prefab 핵심 구성 검증에 실패했습니다.");
            }

            ValidateHumanoid(humanoid);
            bool hasSkinnedMesh = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                hasSkinnedMesh |= renderers[i] is SkinnedMeshRenderer;
                Material[] materials = renderers[i].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] == null || !EditorUtility.IsPersistent(materials[materialIndex]))
                    {
                        throw new InvalidOperationException($"Konseki VRM 재질 참조가 올바르지 않습니다: {renderers[i].name}");
                    }
                }
            }

            if (!hasSkinnedMesh)
            {
                throw new InvalidOperationException("Konseki VRM 타겟에 SkinnedMeshRenderer가 없습니다.");
            }

            Bounds bounds = CalculateBounds(prefab);
            ValidateWalkPose(prefab);
            Debug.Log($"[Konseki] Konseki VRM 검증 완료: Renderer {renderers.Length}개, " +
                      $"bounds={bounds.size.x:0.00}x{bounds.size.y:0.00}x{bounds.size.z:0.00}m");
        }

        private static void ValidateWalkPose(GameObject prefab)
        {
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                CartoonTargetAnimator walkAnimator = instance == null ? null : instance.GetComponent<CartoonTargetAnimator>();
                Animator humanoid = instance == null ? null : instance.GetComponentInChildren<Animator>(true);
                ValidateHumanoid(humanoid);

                Transform leftKnee = RequireBone(humanoid, HumanBodyBones.LeftLowerLeg);
                Transform rightKnee = RequireBone(humanoid, HumanBodyBones.RightLowerLeg);
                walkAnimator.PreviewPose(0.25f, 1f);
                Vector3 leftForward = leftKnee.position;
                Vector3 rightBack = rightKnee.position;
                walkAnimator.PreviewPose(0.75f, 1f);
                float travel = Vector3.Distance(leftForward, leftKnee.position) +
                               Vector3.Distance(rightBack, rightKnee.position);
                if (travel < 0.12f)
                {
                    throw new InvalidOperationException($"Konseki VRM 보행 관절이 움직이지 않습니다: travel={travel:0.000}m");
                }

                Debug.Log($"[Konseki] Konseki VRM 보행 검증 완료: 양쪽 무릎 이동 합계 {travel:0.00}m");
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        private static Transform BoneOrFallback(Animator animator, HumanBodyBones primary, HumanBodyBones fallback)
        {
            return animator.GetBoneTransform(primary) ?? RequireBone(animator, fallback);
        }

        private static Transform RequireBone(Animator animator, HumanBodyBones bone)
        {
            Transform result = animator == null ? null : animator.GetBoneTransform(bone);
            if (result == null)
            {
                throw new InvalidOperationException($"Konseki.vrm Humanoid 본을 찾지 못했습니다: {bone}");
            }

            return result;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Transform CreateTransform(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
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
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
