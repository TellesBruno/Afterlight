using System.Collections.Generic;
using UnityEngine;

public enum QuestType
{
    Primary,
    Secondary
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "Afterlight/Quest")]
public class QuestData : ScriptableObject
{
    [Tooltip("Unique identifier used for save/load. Use snake_case, e.g. quest_001_find_key. Must not change after release.")]
    public string id;

    [Tooltip("Display title shown in the quest list.")]
    public string title;

    [Tooltip("Full description text shown in the detail panel.")]
    [TextArea(3, 8)]
    public string description;

    [Header("Classification")]
    [Tooltip("Primary quests drive the main story; Secondary quests are optional side content.")]
    public QuestType questType = QuestType.Secondary;

    [Tooltip("Only meaningful for Primary quests. Always false for Secondary quests.")]
    public bool isMandatory;

    [Tooltip("If true, this quest can transition to Failed when Fail Condition is met.")]
    public bool canFail;

    [Header("Progress Conditions")]
    [Tooltip("Condition that must be met for this quest to auto-complete. Leave every field empty to require a manual QuestManager.CompleteQuest(...) call instead.")]
    public QuestCondition completeCondition;

    [Tooltip("Condition that must be met for this quest to auto-fail. Only evaluated if Can Fail is true. Leave empty to only allow manual QuestManager.FailQuest(...) calls.")]
    public QuestCondition failCondition;

    [Header("Rewards & Consequences")]
    [Tooltip("If set, granted to the player's ClueInventory when this quest completes.")]
    public ClueData rewardClue;

    [Tooltip("Hidden consequences applied (never shown in UI) when this quest completes.")]
    public List<ConsequenceData> consequencesOnComplete = new List<ConsequenceData>();

    [Tooltip("Hidden consequences applied (never shown in UI) when this quest fails.")]
    public List<ConsequenceData> consequencesOnFail = new List<ConsequenceData>();

    private void OnValidate()
    {
        if (questType == QuestType.Secondary && isMandatory)
            isMandatory = false;
    }
}
