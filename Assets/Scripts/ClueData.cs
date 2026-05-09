using UnityEngine;

[CreateAssetMenu(fileName = "NewClue", menuName = "Afterlight/Clue")]
public class ClueData : ScriptableObject
{
    [Tooltip("Unique identifier used for save/load. Use snake_case, e.g. clue_001_carta. Must not change after release.")]
    public string id;

    [Tooltip("Display title shown in the clue list.")]
    public string title;

    [Tooltip("Full description text shown in the detail panel.")]
    [TextArea(3, 8)]
    public string description;
}
