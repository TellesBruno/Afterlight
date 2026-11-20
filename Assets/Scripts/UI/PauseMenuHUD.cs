using TMPro;
using UnityEngine;

/// <summary>Read-only mission summary. Opening this panel never changes quest state.</summary>
public class PauseMenuHUD : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private TMP_Text objectiveText;

    private void OnEnable()
    {
        if (questManager != null)
            questManager.OnQuestStateChanged += OnQuestStateChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (questManager != null)
            questManager.OnQuestStateChanged -= OnQuestStateChanged;
    }

    private void OnQuestStateChanged(QuestData quest, QuestState state) => Refresh();

    private void Refresh()
    {
        if (objectiveText == null) return;
        QuestData selected = null;
        if (questManager != null)
        {
            foreach (var quest in questManager.GetActiveQuests())
            {
                if (selected == null) selected = quest;
                if (quest.questType == QuestType.Primary) { selected = quest; break; }
            }
        }
        objectiveText.text = selected != null ? selected.title : "No active objectives.";
    }
}
