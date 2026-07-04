using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConsequenceRegistry", menuName = "Afterlight/Consequence Registry")]
public class ConsequenceRegistry : ScriptableObject
{
    [Tooltip("All hidden consequences that exist in the game.")]
    public List<ConsequenceData> allConsequences = new List<ConsequenceData>();

    /// <summary>Returns the ConsequenceData matching the given id, or null if not found.</summary>
    public ConsequenceData GetById(string id)
    {
        foreach (var consequence in allConsequences)
        {
            if (consequence != null && consequence.id == id)
                return consequence;
        }
        return null;
    }
}
