using UnityEngine;

/// <summary>
/// Handles global UI input: toggle Clue Menu (I), Save (F5), Load (F8).
/// Manages game pause and cursor visibility when a menu is open.
///
/// Place on any persistent GameObject in the scene.
/// Assign the references in the Inspector.
/// </summary>
public class GameUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private GameObject clueMenu;

    private bool _isMenuOpen;

    void Update()
    {
        if (inputHandler == null) return;

        if (inputHandler.GetClueMenuDown())
            ToggleClueMenu();

        if (inputHandler.GetSaveDown())
            SaveManager.Instance?.Save();

        if (inputHandler.GetLoadDown())
            SaveManager.Instance?.Load();
    }

    // ── Menu control ─────────────────────────────────────────────────────────

    private void ToggleClueMenu()
    {
        if (_isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        if (clueMenu == null) return;

        _isMenuOpen = true;
        clueMenu.SetActive(true);       // triggers ClueListUI.OnEnable → PopulateList

        Time.timeScale       = 0f;
        Cursor.lockState     = CursorLockMode.None;
        Cursor.visible       = true;
    }

    private void CloseMenu()
    {
        if (clueMenu == null) return;

        _isMenuOpen = false;
        clueMenu.SetActive(false);

        Time.timeScale       = 1f;
        Cursor.lockState     = CursorLockMode.Locked;
        Cursor.visible       = false;
    }
}
