using System.Collections;
using UnityEngine;

/// <summary>
/// Exibe o elemento CollectedClue (filho de PlayerUI) com um fade-in/out suave
/// sempre que uma nova clue é adicionada ao inventário.
/// Mirrors the WeaponIconsHUD fade pattern.
///
/// Setup:
///   1. O GameObject PlayerUI/CollectedClue deve ter um CanvasGroup (alpha 0).
///   2. Adicione este script no mesmo objeto que PlayerHUD (ou no Player).
///   3. Arraste a referência de ClueInventory no Inspector.
/// </summary>
public class CollectedClueHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClueInventory clueInventory;

    [Header("Timing")]
    [Tooltip("Segundos que o elemento fica totalmente visível após coletar a clue.")]
    [SerializeField] private float visibleDuration = 3f;
    [Tooltip("Duração em segundos do fade de visível para invisível.")]
    [SerializeField] private float fadeDuration = 1f;

    private CanvasGroup _group;
    private Coroutine   _fadeCoroutine;

    void Start()
    {
        var root = GameObject.FindWithTag("PlayerUI");
        if (root != null)
        {
            var collectedClue = root.transform.Find("CollectedClue");
            if (collectedClue != null)
                _group = collectedClue.GetComponent<CanvasGroup>();
        }

        if (_group == null)
        {
            Debug.LogWarning("[CollectedClueHUD] CanvasGroup não encontrado em PlayerUI/CollectedClue.");
            enabled = false;
            return;
        }

        _group.alpha = 0f;

        if (clueInventory != null)
            clueInventory.OnClueAdded += HandleClueAdded;
    }

    void OnDestroy()
    {
        if (clueInventory != null)
            clueInventory.OnClueAdded -= HandleClueAdded;
    }

    private void HandleClueAdded(ClueData _) => ShowClue();

    private void ShowClue()
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
