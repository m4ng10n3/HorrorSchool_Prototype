using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mostra il pannello Game Over quando il teacher cattura il player.
///
/// Setup nel Canvas:
///   GameOverScreen (questo script)
///   └── GameOverPanel
///         ├── Text - TMP  →  assegna a "messageText"
///         ├── Button Restart  →  OnClick: trascina GameOverScreen, scegli Restart()
///         └── Button Quit     →  OnClick: trascina GameOverScreen, scegli Quit()
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Pannello")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI messageText;
    public string gameOverMessage = "GAME OVER";

    [Header("Riferimenti scena (auto-trovati se lasciati vuoti)")]
    public TeacherAI teacherAI;
    public TeacherRequestSystem teacherSystem;
    public PlayerController playerController;
    public PlayerInteractor playerInteractor;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Start()
    {
        // Auto-find se non assegnati dall'Inspector
        if (teacherAI     == null) teacherAI     = FindFirstObjectByType<TeacherAI>();
        if (teacherSystem == null) teacherSystem = FindFirstObjectByType<TeacherRequestSystem>();
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerInteractor == null) playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        if (teacherAI != null)
            teacherAI.OnPlayerCaught += ShowGameOver;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (teacherAI != null)
            teacherAI.OnPlayerCaught -= ShowGameOver;
    }

    // ── Mostra Game Over ──────────────────────────────────────────

    private void ShowGameOver()
    {
        // Ferma timer e richieste
        if (teacherSystem != null)
            teacherSystem.Freeze();

        // Mostra pannello
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (messageText != null)
            messageText.text = gameOverMessage;

        // Blocca player
        if (playerController != null) playerController.enabled = false;
        if (playerInteractor  != null) playerInteractor.enabled  = false;

        // Sblocca cursore per i bottoni
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Bottoni (collegare dall'Inspector tramite OnClick) ────────

    public void Restart()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
