using UnityEngine;

/// <summary>
/// Handles global UI input: toggle Clue Menu (I), Pause Menu (Return/Esc), Save (F5), Load (F8).
/// Enforces mutual exclusion — no menu opens if another is already active.
/// Manages game pause and cursor visibility when any menu is open.
///
/// Place on any persistent GameObject in the scene.
/// Assign the references in the Inspector.
/// </summary>
public class GameUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject clueMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject tutorialScreen;
    [SerializeField] private GameObject finishLevelScreen;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private DialogUI dialogScreen;

    private void Start()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += OpenDeathScreen;

        if (SceneLoader.WasNewGame)
        {
            SceneLoader.WasNewGame = false;
            OpenTutorial();
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= OpenDeathScreen;
    }

    void Update()
    {
        if (inputHandler == null) return;

        if (inputHandler.GetReturnDown())   HandleReturnInput();
        if (inputHandler.GetClueMenuDown()) HandleClueMenuInput();
        if (inputHandler.GetSaveDown() && (CombatManager.Instance == null || !CombatManager.Instance.IsInCombat))
            SaveManager.Instance?.Save();
        if (inputHandler.GetLoadDown())     SaveManager.Instance?.Load();
    }

    // ── Input handlers ───────────────────────────────────────────────────────

    private void HandleReturnInput()
    {
        if (deathScreen       != null && deathScreen.activeSelf)             return; // não dispensável
        if (clueMenu          != null && clueMenu.activeSelf)                { CloseClueMenu();        return; }
        if (tutorialScreen    != null && tutorialScreen.activeSelf)          { CloseTutorial();        return; }
        if (finishLevelScreen != null && finishLevelScreen.activeSelf)       { CloseFinishLevel();     return; }
        if (pauseMenu         != null && pauseMenu.activeSelf)               { ClosePauseMenu();       return; }
        if (dialogScreen      != null && dialogScreen.gameObject.activeSelf) { dialogScreen.Close();   return; }
        OpenPauseMenu();
    }

    private void HandleClueMenuInput()
    {
        if (clueMenu != null && clueMenu.activeSelf) { CloseClueMenu(); return; }
        if (!IsAnyMenuOpen()) OpenClueMenu();
    }

    // ── Menu control ─────────────────────────────────────────────────────────

    public void OpenTutorial()
    {
        if (tutorialScreen == null) return;
        tutorialScreen.SetActive(true);
        PauseGame();
    }

    public void CloseTutorial()
    {
        if (tutorialScreen == null) return;
        tutorialScreen.SetActive(false);
        ResumeGame();
    }

    public void OpenDeathScreen()
    {
        if (deathScreen == null) return;
        deathScreen.SetActive(true);
        PauseGame();
    }

    public void OpenFinishLevel()
    {
        if (finishLevelScreen == null) return;
        finishLevelScreen.SetActive(true);
        PauseGame();
    }

    public void CloseFinishLevel()
    {
        if (finishLevelScreen == null) return;
        finishLevelScreen.SetActive(false);
        ResumeGame();
    }

    public void OpenTutorialFromPauseMenu()
    {
        if (pauseMenu == null || tutorialScreen == null) return;
        pauseMenu.SetActive(false);
        tutorialScreen.SetActive(true);
    }

    private void OpenClueMenu()
    {
        if (clueMenu == null) return;
        clueMenu.SetActive(true);
        PauseGame();
    }

    private void CloseClueMenu()
    {
        if (clueMenu == null) return;
        clueMenu.SetActive(false);
        ResumeGame();
    }

    private void OpenPauseMenu()
    {
        if (pauseMenu == null) return;
        pauseMenu.SetActive(true);
        PauseGame();
    }

    private void ClosePauseMenu()
    {
        if (pauseMenu == null) return;
        pauseMenu.SetActive(false);
        ResumeGame();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool IsAnyMenuOpen() =>
        (clueMenu          != null && clueMenu.activeSelf)               ||
        (tutorialScreen    != null && tutorialScreen.activeSelf)         ||
        (finishLevelScreen != null && finishLevelScreen.activeSelf)      ||
        (deathScreen       != null && deathScreen.activeSelf)            ||
        (pauseMenu         != null && pauseMenu.activeSelf)              ||
        (dialogScreen      != null && dialogScreen.gameObject.activeSelf);

    public void PauseGame()
    {
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void ResumeGame()
    {
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
