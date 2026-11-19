using UnityEngine;
using UnityEngine.SceneManagement;

namespace Konseki
{
    public sealed class TailGameManager : MonoBehaviour
    {
        [Header("Tail distances")]
        [SerializeField] private float tooCloseDistance = 4.5f;
        [SerializeField] private float idealMaxDistance = 16f;
        [SerializeField] private float lostDistance = 25f;
        [SerializeField] private float lostGraceSeconds = 5f;

        [Header("Detection")]
        [SerializeField] private float targetViewAngle = 62f;
        [SerializeField] private float suspicionGainPerSecond = 24f;
        [SerializeField] private float suspicionRecoveryPerSecond = 8f;

        private FirstPersonController player;
        private TargetWalker target;
        private float lostTimer;

        public GamePhase Phase { get; private set; } = GamePhase.Playing;
        public float DistanceToTarget { get; private set; }
        public float Suspicion { get; private set; }
        public float LostRatio => Mathf.Clamp01(lostTimer / lostGraceSeconds);
        public string StatusMessage { get; private set; } = "타깃을 놓치지 말고 따라가세요";

        public void Configure(FirstPersonController playerController, TargetWalker targetWalker)
        {
            player = playerController;
            target = targetWalker;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

            if (Phase != GamePhase.Playing || player == null || target == null)
            {
                return;
            }

            EvaluateTail();
        }

        private void EvaluateTail()
        {
            Vector3 targetToPlayer = player.transform.position - target.transform.position;
            targetToPlayer.y = 0f;
            DistanceToTarget = targetToPlayer.magnitude;

            float viewAngle = targetToPlayer.sqrMagnitude < 0.01f
                ? 0f
                : Vector3.Angle(target.transform.forward, targetToPlayer.normalized);
            bool inTargetView = viewAngle <= targetViewAngle * 0.5f;
            bool dangerouslyClose = DistanceToTarget < tooCloseDistance;

            if (dangerouslyClose || (inTargetView && DistanceToTarget < idealMaxDistance))
            {
                float proximityMultiplier = dangerouslyClose ? 1.35f : 0.75f;
                Suspicion += suspicionGainPerSecond * proximityMultiplier * Time.deltaTime;
            }
            else
            {
                Suspicion -= suspicionRecoveryPerSecond * Time.deltaTime;
            }

            Suspicion = Mathf.Clamp(Suspicion, 0f, 100f);

            if (DistanceToTarget > lostDistance)
            {
                lostTimer += Time.deltaTime;
            }
            else
            {
                lostTimer = Mathf.Max(0f, lostTimer - Time.deltaTime * 1.5f);
            }

            if (Suspicion >= 100f)
            {
                EndGame(false, "정체를 들켰습니다");
                return;
            }

            if (lostTimer >= lostGraceSeconds)
            {
                EndGame(false, "타깃을 놓쳤습니다");
                return;
            }

            if (target.ReachedDestination)
            {
                bool closeEnough = DistanceToTarget <= idealMaxDistance && DistanceToTarget >= tooCloseDistance;
                EndGame(closeEnough, closeEnough ? "목적지까지 미행에 성공했습니다" : "마지막 순간에 거리를 놓쳤습니다");
                return;
            }

            if (dangerouslyClose)
            {
                StatusMessage = "너무 가깝습니다 — 천천히 거리를 벌리세요";
            }
            else if (inTargetView && DistanceToTarget < idealMaxDistance)
            {
                StatusMessage = "타깃의 시야입니다 — 뒤로 돌아가세요";
            }
            else if (DistanceToTarget > lostDistance)
            {
                StatusMessage = "타깃을 놓치고 있습니다 — 조금 가까이 가세요";
            }
            else if (DistanceToTarget > idealMaxDistance)
            {
                StatusMessage = "거리가 멉니다 — 조심스럽게 따라붙으세요";
            }
            else
            {
                StatusMessage = "안전 거리를 유지하고 있습니다";
            }
        }

        private void EndGame(bool success, string message)
        {
            Phase = success ? GamePhase.Success : GamePhase.Failed;
            StatusMessage = message;
            player.InputEnabled = false;
            target.SetMovementEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
