using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Drives the clue detail panel UI.
///
/// Expected Canvas hierarchy:
///
///   ClueScreen (this component)
///   ├── ClueListScrollView
///   │   └── Viewport/Content    ← assign as buttonContainer
///   ├── TitleText (TMP_Text)    ← assign as titleText
///   └── DescriptionScrollView
///       └── Viewport/Content/DescriptionText (TMP_Text) ← assign as descriptionText
/// </summary>
public class ClueListUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ClueRegistry registry;
    [SerializeField] private ClueInventory inventory;

    [Header("UI References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject clueButtonPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private const string Locked = "???";

    // Maps clue id → instantiated button item for O(1) refresh.
    private readonly Dictionary<string, ClueButtonItem> _buttonMap = new Dictionary<string, ClueButtonItem>();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (inventory != null)
            inventory.OnClueAdded += OnClueAdded;

        PopulateList();
        ClearDetail();
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnClueAdded -= OnClueAdded;
    }

    // ── List Building ────────────────────────────────────────────────────────

    private void PopulateList()
    {
        if (registry == null || buttonContainer == null || clueButtonPrefab == null) return;

        // Destroy previous buttons (e.g. if panel was reopened)
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
        _buttonMap.Clear();

        foreach (var clue in registry.allClues)
        {
            if (clue == null) continue;

            bool collected = inventory != null && inventory.IsCollected(clue.id);

            var go   = Instantiate(clueButtonPrefab, buttonContainer);
            var item = go.GetComponent<ClueButtonItem>();
            if (item == null)
            {
                Debug.LogWarning("[ClueListUI] clueButtonPrefab is missing a ClueButtonItem component.");
                continue;
            }

            item.Setup(clue, collected, OnButtonClicked);
            _buttonMap[clue.id] = item;
        }
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    private void OnClueAdded(ClueData clue)
    {
        if (clue == null) return;

        if (_buttonMap.TryGetValue(clue.id, out var item))
            item.MarkCollected();
    }

    private void OnButtonClicked(ClueData clue, bool isCollected)
    {
        if (isCollected)
        {
            titleText.text       = clue.title;
            descriptionText.text = clue.description;
        }
        else
        {
            titleText.text       = Locked;
            descriptionText.text = Locked;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ClearDetail()
    {
        if (titleText != null)       titleText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
    }
}
