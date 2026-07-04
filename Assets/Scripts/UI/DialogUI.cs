using TMPro;
using UnityEngine;

/// <summary>
/// Drives the NPC dialog screen UI.
///
/// Expected Canvas hierarchy:
///
///   DialogScreen  (tag: DialogScreen) (this component)
///   ├── Dialog Options              ← ScrollView
///   │   └── Viewport/Content       ← assign as optionsContainer
///   └── Dialog Response            ← assign as responseText (TMP_Text)
///
/// Starts inactive in the scene. Opened and closed by NPCInteraction / the close button.
/// Mirrors ClueListUI.cs.
/// </summary>
public class DialogUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Content transform inside the Dialog Options ScrollView.")]
    [SerializeField] private Transform optionsContainer;

    [Tooltip("Prefab for each dialog option button. Must have a DialogOptionButton component.")]
    [SerializeField] private GameObject optionButtonPrefab;

    [Tooltip("Text field that shows the NPC's greeting and responses.")]
    [SerializeField] private TMP_Text responseText;

    private ClueInventory _inventory;
    private QuestManager _questManager;
    private ConsequenceTracker _consequenceTracker;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the dialog screen for the given NPC.
    /// Pauses the game and unlocks the cursor.
    /// </summary>
    public void Open(NPCData data, ClueInventory inventory, QuestManager questManager, ConsequenceTracker consequenceTracker)
    {
        if (data == null) return;

        _inventory          = inventory;
        _questManager       = questManager;
        _consequenceTracker = consequenceTracker;

        // Show greeting
        if (responseText != null)
            responseText.text = data.greeting;

        // Build option buttons
        PopulateOptions(data);

        // Pause and unlock cursor
        Time.timeScale       = 0f;
        Cursor.lockState     = CursorLockMode.None;
        Cursor.visible       = true;

        gameObject.SetActive(true);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void PopulateOptions(NPCData data)
    {
        if (optionsContainer == null || optionButtonPrefab == null) return;

        // Clear previous buttons
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);

        // Regular options — only shown if required clue condition is met
        foreach (var option in data.options)
        {
            if (option == null) continue;

            bool requirementMet =
                (option.requiredClue == null || (_inventory != null && _inventory.IsCollected(option.requiredClue.id))) &&
                (option.requiredConsequence == null || (_consequenceTracker != null && _consequenceTracker.HasConsequence(option.requiredConsequence.id))) &&
                (option.forbiddenConsequence == null || _consequenceTracker == null || !_consequenceTracker.HasConsequence(option.forbiddenConsequence.id));

            if (!requirementMet) continue;

            SpawnButton(option, OnOptionSelected);
        }

        // Close option — always last
        var closeOption = new DialogOption
        {
            optionText   = data.closeOptionText,
            responseText = string.Empty
        };
        SpawnButton(closeOption, _ => Close());
    }

    private void SpawnButton(DialogOption option, System.Action<DialogOption> callback)
    {
        var go  = Instantiate(optionButtonPrefab, optionsContainer);
        var btn = go.GetComponent<DialogOptionButton>();
        if (btn == null)
        {
            Debug.LogWarning("[DialogUI] optionButtonPrefab is missing a DialogOptionButton component.");
            return;
        }
        btn.Setup(option, callback);
    }

    private void OnOptionSelected(DialogOption option)
    {
        // Show NPC response
        if (responseText != null)
            responseText.text = option.responseText;

        // Grant clue if applicable
        if (option.clueToGrant != null && _inventory != null)
            _inventory.AddClue(option.clueToGrant);

        // Start quest if applicable
        if (option.questToStart != null && _questManager != null)
            _questManager.StartQuest(option.questToStart);

        // Any option selected — even one that grants nothing directly — can be
        // the missing piece for a quest condition (e.g. a consequence gate).
        _questManager?.RecheckActiveQuests();
    }

    /// <summary>Closes the dialog, resumes the game and locks the cursor.</summary>
    public void Close()
    {
        gameObject.SetActive(false);

        Time.timeScale       = 1f;
        Cursor.lockState     = CursorLockMode.Locked;
        Cursor.visible       = false;
    }
}
