using TMPro;
using UnityEngine;

public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    private TMP_Text _text;

    public void Initialize(TMP_Text text, string localizationKey)
    {
        _text = text;
        key = localizationKey;
        Refresh();
    }

    private void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        LocalizationService.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() { LocalizationService.LanguageChanged -= Refresh; }

    private void Refresh()
    {
        if (_text != null && !string.IsNullOrEmpty(key))
            _text.text = LocalizationService.Get(key);
    }
}
