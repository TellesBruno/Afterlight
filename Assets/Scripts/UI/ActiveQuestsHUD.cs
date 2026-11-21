using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows up to 3 active quests (filho de PlayerUI/ActiveQuests) when Tab
/// ("HighlightQuests") is pressed, with a fade-in/out identical to
/// WeaponIconsHUD/CollectedClueHUD. Primary quests fill the 3 slots first;
/// any remaining slots are filled with Secondary quests. The text refreshes
/// whenever a quest starts, completes or fails, even while hidden.
///
/// Setup:
///   1. O GameObject PlayerUI/ActiveQuests deve ter um CanvasGroup (alpha 0)
///      e, entre seus filhos, um TMP_Text para o conteúdo.
///   2. Adicione este script no mesmo objeto que PlayerHUD (ou no Player).
///   3. Arraste as referências de QuestManager e PlayerInputHandler no Inspector.
/// </summary>
public class ActiveQuestsHUD : MonoBehaviour
{
    private const int MaxShown = 3;

    [Header("References")]
    [SerializeField] private QuestManager questManager;
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Timing")]
    [Tooltip("Segundos que o painel fica totalmente visível após pressionar Tab.")]
    [SerializeField] private float visibleDuration = 5f;
    [Tooltip("Duração em segundos do fade de visível para invisível.")]
    [SerializeField] private float fadeDuration = 1f;

    private CanvasGroup _group;
    private TMP_Text _text;
    private Coroutine _fadeCoroutine;

    void Start()
    {
        var root = GameObject.FindWithTag("PlayerUI");
        if (root != null)
        {
            var activeQuests = root.transform.Find("ActiveQuests");
            if (activeQuests != null)
            {
                _group = activeQuests.GetComponent<CanvasGroup>();
                _text  = activeQuests.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (_group == null || _text == null)
        {
            Debug.LogWarning("[ActiveQuestsHUD] CanvasGroup ou TMP_Text não encontrados em PlayerUI/ActiveQuests.");
            enabled = false;
            return;
        }

        _group.alpha = 0f;

        if (questManager != null)
            questManager.OnQuestStateChanged += HandleQuestStateChanged;
        LocalizationService.LanguageChanged += RefreshText;

        RefreshText();
    }

    void OnDestroy()
    {
        if (questManager != null)
            questManager.OnQuestStateChanged -= HandleQuestStateChanged;
        LocalizationService.LanguageChanged -= RefreshText;
    }

    void Update()
    {
        if (inputHandler != null && inputHandler.GetHighlightQuestsDown())
            ShowPanel();
    }

    private void HandleQuestStateChanged(QuestData quest, QuestState state) => RefreshText();

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

    private void RefreshText()
    {
        if (_text == null || questManager == null) return;

        var primaries   = new List<QuestData>();
        var secondaries = new List<QuestData>();

        foreach (var quest in questManager.GetActiveQuests())
        {
            if (quest.questType == QuestType.Primary) primaries.Add(quest);
            else secondaries.Add(quest);
        }

        int primaryCount   = Mathf.Min(primaries.Count, MaxShown);
        int secondaryCount = Mathf.Min(secondaries.Count, MaxShown - primaryCount);

        var sb = new StringBuilder();

        if (primaryCount > 0)
        {
            sb.AppendLine(LocalizationService.Get("ui.primaryObjective"));
            for (int i = 0; i < primaryCount; i++)
                sb.AppendLine("\t" + LocalizationService.QuestTitle(primaries[i]));
        }

        if (secondaryCount > 0)
        {
            sb.AppendLine(LocalizationService.Get("ui.secondaryObjective"));
            for (int i = 0; i < secondaryCount; i++)
                sb.AppendLine("\t" + LocalizationService.QuestTitle(secondaries[i]));
        }

        _text.text = sb.ToString().TrimEnd();
    }
}
