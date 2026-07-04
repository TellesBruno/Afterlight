using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static bool WasNewGame;

    private const int SampleSceneIndex = 0;

    [SerializeField] private int targetSceneIndex;
    [SerializeField] private bool saveBeforeLeave;
    [SerializeField] private bool loadSaveOnEnter;
    [SerializeField] private bool deleteSaveOnLoad;

    [Header("Quests (optional)")]
    [Tooltip("QuestManager in the current scene. Only required if Quests To Fail On Leave is used.")]
    [SerializeField] private QuestManager questManager;

    [Tooltip("Quests that can no longer be completed once this transition happens. Any of these still Active are force-failed before leaving (the quest's Can Fail must be enabled). Leave empty to fail none.")]
    [SerializeField] private List<QuestData> questsToFailOnLeave = new List<QuestData>();

    [Tooltip("Quests started once the target scene finishes loading. Leave empty to start none.")]
    [SerializeField] private List<QuestData> questsToStartOnEnter = new List<QuestData>();

    public void LoadScene()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        FailOpenQuests();

        if (saveBeforeLeave && SceneManager.GetActiveScene().buildIndex == SampleSceneIndex)
            SaveManager.Instance?.Save();

        if (deleteSaveOnLoad)
        {
            SaveManager.Instance?.DeleteSave();
            WasNewGame = true;
        }

        if (loadSaveOnEnter && targetSceneIndex == SampleSceneIndex)
        {
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> handler = null;
            handler = (scene, mode) =>
            {
                SceneManager.sceneLoaded -= handler;
                SaveManager.Instance?.Load();
            };
            SceneManager.sceneLoaded += handler;
        }

        StartQuestsOnEnterWhenLoaded();

        SceneManager.LoadScene(targetSceneIndex);
    }

    // ── Quests ───────────────────────────────────────────────────────────────

    /// <summary>Force-fails any still-Active quest in questsToFailOnLeave before this scene is left.</summary>
    private void FailOpenQuests()
    {
        if (questsToFailOnLeave.Count == 0) return;

        if (questManager == null)
        {
            Debug.LogWarning("[SceneLoader] Quests To Fail On Leave is set but no QuestManager reference is assigned.");
            return;
        }

        foreach (var quest in questsToFailOnLeave)
        {
            if (quest == null) continue;
            if (questManager.GetState(quest) == QuestState.Active)
                questManager.FailQuest(quest);
        }
    }

    /// <summary>Starts questsToStartOnEnter on the QuestManager found in the target scene, once it finishes loading.</summary>
    private void StartQuestsOnEnterWhenLoaded()
    {
        if (questsToStartOnEnter.Count == 0) return;

        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> handler = null;
        handler = (scene, mode) =>
        {
            SceneManager.sceneLoaded -= handler;

            var loadedQuestManager = FindFirstObjectByType<QuestManager>();
            if (loadedQuestManager == null)
            {
                Debug.LogWarning("[SceneLoader] Quests To Start On Enter is set but no QuestManager was found in the loaded scene.");
                return;
            }

            foreach (var quest in questsToStartOnEnter)
                if (quest != null)
                    loadedQuestManager.StartQuest(quest);
        };
        SceneManager.sceneLoaded += handler;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (saveBeforeLeave && SceneManager.GetActiveScene().buildIndex == SampleSceneIndex)
            SaveManager.Instance?.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
