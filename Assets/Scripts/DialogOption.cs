using System;
using UnityEngine;

/// <summary>
/// A single dialog option shown as a button during an NPC conversation.
/// Embedded directly in NPCData — not a ScriptableObject.
/// </summary>
[Serializable]
public class DialogOption
{
    [Tooltip("Text displayed on the dialog button.")]
    public string optionText;

    [Tooltip("NPC response text shown when this option is selected.")]
    [TextArea(2, 6)]
    public string responseText;

    [Tooltip("If set, this option is only shown when the player has collected this clue.")]
    public ClueData requiredClue;

    [Tooltip("If set, this option is only shown once this (hidden, dev-only) consequence has been applied.")]
    public ConsequenceData requiredConsequence;

    [Tooltip("If set, this option is hidden once this (hidden, dev-only) consequence has been applied.")]
    public ConsequenceData forbiddenConsequence;

    [Tooltip("If set, selecting this option grants this clue to the player's inventory.")]
    public ClueData clueToGrant;

    [Tooltip("If set, selecting this option starts this quest.")]
    public QuestData questToStart;
}
