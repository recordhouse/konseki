using UnityEngine;

namespace Konseki
{
    public sealed class TargetWalker : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.15f;
        [SerializeField, Min(1f)] private float turnSpeed = 135f;
        [SerializeField, Min(0.05f)] private float waypointTolerance = 0.18f;

        [SerializeField] private Vector3[] route;
        private int waypointIndex;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        private bool movementEnabled = true;

        public bool ReachedDestination { get; private set; }
        public float RouteProgress => route == null || route.Length < 2
            ? 0f
            : Mathf.Clamp01((float)waypointIndex / (route.Length - 1));

        public void Configure(
            Vector3[] points,
            Transform leftArmTransform,
            Transform rightArmTransform,
            Transform leftLegTransform,
            Transform rightLegTransform)
        {
            route = points;
            leftArm = leftArmTransform;
            rightArm = rightArmTransform;
            leftLeg = leftLegTransform;
            rightLeg = rightLegTransform;
            waypointIndex = points != null && points.Length > 1 ? 1 : 0;
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
        }

        private void Update()
        {
            if (!movementEnabled || ReachedDestination || route == null || route.Length == 0)
            {
                AnimateLimbs(false);
                return;
            }

            Vector3 target = route[waypointIndex];
            Vector3 flatDelta = target - transform.position;
            flatDelta.y = 0f;

            if (flatDelta.magnitude <= waypointTolerance)
            {
                if (waypointIndex >= route.Length - 1)
                {
                    ReachedDestination = true;
                    AnimateLimbs(false);
                    return;
                }

                waypointIndex++;
                target = route[waypointIndex];
                flatDelta = target - transform.position;
                flatDelta.y = 0f;
            }

            if (flatDelta.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
                AnimateLimbs(true);
            }
        }

        private void AnimateLimbs(bool walking)
        {
            float swing = walking ? Mathf.Sin(Time.time * 7f) * 24f : 0f;
            SetLimbRotation(leftArm, swing);
            SetLimbRotation(rightArm, -swing);
            SetLimbRotation(leftLeg, -swing);
            SetLimbRotation(rightLeg, swing);
        }

        private static void SetLimbRotation(Transform limb, float xAngle)
        {
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
            }
        }
    }
}
