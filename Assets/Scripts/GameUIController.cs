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
    [SerializeField] private GameObject clueMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private DialogUI dialogScreen;

    void Update()
    {
        if (inputHandler == null) return;

        if (inputHandler.GetReturnDown())   HandleReturnInput();
        if (inputHandler.GetClueMenuDown()) HandleClueMenuInput();
        if (inputHandler.GetSaveDown())     SaveManager.Instance?.Save();
        if (inputHandler.GetLoadDown())     SaveManager.Instance?.Load();
    }

    // ── Input handlers ───────────────────────────────────────────────────────

    private void HandleReturnInput()
    {
        if (clueMenu != null && clueMenu.activeSelf)                         { CloseClueMenu();        return; }
        if (pauseMenu != null && pauseMenu.activeSelf)                       { ClosePauseMenu();       return; }
        if (dialogScreen != null && dialogScreen.gameObject.activeSelf)      { dialogScreen.Close();   return; }
        OpenPauseMenu();
    }

    private void HandleClueMenuInput()
    {
        if (clueMenu != null && clueMenu.activeSelf) { CloseClueMenu(); return; }
        if (!IsAnyMenuOpen()) OpenClueMenu();
    }

    // ── Menu control ─────────────────────────────────────────────────────────

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
        (clueMenu     != null && clueMenu.activeSelf)                    ||
        (pauseMenu    != null && pauseMenu.activeSelf)                   ||
        (dialogScreen != null && dialogScreen.gameObject.activeSelf);

    private void PauseGame()
    {
        Time.timeScale   = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void ResumeGame()
    {
        Time.timeScale   = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
