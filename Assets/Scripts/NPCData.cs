using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the dialog data for a single NPC: greeting, conversation options,
/// and the always-present close option.
///
/// Create via: Assets → Create → Afterlight → NPC Dialog
/// </summary>
[CreateAssetMenu(fileName = "NewNPCDialog", menuName = "Afterlight/NPC Dialog")]
public class NPCData : ScriptableObject
{
    [Tooltip("Unique identifier for this NPC. Use snake_case, e.g. npc_001_guarda. Must not change after release.")]
    public string id;

    [Tooltip("First line the NPC says when the player opens the dialog.")]
    [TextArea(2, 6)]
    public string greeting;

    [Tooltip("List of dialog options available to the player. Options with a Required Clue are hidden until that clue is collected.")]
    public List<DialogOption> options = new List<DialogOption>();

    [Tooltip("Text on the button that always appears last and closes the conversation.")]
    public string closeOptionText = "Até logo.";
}
