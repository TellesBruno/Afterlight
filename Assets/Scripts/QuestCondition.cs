using System;
using UnityEngine;

/// <summary>
/// A set of optional requirements combined with AND semantics. Leaving a field
/// empty means that requirement is not checked. A condition with every field
/// empty is never automatically satisfied (see IsEmpty) — use it for quests
/// that should only be completed/failed via a direct script call.
/// </summary>
[Serializable]
public class QuestCondition
{
    [Tooltip("If set, requires the player to have collected this clue.")]
    public ClueData requiredClue;

    [Tooltip("If set, requires Required Quest to be in Required Quest State.")]
    public QuestData requiredQuest;

    [Tooltip("State that Required Quest must be in. Only used if Required Quest is set.")]
    public QuestState requiredQuestState = QuestState.Completed;

    [Tooltip("If set, requires this (hidden, dev-only) consequence to have been applied.")]
    public ConsequenceData requiredConsequence;

    /// <summary>True if no requirement fields are set. Such a condition is never met automatically.</summary>
    public bool IsEmpty => requiredClue == null && requiredQuest == null && requiredConsequence == null;

    /// <summary>Returns true if every set requirement field is currently satisfied.</summary>
    public bool IsMet(ClueInventory clueInventory, QuestManager questManager, ConsequenceTracker consequenceTracker)
    {
        if (requiredClue != null && (clueInventory == null || !clueInventory.IsCollected(requiredClue.id)))
            return false;

        if (requiredQuest != null && (questManager == null || questManager.GetState(requiredQuest.id) != requiredQuestState))
            return false;

        if (requiredConsequence != null && (consequenceTracker == null || !consequenceTracker.HasConsequence(requiredConsequence.id)))
            return false;

        return true;
    }
}
