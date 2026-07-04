using UnityEngine;

/// <summary>
/// Placed on the "Clue Trigger" child of a ClueObject.
/// Detects the player entering the trigger zone and, on Interact input,
/// adds the clue to the player's inventory and disables the parent ClueObject.
/// Mirrors the DummySpawner interaction pattern.
/// </summary>
public class ClueTrigger : MonoBehaviour
{
    [Tooltip("The clue asset this object grants when collected.")]
    [SerializeField] private ClueData clueData;

    [Tooltip("If set, this clue can only be interacted with while this quest is Active. Leave empty to allow collection at any time (current behavior).")]
    [SerializeField] private QuestData requiredQuest;

    /// <summary>Used by SaveManager to identify this trigger's clue when syncing world state.</summary>
    public string ClueId => clueData != null ? clueData.id : string.Empty;

    private PlayerInputHandler _playerInput;
    private QuestManager _questManager;
    private GameObject _interactionPrompt;

    void Awake()
    {
        var playerUI = GameObject.FindWithTag("PlayerUI");
        if (playerUI != null)
            _interactionPrompt = playerUI.transform.Find("Interaction")?.gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        var handler = other.GetComponent<PlayerInputHandler>();
        if (handler == null) return;

        _playerInput  = handler;
        _questManager = handler.GetComponentInParent<QuestManager>();
        UpdatePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() == null) return;

        _playerInput  = null;
        _questManager = null;
        _interactionPrompt?.SetActive(false);
    }

    void Update()
    {
        if (_playerInput == null) return;

        // Requirement can change while the player lingers in the trigger
        // (e.g. the required quest completes/fails elsewhere), so keep the prompt honest.
        UpdatePrompt();

        if (!_playerInput.GetInteractDown() || !IsRequiredQuestActive()) return;

        CollectClue();
    }

    void CollectClue()
    {
        if (clueData == null)
        {
            Debug.LogWarning($"[ClueTrigger] No ClueData assigned on {gameObject.name}.");
            return;
        }

        var inventory = _playerInput.GetComponentInParent<ClueInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[ClueTrigger] ClueInventory not found on player.");
            return;
        }

        inventory.AddClue(clueData);
        _questManager?.RecheckActiveQuests();
        _interactionPrompt?.SetActive(false);

        // Disable the parent ClueObject so this trigger no longer operates.
        transform.parent.gameObject.SetActive(false);
    }

    private void UpdatePrompt() => _interactionPrompt?.SetActive(IsRequiredQuestActive());

    private bool IsRequiredQuestActive() =>
        requiredQuest == null || (_questManager != null && _questManager.GetState(requiredQuest) == QuestState.Active);
}
