using UnityEngine;

namespace Konseki
{
    public sealed class TailHud : MonoBehaviour
    {
        private TailGameManager gameManager;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centeredStyle;
        private Texture2D whiteTexture;
        private float openedAt;

        private void Awake()
        {
            openedAt = Time.unscaledTime;
        }

        public void Configure(TailGameManager manager)
        {
            gameManager = manager;
        }

        private void OnGUI()
        {
            if (gameManager == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyles();
            DrawObjectivePanel();
            DrawSuspicionPanel();
            DrawCrosshair();
            DrawControls();
            DrawVariantTitle();

            if (gameManager.Phase != GamePhase.Playing)
            {
                DrawResult();
            }
        }

        private void DrawObjectivePanel()
        {
            Rect panel = new Rect(24f, 24f, 410f, 104f);
            DrawRect(panel, PanelColor());
            DrawRect(new Rect(panel.x, panel.y, 5f, panel.height), AccentColor());
            GUI.Label(new Rect(40f, 34f, 380f, 28f), "미행", titleStyle);
            GUI.Label(new Rect(40f, 65f, 380f, 24f), gameManager.StatusMessage, bodyStyle);
            GUI.Label(new Rect(40f, 93f, 380f, 22f), $"거리  {gameManager.DistanceToTarget:0.0} m", bodyStyle);
        }

        private void DrawSuspicionPanel()
        {
            float width = Mathf.Min(330f, Screen.width - 48f);
            Rect panel = new Rect(Screen.width - width - 24f, 24f, width, 86f);
            DrawRect(panel, PanelColor());
            DrawRect(new Rect(panel.x, panel.y, panel.width, 4f), AccentColor());
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, width - 32f, 24f), "의심도", bodyStyle);

            Rect background = new Rect(panel.x + 16f, panel.y + 42f, width - 32f, 16f);
            DrawRect(background, new Color(0.09f, 0.1f, 0.14f, 1f));
            float normalized = gameManager.Suspicion / 100f;
            Color fill = Color.Lerp(new Color(0.25f, 0.75f, 0.85f), new Color(0.95f, 0.18f, 0.12f), normalized);
            DrawRect(new Rect(background.x, background.y, background.width * normalized, background.height), fill);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 61f, width - 32f, 20f), $"{gameManager.Suspicion:0}%", bodyStyle);
        }

        private void DrawCrosshair()
        {
            float x = Screen.width * 0.5f;
            float y = Screen.height * 0.5f;
            DrawRect(new Rect(x - 6f, y, 12f, 1f), new Color(1f, 1f, 1f, 0.7f));
            DrawRect(new Rect(x, y - 6f, 1f, 12f), new Color(1f, 1f, 1f, 0.7f));
        }

        private void DrawControls()
        {
            Rect panel = new Rect(24f, Screen.height - 50f, Mathf.Min(620f, Screen.width - 48f), 28f);
            DrawRect(panel, PanelColor());
            GUI.Label(new Rect(panel.x + 14f, panel.y + 4f, panel.width - 28f, 20f), "WASD 이동  ·  마우스 시점  ·  ESC 커서  ·  R 재시작", bodyStyle);
        }

        private void DrawVariantTitle()
        {
            float elapsed = Time.unscaledTime - openedAt;
            if (elapsed > 4.5f)
            {
                return;
            }

            float alpha = elapsed < 3.5f ? 1f : 1f - (elapsed - 3.5f);
            Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.72f, 520f, 74f);
            Color background = PanelColor();
            background.a *= alpha;
            DrawRect(panel, background);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 34f), AlleyVariantInfo.DisplayName(AlleyVariantInfo.Current), centeredStyle);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 40f, panel.width - 24f, 22f), "타깃과 안전 거리를 유지하십시오", centeredStyle);
            GUI.color = previous;
        }

        private void DrawResult()
        {
            Rect shade = new Rect(0f, 0f, Screen.width, Screen.height);
            DrawRect(shade, new Color(0f, 0f, 0f, 0.62f));

            string heading = gameManager.Phase == GamePhase.Success ? "미행 성공" : "미행 실패";
            Rect box = new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.5f - 80f, 500f, 160f);
            DrawRect(box, new Color(0.02f, 0.025f, 0.05f, 0.96f));
            GUI.Label(new Rect(box.x + 20f, box.y + 24f, box.width - 40f, 36f), heading, centeredStyle);
            GUI.Label(new Rect(box.x + 20f, box.y + 70f, box.width - 40f, 28f), gameManager.StatusMessage, centeredStyle);
            GUI.Label(new Rect(box.x + 20f, box.y + 112f, box.width - 40f, 24f), "R 키로 다시 시작", centeredStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                normal = { textColor = AccentColor() }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.9f, 0.93f, 1f) }
            };

            centeredStyle = new GUIStyle(bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

        private static Color PanelColor()
        {
            return new Color(0.045f, 0.075f, 0.085f, 0.88f);
        }

        private static Color AccentColor()
        {
            return new Color(0.9f, 0.78f, 0.55f, 1f);
        }
    }
}
