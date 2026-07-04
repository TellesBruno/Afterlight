using UnityEngine;

[CreateAssetMenu(fileName = "NewConsequence", menuName = "Afterlight/Consequence")]
public class ConsequenceData : ScriptableObject
{
    [Tooltip("Unique identifier used for save/load. Use snake_case, e.g. consequence_001_betrayed_guard. Must not change after release.")]
    public string id;

    [Tooltip("Developer-facing notes describing what this consequence represents and where it's checked. Never shown in any player-facing UI.")]
    [TextArea(3, 8)]
    public string devNotes;
}
