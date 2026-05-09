using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component placed on each button prefab item in the clue list.
/// Holds a reference to the clue it represents and wires the click callback.
/// </summary>
[RequireComponent(typeof(Button))]
public class ClueButtonItem : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private ClueData _clueData;
    private bool _isCollected;
    private Action<ClueData, bool> _onClick;

    /// <summary>
    /// Initialises this button item.
    /// </summary>
    /// <param name="clue">The clue this button represents.</param>
    /// <param name="collected">Whether the clue has been collected.</param>
    /// <param name="onClick">Callback when clicked. Args: (ClueData, isCollected).</param>
    public void Setup(ClueData clue, bool collected, Action<ClueData, bool> onClick)
    {
        _clueData    = clue;
        _isCollected = collected;
        _onClick     = onClick;

        RefreshLabel();

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /// <summary>Marks this clue as collected and updates the label without re-instantiating.</summary>
    public void MarkCollected()
    {
        _isCollected = true;
        RefreshLabel();
    }

    public string ClueId => _clueData != null ? _clueData.id : string.Empty;

    private void RefreshLabel()
    {
        if (label == null) return;
        label.text = _isCollected ? _clueData.title : "???";
    }

    private void OnClick() => _onClick?.Invoke(_clueData, _isCollected);
}
