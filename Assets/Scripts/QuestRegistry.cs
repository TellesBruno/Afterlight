using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestRegistry", menuName = "Afterlight/Quest Registry")]
public class QuestRegistry : ScriptableObject
{
    [Tooltip("All quests that exist in the game, in the order they should appear in the UI list.")]
    public List<QuestData> allQuests = new List<QuestData>();

    /// <summary>Returns the QuestData matching the given id, or null if not found.</summary>
    public QuestData GetById(string id)
    {
        foreach (var quest in allQuests)
        {
            if (quest != null && quest.id == id)
                return quest;
        }
        return null;
    }
}
