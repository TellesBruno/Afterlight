using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks quest state for the player and evaluates progress/fail conditions.
/// Quests must be explicitly started via StartQuest — only Active quests are
/// evaluated. Once a quest reaches Completed or Failed it is terminal and is
/// never re-evaluated, even if other conditions would later be met.
/// </summary>
public class QuestManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Master list of all quests. Used to resolve ids and enumerate quests for cascade evaluation.")]
    [SerializeField] private QuestRegistry registry;

    [Tooltip("ClueInventory used to check clue-based conditions and grant clue rewards. Typically the sibling component on the Player.")]
    [SerializeField] private ClueInventory clueInventory;

    [Tooltip("ConsequenceTracker used to check consequence-based conditions and apply consequences on complete/fail. Typically the sibling component on the Player.")]
    [SerializeField] private ConsequenceTracker consequenceTracker;

    /// <summary>Fired whenever a quest's state changes, for any reason (start, live completion/failure, or load-time restore). UI subscribes to this to refresh.</summary>
    public event Action<QuestData, QuestState> OnQuestStateChanged;

    private readonly Dictionary<string, QuestState> _states = new Dictionary<string, QuestState>();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (clueInventory != null) clueInventory.OnClueAdded += HandleClueAdded;
        if (consequenceTracker != null) consequenceTracker.OnConsequenceApplied += HandleConsequenceApplied;
    }

    void OnDisable()
    {
        if (clueInventory != null) clueInventory.OnClueAdded -= HandleClueAdded;
        if (consequenceTracker != null) consequenceTracker.OnConsequenceApplied -= HandleConsequenceApplied;
    }

    private void HandleClueAdded(ClueData clue) => EvaluateAll();
    private void HandleConsequenceApplied(ConsequenceData consequence) => EvaluateAll();

    // ── Queries ──────────────────────────────────────────────────────────────

    /// <summary>Returns the current state of the quest with the given id, or NotStarted if unknown.</summary>
    public QuestState GetState(string id) =>
        !string.IsNullOrEmpty(id) && _states.TryGetValue(id, out var state) ? state : QuestState.NotStarted;

    public QuestState GetState(QuestData quest) => quest == null ? QuestState.NotStarted : GetState(quest.id);

    public bool IsActive(string id) => GetState(id) == QuestState.Active;
    public bool IsCompleted(string id) => GetState(id) == QuestState.Completed;
    public bool IsFailed(string id) => GetState(id) == QuestState.Failed;

    /// <summary>Returns all quests currently Active, in registry order.</summary>
    public List<QuestData> GetActiveQuests()
    {
        var result = new List<QuestData>();
        if (registry == null) return result;

        foreach (var quest in registry.allQuests)
            if (quest != null && GetState(quest.id) == QuestState.Active)
                result.Add(quest);

        return result;
    }

    // ── Live transitions (grant rewards) ────────────────────────────────────

    /// <summary>Starts a quest (NotStarted → Active). No-op if already started or terminal.</summary>
    public void StartQuest(QuestData quest)
    {
        if (quest == null) return;
        if (GetState(quest.id) != QuestState.NotStarted) return;

        SetStateInternal(quest, QuestState.Active);
        Debug.Log($"[QuestManager] Quest started: {quest.id} — {quest.title}");
        EvaluateAll();
    }

    /// <summary>Completes a quest, granting its reward clue and applying its OnComplete consequences. No-op unless the quest is currently Active.</summary>
    public void CompleteQuest(QuestData quest)
    {
        if (quest == null) return;
        if (GetState(quest.id) != QuestState.Active) return;

        SetStateInternal(quest, QuestState.Completed);

        if (quest.rewardClue != null && clueInventory != null)
            clueInventory.AddClue(quest.rewardClue);
        ApplyConsequences(quest.consequencesOnComplete);

        Debug.Log($"[QuestManager] Quest completed: {quest.id} — {quest.title}");
        EvaluateAll();
    }

    /// <summary>Fails a quest and applies its OnFail consequences. No-op if Can Fail is false or the quest isn't Active.</summary>
    public void FailQuest(QuestData quest)
    {
        if (quest == null) return;
        if (!quest.canFail)
        {
            Debug.LogWarning($"[QuestManager] Attempted to fail non-failable quest '{quest.id}'.");
            return;
        }
        if (GetState(quest.id) != QuestState.Active) return;

        SetStateInternal(quest, QuestState.Failed);
        ApplyConsequences(quest.consequencesOnFail);

        Debug.Log($"[QuestManager] Quest failed: {quest.id} — {quest.title}");
        EvaluateAll();
    }

    private void ApplyConsequences(List<ConsequenceData> consequences)
    {
        if (consequences == null || consequenceTracker == null) return;
        foreach (var consequence in consequences)
            if (consequence != null)
                consequenceTracker.ApplyConsequence(consequence);
    }

    // ── Cascading evaluation ─────────────────────────────────────────────────

    /// <summary>
    /// Re-checks every Active quest's fail/complete conditions. Safe to call at
    /// any time — quests that are not Active are no-ops. Call this whenever
    /// gameplay state that a quest condition could depend on changes: a clue is
    /// collected, a consequence is applied, or a dialog option is selected.
    /// Redundant calls (e.g. on top of the automatic ones below) are harmless —
    /// quests only transition once, so extra passes just find nothing to do.
    /// </summary>
    public void RecheckActiveQuests() => EvaluateAll();

    /// <summary>
    /// Re-checks every Active quest's fail/complete conditions. Called after any
    /// state change (start/complete/fail/clue gained/consequence applied).
    /// Terminates because quest states are monotonic: each quest can transition
    /// at most once, so nested calls triggered by transitions are bounded by
    /// the total number of quests.
    /// </summary>
    private void EvaluateAll()
    {
        if (registry == null) return;
        foreach (var quest in registry.allQuests)
            EvaluateQuest(quest);
    }

    private void EvaluateQuest(QuestData quest)
    {
        if (quest == null) return;
        if (GetState(quest.id) != QuestState.Active) return; // terminal / not-started guard

        // Fail is checked before completion — a quest whose fail and complete
        // conditions are both met in the same pass always fails, never completes.
        if (quest.canFail && !quest.failCondition.IsEmpty && quest.failCondition.IsMet(clueInventory, this, consequenceTracker))
        {
            FailQuest(quest);
            return;
        }

        if (!quest.completeCondition.IsEmpty && quest.completeCondition.IsMet(clueInventory, this, consequenceTracker))
        {
            CompleteQuest(quest);
        }
    }

    // ── Save / Load ──────────────────────────────────────────────────────────

    /// <summary>Returns every tracked quest's id + state for serialization.</summary>
    public QuestSaveData[] GetQuestStates()
    {
        var result = new QuestSaveData[_states.Count];
        int i = 0;
        foreach (var pair in _states)
            result[i++] = new QuestSaveData { id = pair.Key, state = (int)pair.Value };
        return result;
    }

    /// <summary>
    /// Restores quest states from a save file. Sets state directly and fires
    /// OnQuestStateChanged for UI refresh, but does not grant rewards or
    /// trigger cascade evaluation — the saved snapshot is already the resolved
    /// end-state of a previous evaluation pass.
    /// </summary>
    public void LoadQuestStates(QuestSaveData[] savedStates)
    {
        if (savedStates == null || registry == null) return;

        _states.Clear();

        foreach (var entry in savedStates)
        {
            var quest = registry.GetById(entry.id);
            if (quest == null)
            {
                Debug.LogWarning($"[QuestManager] Saved quest id '{entry.id}' not found in registry. Skipping.");
                continue;
            }
            SetStateInternal(quest, (QuestState)entry.state);
        }
    }

    /// <summary>Single primitive shared by live transitions and Load: writes the dictionary and fires the UI event. Never grants rewards, never cascades — callers own that.</summary>
    private void SetStateInternal(QuestData quest, QuestState newState)
    {
        _states[quest.id] = newState;
        OnQuestStateChanged?.Invoke(quest, newState);
    }
}
