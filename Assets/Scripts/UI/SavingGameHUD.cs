using System.Collections;
using UnityEngine;

/// <summary>
/// Exibe o elemento SavingGame (filho de PlayerUI) com um fade-in/out suave
/// sempre que o jogo é salvo com sucesso (SaveManager.OnGameSaved). Mirrors
/// the ActiveQuestsHUD/CollectedClueHUD fade pattern.
///
/// Setup:
///   1. O GameObject PlayerUI/SavingGame deve ter um CanvasGroup (alpha 0).
///   2. Adicione este script no mesmo objeto que PlayerHUD (ou no Player).
/// </summary>
public class SavingGameHUD : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Segundos que o elemento fica totalmente visível após salvar.")]
    [SerializeField] private float visibleDuration = 2f;
    [Tooltip("Duração em segundos do fade de visível para invisível.")]
    [SerializeField] private float fadeDuration = 1f;

    private CanvasGroup _group;
    private Coroutine   _fadeCoroutine;

    void Start()
    {
        var root = GameObject.FindWithTag("PlayerUI");
        if (root != null)
        {
            var savingGame = root.transform.Find("SavingGame");
            if (savingGame != null)
                _group = savingGame.GetComponent<CanvasGroup>();
        }

        if (_group == null)
        {
            Debug.LogWarning("[SavingGameHUD] CanvasGroup não encontrado em PlayerUI/SavingGame.");
            enabled = false;
            return;
        }

        _group.alpha = 0f;

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnGameSaved += ShowPanel;
    }

    void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnGameSaved -= ShowPanel;
    }

    private void ShowPanel()
    {
        if (_group == null) return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _group.alpha   = 1f;
        _fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed      += Time.deltaTime;
            _group.alpha  = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        _group.alpha   = 0f;
        _fadeCoroutine = null;
    }
}
