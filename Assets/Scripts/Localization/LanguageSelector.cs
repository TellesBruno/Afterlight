using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LanguageSelector : MonoBehaviour
{
    private static readonly Color PanelColor = new Color32(27, 34, 39, 250);
    private static readonly Color TextColor = new Color32(228, 231, 233, 255);
    private static readonly Color ButtonColor = new Color32(32, 38, 43, 32);

    private GameObject _modal;
    private Button _openButton;
    private Button _englishButton;
    private Button _portugueseButton;
    private Button _backButton;
    private TMP_Text _openLabel;
    private TMP_Text _heading;
    private TMP_Text _backLabel;
    private bool _isMainMenu;

    public static void CreateForScene(Scene scene)
    {
        if (scene.name != "MainMenu" && scene.name != "SampleScene") return;

        Transform parent = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (scene.name == "SampleScene")
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == "PauseMenu") { parent = child; break; }
            }
            else
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null) parent = canvas.transform;
            }
            if (parent != null) break;
        }
        if (parent == null || parent.Find("LanguageSelector") != null) return;

        var referenceLabel = parent.GetComponentInChildren<Button>(true)?.GetComponentInChildren<TMP_Text>(true);
        var opener = new GameObject("LanguageSelector", typeof(RectTransform), typeof(LanguageSelector));
        var openerRect = opener.GetComponent<RectTransform>();
        openerRect.SetParent(parent, false);
        if (scene.name == "MainMenu")
        {
            openerRect.anchorMin = openerRect.anchorMax = openerRect.pivot = new Vector2(1f, 0f);
            openerRect.anchoredPosition = new Vector2(-32f, 28f);
            openerRect.sizeDelta = new Vector2(360f, 60f);
        }
        else
        {
            PlaceInPauseMenu(parent, openerRect);
        }

        var selector = opener.GetComponent<LanguageSelector>();
        selector._isMainMenu = scene.name == "MainMenu";
        selector._openButton = MakeButton(openerRect, "OpenLanguageModal", referenceLabel,
            new Vector2(360f, 60f), Vector2.zero, "Language", selector.OpenModal, out selector._openLabel);
        if (!selector._isMainMenu)
        {
            var buttonRect = (RectTransform)selector._openButton.transform;
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
        }

        var modalParent = parent.GetComponentInParent<Canvas>()?.transform ?? parent;
        selector.BuildModal(modalParent, referenceLabel);
        selector.Refresh();
    }

    private static void PlaceInPauseMenu(Transform parent, RectTransform openerRect)
    {
        var mainMenu = parent.Find("MainMenu") as RectTransform;
        var tutorial = parent.Find("Tutorial") as RectTransform;
        var quit = parent.Find("QuitButtom") as RectTransform;
        if (tutorial == null) return;

        float step = mainMenu != null ? mainMenu.anchorMin.y - tutorial.anchorMin.y : 0.125f;
        openerRect.anchorMin = tutorial.anchorMin - new Vector2(0f, step);
        openerRect.anchorMax = tutorial.anchorMax - new Vector2(0f, step);
        openerRect.anchoredPosition = tutorial.anchoredPosition;
        openerRect.sizeDelta = tutorial.sizeDelta;
        openerRect.pivot = tutorial.pivot;
        openerRect.SetSiblingIndex(tutorial.GetSiblingIndex() + 1);

        if (quit != null)
        {
            quit.anchorMin = tutorial.anchorMin - new Vector2(0f, step * 2f);
            quit.anchorMax = tutorial.anchorMax - new Vector2(0f, step * 2f);
        }
    }

    public bool TryCloseModal()
    {
        if (_modal == null || !_modal.activeSelf) return false;
        CloseModal();
        return true;
    }

    private void BuildModal(Transform parent, TMP_Text referenceLabel)
    {
        _modal = new GameObject("LanguageModal", typeof(RectTransform), typeof(Image));
        var backdrop = _modal.GetComponent<RectTransform>();
        backdrop.SetParent(parent, false);
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        _modal.GetComponent<Image>().color = new Color32(0, 0, 0, 196);

        var card = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.SetParent(backdrop, false);
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(520f, 310f);
        card.GetComponent<Image>().color = PanelColor;

        _heading = MakeText(cardRect, "Heading", referenceLabel, 30f, TextAlignmentOptions.MidlineLeft);
        var headingRect = _heading.rectTransform;
        headingRect.anchorMin = headingRect.anchorMax = headingRect.pivot = new Vector2(0.5f, 1f);
        headingRect.anchoredPosition = new Vector2(0f, -20f);
        headingRect.sizeDelta = new Vector2(456f, 50f);

        _englishButton = MakeButton(cardRect, "English", referenceLabel,
            new Vector2(440f, 58f), new Vector2(0f, 40f), "English",
            () => ChooseLanguage("en"), out _);
        _portugueseButton = MakeButton(cardRect, "PortugueseBrazil", referenceLabel,
            new Vector2(440f, 58f), new Vector2(0f, -35f), "Português (Brasil)",
            () => ChooseLanguage("pt-BR"), out _);
        _backButton = MakeButton(cardRect, "Back", referenceLabel,
            new Vector2(440f, 58f), new Vector2(0f, -110f), "Back",
            CloseModal, out _backLabel);

        SetNavigation(_englishButton, _backButton, _portugueseButton);
        SetNavigation(_portugueseButton, _englishButton, _backButton);
        SetNavigation(_backButton, _portugueseButton, _englishButton);
        _modal.SetActive(false);
    }

    private static Button MakeButton(Transform parent, string name, TMP_Text referenceLabel,
        Vector2 size, Vector2 position, string label, UnityEngine.Events.UnityAction onClick, out TMP_Text text)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = obj.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = ButtonColor;
        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(onClick);

        text = MakeText(rect, "Label", referenceLabel, 26f, TextAlignmentOptions.MidlineLeft);
        text.text = label;
        var labelRect = text.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(22f, 10f);
        labelRect.offsetMax = new Vector2(-18f, -10f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 26f;

        var mark = new GameObject("SelectionMark", typeof(RectTransform), typeof(Image));
        var markRect = mark.GetComponent<RectTransform>();
        markRect.SetParent(rect, false);
        markRect.anchorMin = Vector2.zero;
        markRect.anchorMax = Vector2.up;
        markRect.offsetMin = new Vector2(0f, 6f);
        markRect.offsetMax = new Vector2(3f, -6f);
        var markImage = mark.GetComponent<Image>();
        markImage.color = Color.clear;
        markImage.raycastTarget = false;

        obj.AddComponent<GUIButtonFeedback>().Initialize(image, markImage, text);
        return button;
    }

    private static TMP_Text MakeText(Transform parent, string name, TMP_Text referenceLabel,
        float fontSize, TextAlignmentOptions alignment)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = referenceLabel != null ? referenceLabel.font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Normal;
        text.color = TextColor;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void SetNavigation(Button button, Selectable up, Selectable down)
    {
        var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
        button.navigation = navigation;
    }

    private void OpenModal()
    {
        _modal.SetActive(true);
        _modal.transform.SetAsLastSibling();
        EventSystem.current?.SetSelectedGameObject(
            LocalizationService.Language == "pt-BR" ? _portugueseButton.gameObject : _englishButton.gameObject);
    }

    private void ChooseLanguage(string language)
    {
        LocalizationService.SetLanguage(language);
        CloseModal();
    }

    private void CloseModal()
    {
        _modal.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(_openButton.gameObject);
    }

    private void OnEnable() { LocalizationService.LanguageChanged += Refresh; Refresh(); }
    private void OnDisable()
    {
        LocalizationService.LanguageChanged -= Refresh;
        if (_modal != null) _modal.SetActive(false);
    }

    private void Update()
    {
        if (_isMainMenu && _modal != null && _modal.activeSelf &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseModal();
    }

    private void Refresh()
    {
        if (_openLabel != null) _openLabel.text = LocalizationService.Get("ui.languageButton", "Language");
        if (_heading != null) _heading.text = LocalizationService.Get("ui.language", "Select Language");
        if (_backLabel != null) _backLabel.text = LocalizationService.Get("ui.backButton", "Back");
    }
}
