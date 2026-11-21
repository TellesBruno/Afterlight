using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Drives the quest detail panel UI.
///
/// Expected Canvas hierarchy:
///
///   QuestScreen (this component)
///   ├── QuestListScrollView
///   │   └── Viewport/Content    ← assign as buttonContainer
///   ├── TitleText (TMP_Text)    ← assign as titleText
///   └── DescriptionScrollView
///       └── Viewport/Content/DescriptionText (TMP_Text) ← assign as descriptionText
/// </summary>
public class QuestListUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private QuestRegistry registry;
    [SerializeField] private QuestManager manager;

    [Header("UI References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject questButtonPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private const string Locked = "???";

    // Maps quest id → instantiated button item for O(1) refresh.
    private readonly Dictionary<string, QuestButtonItem> _buttonMap = new Dictionary<string, QuestButtonItem>();
    private QuestData _selectedQuest;
    private QuestState _selectedState;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        LocalizationService.LanguageChanged += RefreshSelection;
        if (manager != null)
            manager.OnQuestStateChanged += OnQuestStateChanged;

        PopulateList();
        ClearDetail();
    }

    void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshSelection;
        if (manager != null)
            manager.OnQuestStateChanged -= OnQuestStateChanged;
    }

    // ── List Building ────────────────────────────────────────────────────────

    private void PopulateList()
    {
        if (registry == null || buttonContainer == null || questButtonPrefab == null) return;

        // Destroy previous buttons (e.g. if panel was reopened)
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
        _buttonMap.Clear();

        foreach (var quest in registry.allQuests)
        {
            if (quest == null) continue;

            QuestState state = manager != null ? manager.GetState(quest.id) : QuestState.NotStarted;

            var go   = Instantiate(questButtonPrefab, buttonContainer);
            var item = go.GetComponent<QuestButtonItem>();
            if (item == null)
            {
                Debug.LogWarning("[QuestListUI] questButtonPrefab is missing a QuestButtonItem component.");
                continue;
            }

            item.Setup(quest, state, OnButtonClicked);
            _buttonMap[quest.id] = item;
        }
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    private void OnQuestStateChanged(QuestData quest, QuestState state)
    {
        if (quest == null) return;

        if (_buttonMap.TryGetValue(quest.id, out var item))
            item.SetState(state);
        if (_selectedQuest == quest)
        {
            _selectedState = state;
            RefreshSelection();
        }
    }

    private void OnButtonClicked(QuestData quest, QuestState state)
    {
        _selectedQuest = quest;
        _selectedState = state;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (_selectedQuest == null) return;
        if (_selectedState != QuestState.NotStarted)
        {
            titleText.text       = LocalizationService.QuestTitle(_selectedQuest);
            descriptionText.text = LocalizationService.QuestDescription(_selectedQuest);
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
        _selectedQuest = null;
        if (titleText != null)       titleText.text = string.Empty;
        if (descriptionText != null) descriptionText.text = string.Empty;
    }
}
