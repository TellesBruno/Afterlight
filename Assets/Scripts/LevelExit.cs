using System;
using UnityEngine;

/// <summary>
/// Placed on a LevelExit GameObject (BoxCollider with Is Trigger enabled).
/// Shows the FinishLevelInteraction UI when the player enters the trigger zone.
/// If the player has all required clues, pressing Interact (E) pauses the game
/// and opens the FinishLevel confirmation menu.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Header("Required Clues")]
    [Tooltip("Todas as clues necessárias para finalizar a fase. Deixe vazio para saída livre.")]
    [SerializeField] private ClueData[] _requiredClues = Array.Empty<ClueData>();

    [Header("References")]
    [SerializeField] private GameUIController _gameUI;

    private PlayerInputHandler _playerInput;
    private ClueInventory _inventory;

    private GameObject _finishLevelInteractionUI;
    private GameObject _cannotExitUI;
    private GameObject _canExitUI;

    void Awake()
    {
        var playerUI = GameObject.FindWithTag("PlayerUI");
        if (playerUI != null)
        {
            var finishInteraction = playerUI.transform.Find("FinishLevelInteraction");
            if (finishInteraction != null)
            {
                _finishLevelInteractionUI = finishInteraction.gameObject;
                _cannotExitUI = finishInteraction.Find("CannotExit")?.gameObject;
                _canExitUI    = finishInteraction.Find("CanExit")?.gameObject;
            }
            else
            {
                Debug.LogWarning("[LevelExit] 'FinishLevelInteraction' not found under PlayerUI.");
            }
        }
        else
        {
            Debug.LogWarning("[LevelExit] No GameObject with tag 'PlayerUI' found in scene.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var handler = other.GetComponent<PlayerInputHandler>();
        if (handler == null) return;

        _playerInput = handler;
        _inventory   = handler.GetComponentInParent<ClueInventory>();

        ShowExitUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() == null) return;

        _playerInput = null;
        _inventory   = null;

        HideExitUI();
    }

    void Update()
    {
        if (_playerInput == null || !_playerInput.GetInteractDown()) return;
        if (!HasAllClues()) return;

        _gameUI.OpenFinishLevel();
    }

    void ShowExitUI()
    {
        _finishLevelInteractionUI?.SetActive(true);

        bool canExit = HasAllClues();
        _cannotExitUI?.SetActive(!canExit);
        _canExitUI?.SetActive(canExit);
    }

    void HideExitUI()
    {
        _cannotExitUI?.SetActive(false);
        _canExitUI?.SetActive(false);
        _finishLevelInteractionUI?.SetActive(false);
    }

    bool HasAllClues()
    {
        if (_inventory == null) return _requiredClues.Length == 0;
        foreach (var clue in _requiredClues)
            if (clue != null && !_inventory.IsCollected(clue.id)) return false;
        return true;
    }
}
