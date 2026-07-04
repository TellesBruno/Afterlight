using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component placed on each button prefab item in the quest list.
/// Holds a reference to the quest it represents and wires the click callback.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuestButtonItem : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private QuestData _questData;
    private QuestState _state;
    private Action<QuestData, QuestState> _onClick;

    /// <summary>
    /// Initialises this button item.
    /// </summary>
    /// <param name="quest">The quest this button represents.</param>
    /// <param name="state">The quest's current state.</param>
    /// <param name="onClick">Callback when clicked. Args: (QuestData, current state).</param>
    public void Setup(QuestData quest, QuestState state, Action<QuestData, QuestState> onClick)
    {
        _questData = quest;
        _state     = state;
        _onClick   = onClick;

        RefreshLabel();

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /// <summary>Updates the displayed state without re-instantiating.</summary>
    public void SetState(QuestState state)
    {
        _state = state;
        RefreshLabel();
    }

    public string QuestId => _questData != null ? _questData.id : string.Empty;

    private void RefreshLabel()
    {
        if (label == null) return;
        label.text = _state == QuestState.NotStarted ? "???" : $"{_questData.title} ({_state})";
    }

    private void OnClick() => _onClick?.Invoke(_questData, _state);
}
