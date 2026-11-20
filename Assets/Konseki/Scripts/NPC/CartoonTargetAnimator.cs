using UnityEngine;

namespace Konseki
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CartoonTargetAnimator : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Transform movementRoot;
        [SerializeField, Range(0.1f, 2f)] private float strideReferenceSpeed = 1.15f;
        [SerializeField, Range(2f, 9f)] private float cycleSpeed = 4.4f;
        [SerializeField, Range(8f, 45f)] private float legSwingDegrees = 18f;
        [SerializeField, Range(4f, 35f)] private float armSwingDegrees = 12f;

        [Header("Stylized Rig")]
        [SerializeField] private bool lowerHumanoidTPoseArms;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform torsoPivot;
        [SerializeField] private Transform skirtPivot;
        [SerializeField] private Transform headPivot;
        [SerializeField] private Transform hairPivot;
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform leftThigh;
        [SerializeField] private Transform rightThigh;
        [SerializeField] private Transform leftCalf;
        [SerializeField] private Transform rightCalf;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;

        private Vector3 lastPosition;
        private Vector3 bodyRestPosition;
        private Quaternion torsoRestRotation;
        private Quaternion skirtRestRotation;
        private Quaternion headRestRotation;
        private Quaternion hairRestRotation;
        private Quaternion leftUpperArmRestRotation;
        private Quaternion rightUpperArmRestRotation;
        private Quaternion leftForearmRestRotation;
        private Quaternion rightForearmRestRotation;
        private Quaternion leftThighRestRotation;
        private Quaternion rightThighRestRotation;
        private Quaternion leftCalfRestRotation;
        private Quaternion rightCalfRestRotation;
        private Quaternion leftFootRestRotation;
        private Quaternion rightFootRestRotation;
        private float phase;
        private float movementBlend;
        private float movementBlendVelocity;
        private bool restPoseCached;

        public bool IsConfigured => bodyRoot != null && leftThigh != null && rightThigh != null;

        public void Configure(
            Transform root,
            Transform body,
            Transform torso,
            Transform skirt,
            Transform head,
            Transform hair,
            Transform leftArm,
            Transform rightArm,
            Transform leftLowerArm,
            Transform rightLowerArm,
            Transform leftUpperLeg,
            Transform rightUpperLeg,
            Transform leftLowerLeg,
            Transform rightLowerLeg,
            Transform leftShoe,
            Transform rightShoe)
        {
            movementRoot = root;
            bodyRoot = body;
            torsoPivot = torso;
            skirtPivot = skirt;
            headPivot = head;
            hairPivot = hair;
            leftUpperArm = leftArm;
            rightUpperArm = rightArm;
            leftForearm = leftLowerArm;
            rightForearm = rightLowerArm;
            leftThigh = leftUpperLeg;
            rightThigh = rightUpperLeg;
            leftCalf = leftLowerLeg;
            rightCalf = rightLowerLeg;
            leftFoot = leftShoe;
            rightFoot = rightShoe;
            CacheRestPose();
        }

        public void SetMovementRoot(Transform root)
        {
            movementRoot = root;
            lastPosition = RootPosition();
        }

        public void SetHumanoidTPoseCorrection(bool enabled)
        {
            lowerHumanoidTPoseArms = enabled;
        }

        public void PreviewPose(float normalizedPhase, float walkBlend)
        {
            if (!restPoseCached)
            {
                CacheRestPose();
            }

            phase = normalizedPhase * Mathf.PI * 2f;
            movementBlend = Mathf.Clamp01(walkBlend);
            ApplyPose(0f);
        }

        private void Awake()
        {
            CacheRestPose();
        }

        private void OnEnable()
        {
            lastPosition = RootPosition();
        }

        private void LateUpdate()
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 position = RootPosition();
            Vector3 delta = position - lastPosition;
            delta.y = 0f;
            float speed = delta.magnitude / deltaTime;
            float targetBlend = Mathf.InverseLerp(0.025f, strideReferenceSpeed * 0.45f, speed);
            movementBlend = Mathf.SmoothDamp(
                movementBlend,
                targetBlend,
                ref movementBlendVelocity,
                targetBlend > movementBlend ? 0.12f : 0.22f,
                Mathf.Infinity,
                deltaTime);

            phase += deltaTime * Mathf.Lerp(1.15f, cycleSpeed, movementBlend);
            ApplyPose(Time.time);
            lastPosition = position;
        }

        private void CacheRestPose()
        {
            if (bodyRoot != null) bodyRestPosition = bodyRoot.localPosition;
            torsoRestRotation = LocalRotation(torsoPivot);
            skirtRestRotation = LocalRotation(skirtPivot);
            headRestRotation = LocalRotation(headPivot);
            hairRestRotation = LocalRotation(hairPivot);
            leftUpperArmRestRotation = LocalRotation(leftUpperArm);
            rightUpperArmRestRotation = LocalRotation(rightUpperArm);
            leftForearmRestRotation = LocalRotation(leftForearm);
            rightForearmRestRotation = LocalRotation(rightForearm);
            leftThighRestRotation = LocalRotation(leftThigh);
            rightThighRestRotation = LocalRotation(rightThigh);
            leftCalfRestRotation = LocalRotation(leftCalf);
            rightCalfRestRotation = LocalRotation(rightCalf);
            leftFootRestRotation = LocalRotation(leftFoot);
            rightFootRestRotation = LocalRotation(rightFoot);
            lastPosition = RootPosition();
            restPoseCached = true;
        }

        private void ApplyPose(float absoluteTime)
        {
            float stride = Mathf.Sin(phase);
            float oppositeStride = Mathf.Sin(phase + Mathf.PI);
            float doubleStep = Mathf.Abs(Mathf.Sin(phase));
            float idleBreath = Mathf.Sin(absoluteTime * 1.55f);
            float walk = movementBlend;
            float idle = 1f - walk;

            if (bodyRoot != null)
            {
                bodyRoot.localPosition = bodyRestPosition + new Vector3(
                    0f,
                    doubleStep * 0.014f * walk + idleBreath * 0.004f * idle,
                    0f);
            }

            SetRotation(torsoPivot, torsoRestRotation, new Vector3(1.8f * walk, 0f, -stride * 2.2f * walk));
            SetRotation(skirtPivot, skirtRestRotation, new Vector3(-doubleStep * 1.5f * walk, stride * 1.8f * walk, -stride * 1.2f * walk));

            float glance = -7f + Mathf.Sin(absoluteTime * 0.48f) * 5f;
            SetRotation(headPivot, headRestRotation, new Vector3(idleBreath * 1.2f, glance, -stride * 1.6f * walk));
            SetRotation(hairPivot, hairRestRotation, new Vector3(-stride * 4.2f * walk, -glance * 0.32f, stride * 2.8f * walk));

            float leftLeg = stride * legSwingDegrees * walk;
            float rightLeg = oppositeStride * legSwingDegrees * walk;
            SetRotation(leftThigh, leftThighRestRotation, new Vector3(leftLeg, 0f, 0f));
            SetRotation(rightThigh, rightThighRestRotation, new Vector3(rightLeg, 0f, 0f));

            float leftKnee = Mathf.Max(0f, -stride) * 22f * walk;
            float rightKnee = Mathf.Max(0f, -oppositeStride) * 22f * walk;
            SetRotation(leftCalf, leftCalfRestRotation, new Vector3(leftKnee, 0f, 0f));
            SetRotation(rightCalf, rightCalfRestRotation, new Vector3(rightKnee, 0f, 0f));
            SetRotation(leftFoot, leftFootRestRotation, new Vector3(-leftKnee * 0.45f - leftLeg * 0.18f, 0f, 0f));
            SetRotation(rightFoot, rightFootRestRotation, new Vector3(-rightKnee * 0.45f - rightLeg * 0.18f, 0f, 0f));

            float leftArm = oppositeStride * armSwingDegrees * walk;
            float rightArm = stride * armSwingDegrees * walk;
            if (lowerHumanoidTPoseArms)
            {
                SetHumanoidArmRotation(leftUpperArm, leftUpperArmRestRotation, leftArm, 72f);
                SetHumanoidArmRotation(rightUpperArm, rightUpperArmRestRotation, rightArm, -72f);
            }
            else
            {
                SetRotation(leftUpperArm, leftUpperArmRestRotation, new Vector3(leftArm, 0f, 0f));
                SetRotation(rightUpperArm, rightUpperArmRestRotation, new Vector3(rightArm, 0f, 0f));
            }
            SetRotation(leftForearm, leftForearmRestRotation, new Vector3(Mathf.Max(0f, stride) * 8f * walk, 0f, 0f));
            SetRotation(rightForearm, rightForearmRestRotation, new Vector3(Mathf.Max(0f, oppositeStride) * 8f * walk, 0f, 0f));
        }

        private void SetHumanoidArmRotation(Transform target, Quaternion rest, float swing, float downAngle)
        {
            if (target == null)
            {
                return;
            }

            Quaternion restWorld = target.parent == null ? rest : target.parent.rotation * rest;
            Transform axes = movementRoot == null ? transform : movementRoot;
            target.rotation =
                Quaternion.AngleAxis(swing, axes.right) *
                Quaternion.AngleAxis(downAngle, axes.forward) *
                restWorld;
        }

        private Vector3 RootPosition()
        {
            return movementRoot == null ? transform.position : movementRoot.position;
        }

        private static Quaternion LocalRotation(Transform target)
        {
            return target == null ? Quaternion.identity : target.localRotation;
        }

        private static void SetRotation(Transform target, Quaternion rest, Vector3 eulerOffset)
        {
            if (target != null)
            {
                target.localRotation = rest * Quaternion.Euler(eulerOffset);
            }
        }
    }
}
