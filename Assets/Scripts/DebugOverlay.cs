using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private Key toggleKey = Key.F3;
    [SerializeField] private bool startVisible = false;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private int targetFPS = 60;

    private bool visible;
    private float smoothDt = 0.016f;
    private readonly StringBuilder sb = new StringBuilder(256);
    private GUIStyle style;
    private GUIStyle boxStyle;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;

        if (!Application.isEditor && !Debug.isDebugBuild)
        {
            Destroy(this);
            return;
        }
        visible = startVisible;
    }

    private void Update()
    {
        smoothDt += (Time.unscaledDeltaTime - smoothDt) * 0.1f;

        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true };
            style.normal.textColor = Color.white;
            boxStyle = new GUIStyle(GUI.skin.box);
        }

        GameManager gm = GameManager.Instance;
        sb.Clear();

        float fps = smoothDt > 0f ? 1f / smoothDt : 0f;
        sb.Append("FPS: ").Append(fps.ToString("0")).Append("  (").Append((smoothDt * 1000f).ToString("0.0")).AppendLine(" ms)");
        sb.Append("State: ").AppendLine(GetGameState());

        string area = string.IsNullOrEmpty(SaveSystem.CurrentAreaName)
            ? SceneManager.GetActiveScene().name
            : SaveSystem.CurrentAreaName;
        sb.Append("Area: ").Append(area).Append("   Slot: ").AppendLine(SaveSystem.CurrentSlot.ToString());

        if (gm.Reputation != null) sb.Append("Reputation: ").AppendLine(gm.Reputation.reputation.ToString());
        if (gm.Inventory != null) sb.Append("Mora: ").AppendLine(gm.Inventory.Currency.ToString());
        if (gm.Health != null) sb.Append("Health: ").AppendLine(gm.Health.CurrentHealth.ToString("0.0"));
        if (gm.Stamina != null) sb.Append("Stamina: ").AppendLine(gm.Stamina.CurrentStamina.ToString("0.0"));

        if (gm.Player != null)
        {
            Vector3 p = gm.Player.transform.position;
            sb.Append("Pos: ").Append(p.x.ToString("0.0")).Append(", ").Append(p.y.ToString("0.0")).Append(", ").AppendLine(p.z.ToString("0.0"));
        }

        IInteractable active = InteractionSelector.Active;
        if (active != null) sb.Append("Interact: ").AppendLine(active.InteractionText);

        string text = sb.ToString();
        Vector2 size = style.CalcSize(new GUIContent(text));
        Rect r = new Rect(10, 10, size.x + 16, size.y + 12);
        GUI.Box(r, GUIContent.none, boxStyle);
        GUI.Label(new Rect(r.x + 8, r.y + 6, size.x + 4, size.y), text, style);
    }

    private static string GetGameState()
    {
        if (Time.timeScale == 0f || GameplayBlocker.IsBlocked(BlockFlags.FreezeTime)) return "Paused";
        if (DialogueManager.IsDialogueActive) return "Dialogue";
        if (NPCInteraction.Current != null) return "Interacting";
        if (GameplayBlocker.Current != BlockFlags.None) return "Blocked (" + GameplayBlocker.Current + ")";
        return "Playing";
    }
}