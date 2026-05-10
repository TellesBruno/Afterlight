using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component placed on each button prefab item in the dialog options list.
/// Mirrors ClueButtonItem.cs.
/// </summary>
[RequireComponent(typeof(Button))]
public class DialogOptionButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private DialogOption _option;
    private Action<DialogOption> _onClick;

    /// <summary>Initialises this button with a dialog option and click callback.</summary>
    public void Setup(DialogOption option, Action<DialogOption> onClick)
    {
        _option  = option;
        _onClick = onClick;

        if (label != null)
            label.text = option.optionText;

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick() => _onClick?.Invoke(_option);
}
