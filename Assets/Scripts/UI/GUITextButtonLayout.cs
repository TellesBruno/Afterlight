using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Wraps long options instead of shrinking text or overflowing a scroll row.</summary>
[RequireComponent(typeof(RectTransform))]
public class GUITextButtonLayout : MonoBehaviour, ILayoutElement
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float minimumHeight = 56f;
    [SerializeField] private float horizontalPadding = 40f;
    [SerializeField] private float verticalPadding = 20f;
    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }
    public float minWidth => 0f;
    public float preferredWidth => -1f;
    public float flexibleWidth => 1f;
    public float minHeight => minimumHeight;
    public float preferredHeight => label == null ? minimumHeight : Mathf.Max(minimumHeight,
        label.GetPreferredValues(label.text, Mathf.Max(1f, ((RectTransform)transform).rect.width - horizontalPadding), Mathf.Infinity).y + verticalPadding);
    public float flexibleHeight => 0f;
    public int layoutPriority => 2;
}
