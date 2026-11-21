using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Visual feedback only; leaves the Button's navigation and callbacks intact.</summary>
[RequireComponent(typeof(Button))]
public class GUIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image selectionMark;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color textColor = new Color32(228, 231, 233, 255);
    [SerializeField] private Color accentColor = new Color32(255, 115, 157, 255);
    [SerializeField] private Color normalColor = new Color32(32, 38, 43, 32);
    [SerializeField] private Color highlightedColor = new Color32(69, 43, 54, 255);

    private Button _button;
    private bool _hovered, _selected, _pressed, _interactable;

    public void Initialize(Image buttonBackground, Image buttonSelectionMark, TMP_Text buttonLabel)
    {
        background = buttonBackground;
        selectionMark = buttonSelectionMark;
        label = buttonLabel;
        if (_button == null) _button = GetComponent<Button>();
        Refresh();
    }

    private void Awake() => _button = GetComponent<Button>();
    private void OnEnable()
    {
        if (_button == null) _button = GetComponent<Button>();
        _hovered = _pressed = false;
        _selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        Refresh();
    }

    private void Update()
    {
        if (_interactable != _button.IsInteractable()) Refresh();
    }

    private void OnDisable() { _hovered = _selected = _pressed = false; Refresh(); }
    public void OnPointerEnter(PointerEventData e) { _hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData e) { _hovered = false; _pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) { _pressed = true; Refresh(); } }
    public void OnPointerUp(PointerEventData e) { _pressed = false; Refresh(); }
    public void OnSelect(BaseEventData e) { _selected = true; Refresh(); }
    public void OnDeselect(BaseEventData e) { _selected = false; _pressed = false; Refresh(); }

    private void Refresh()
    {
        if (_button == null) return;
        _interactable = _button.IsInteractable();
        bool highlighted = _interactable && (_hovered || _selected);
        if (background != null)
            background.color = highlighted ? Color.Lerp(highlightedColor, accentColor, _pressed ? 0.2f : 0f) : normalColor;
        if (label != null)
            label.color = !_interactable ? new Color(textColor.r, textColor.g, textColor.b, 0.4f) : highlighted ? accentColor : textColor;
        if (selectionMark != null)
            selectionMark.color = highlighted ? accentColor : Color.clear;
    }
}
