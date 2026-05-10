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

    [Tooltip("If set, selecting this option grants this clue to the player's inventory.")]
    public ClueData clueToGrant;
}
