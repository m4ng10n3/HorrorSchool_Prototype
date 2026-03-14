using UnityEngine;
using TMPro;

/// <summary>
/// HUD principale: timer a vista, item richiesto, tooltip oggetto guardato.
/// Aggancia a un GameObject dentro il Canvas; assegna i riferimenti dall'Inspector.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Sistema di gioco")]
    public TeacherRequestSystem teacherSystem;
    public PlayerInteractor playerInteractor;

    [Header("Timer")]
    public TextMeshProUGUI timerText;
    public Color timerNormalColor = Color.white;
    public Color timerUrgentColor = new Color(1f, 0.2f, 0.1f);
    public float urgentThreshold = 10f;

    [Header("Item richiesto")]
    public TextMeshProUGUI requestedItemText;

    [Header("Tooltip oggetto guardato")]
    public GameObject hoverPanel;
    public TextMeshProUGUI hoverDescriptionText;

    // ---------------------------------------------------------------

    private void Start()
    {
        if (teacherSystem == null)
            teacherSystem = FindFirstObjectByType<TeacherRequestSystem>();
        if (playerInteractor == null)
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        if (teacherSystem != null)
        {
            teacherSystem.OnNewRequest      += HandleNewRequest;
            teacherSystem.OnRequestCompleted += HandleCompleted;
            teacherSystem.OnRequestFailed   += HandleFailed;
        }

        if (hoverPanel != null)
            hoverPanel.SetActive(false);

        RefreshRequestedItemText();
    }

    private void OnDestroy()
    {
        if (teacherSystem != null)
        {
            teacherSystem.OnNewRequest      -= HandleNewRequest;
            teacherSystem.OnRequestCompleted -= HandleCompleted;
            teacherSystem.OnRequestFailed   -= HandleFailed;
        }
    }

    private void Update()
    {
        UpdateTimer();
        UpdateHoverTooltip();
    }

    // ---------------------------------------------------------------

    private void UpdateTimer()
    {
        if (timerText == null || teacherSystem == null) return;

        if (teacherSystem.RequestActive)
        {
            float t = Mathf.Max(teacherSystem.TimeRemaining, 0f);
            int minutes = (int)(t / 60f);
            int seconds = Mathf.CeilToInt(t % 60f);
            if (seconds == 60) { minutes++; seconds = 0; }  // edge case al secondo esatto
            timerText.text = $"{minutes:00}:{seconds:00}";
            timerText.color = t <= urgentThreshold ? timerUrgentColor : timerNormalColor;
        }
        else
        {
            timerText.text = "--:--";
            timerText.color = timerNormalColor;
        }
    }

    private void UpdateHoverTooltip()
    {
        if (hoverPanel == null || hoverDescriptionText == null || playerInteractor == null) return;

        string desc = playerInteractor.HoveredDescription;
        bool show = !string.IsNullOrEmpty(desc);
        hoverPanel.SetActive(show);
        if (show)
            hoverDescriptionText.text = desc;
    }

    private void RefreshRequestedItemText()
    {
        if (requestedItemText == null || teacherSystem == null) return;

        if (teacherSystem.RequestActive && teacherSystem.RequestedItem != null)
            requestedItemText.text = $"Porta: <b>{teacherSystem.RequestedItem.itemName}</b>";
        else
            requestedItemText.text = "In attesa...";
    }

    // ---------------------------------------------------------------

    private void HandleNewRequest(ItemData item, float timeLimit)
    {
        if (requestedItemText != null)
            requestedItemText.text = $"Porta: <b>{item.itemName}</b>";
    }

    private void HandleCompleted(ItemData item)
    {
        if (requestedItemText != null)
            requestedItemText.text = "<color=#44ff44>Consegnato!</color>";
    }

    private void HandleFailed(ItemData item)
    {
        if (requestedItemText != null)
            requestedItemText.text = "<color=#ff4444>Tempo scaduto!</color>";
    }
}
