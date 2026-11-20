using UnityEngine;

/// <summary>
/// Placed on the Main Menu screen. Disables the Continue Game button's GameObject
/// if no save file exists yet, so the player can't try to load nothing.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject continueGameButton;

    void Start()
    {
        if (continueGameButton != null)
            continueGameButton.SetActive(SaveManager.HasSaveFile());
    }
}
