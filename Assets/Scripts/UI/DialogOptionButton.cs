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
    private string _localizationKey;

    /// <summary>Initialises this button with a dialog option and click callback.</summary>
    public void Setup(DialogOption option, string localizationKey, Action<DialogOption> onClick)
    {
        _option  = option;
        _onClick = onClick;
        _localizationKey = localizationKey;

        RefreshLabel();

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    private void OnEnable() { LocalizationService.LanguageChanged += RefreshLabel; }
    private void OnDisable() { LocalizationService.LanguageChanged -= RefreshLabel; }

    private void RefreshLabel()
    {
        if (label != null && _option != null)
            label.text = LocalizationService.Get(_localizationKey, _option.optionText);
    }

    private void OnClick() => _onClick?.Invoke(_option);
}
