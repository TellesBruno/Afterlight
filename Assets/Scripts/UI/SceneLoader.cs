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

    public void LoadScene()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        SceneManager.LoadScene(targetSceneIndex);
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
