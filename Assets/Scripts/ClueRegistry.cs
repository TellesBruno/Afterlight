using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClueRegistry", menuName = "Afterlight/Clue Registry")]
public class ClueRegistry : ScriptableObject
{
    [Tooltip("All clues that exist in the game, in the order they should appear in the UI list.")]
    public List<ClueData> allClues = new List<ClueData>();

    /// <summary>Returns the ClueData matching the given id, or null if not found.</summary>
    public ClueData GetById(string id)
    {
        foreach (var clue in allClues)
        {
            if (clue != null && clue.id == id)
                return clue;
        }
        return null;
    }
}
