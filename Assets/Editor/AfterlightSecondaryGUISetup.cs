using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Applies the SampleScene typography and button feedback to the other menu scenes.</summary>
public static class AfterlightSecondaryGUISetup
{
    private static readonly Color Normal = new Color32(32, 38, 43, 32);
    private static readonly Color TextColor = new Color32(228, 231, 233, 255);

    [MenuItem("Afterlight/GUI/Style other menu scenes")]
    public static void Apply()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("The SampleScene TMP font is missing.");
        Directory.CreateDirectory("Temp/AfterlightGUI");
        foreach (var name in new[] { "MainMenu", "Intro", "GameOver" })
        {
            var path = "Assets/Scenes/" + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Single();
            var buttons = canvas.GetComponentsInChildren<Button>(true);
            var callbacks = buttons.Select(ButtonCallbacks).ToArray();
            var backup = "Temp/AfterlightGUI/" + name + "-before-style.unity";
            if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Scene backup failed: " + name);
            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Style " + name + " GUI");
            foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) != null) continue;
                StyleText(label, font, name == "GameOver" && label.text == "Level Complete!" ? 48 : 28,
                    name == "GameOver" && label.text == "Level Complete!" ? TextAlignmentOptions.Center : label.alignment);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
            foreach (var button in buttons) StyleButton(button, font);
            if (!buttons.Select(ButtonCallbacks).SequenceEqual(callbacks)) throw new InvalidOperationException(name + ": a button callback changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + name);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/AfterlightGUI/secondary-applied.txt", "Styled MainMenu, Intro and GameOver. Original button callbacks preserved. " + DateTime.Now);
    }

    private static void StyleButton(Button button, TMP_FontAsset font)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        var image = button.GetComponent<Image>();
        if (label == null || image == null) throw new InvalidOperationException("Button needs an Image and TMP label: " + button.name);
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Normal;
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        StyleText(label, font, 26, TextAlignmentOptions.MidlineLeft);
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(22, 10);
        rect.offsetMax = new Vector2(-18, -10);
        var mark = button.transform.Find("SelectionMark");
        if (mark == null)
        {
            var child = new GameObject("SelectionMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(button.transform, false);
            mark = child.transform;
        }
        var markRect = (RectTransform)mark;
        markRect.anchorMin = new Vector2(0, 0);
        markRect.anchorMax = new Vector2(0, 1);
        markRect.offsetMin = new Vector2(0, 6);
        markRect.offsetMax = new Vector2(3, -6);
        var markImage = mark.GetComponent<Image>();
        markImage.color = Color.clear;
        markImage.raycastTarget = false;
        var feedback = button.GetComponent<GUIButtonFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<GUIButtonFeedback>();
        var serialized = new SerializedObject(feedback);
        serialized.FindProperty("background").objectReferenceValue = image;
        serialized.FindProperty("selectionMark").objectReferenceValue = markImage;
        serialized.FindProperty("label").objectReferenceValue = label;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (var component in new UnityEngine.Object[] { button, image, label, rect, feedback })
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
    {
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Normal;
        text.color = TextColor;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
    }

    private static string ButtonCallbacks(Button button)
    {
        var events = button.onClick;
        return button.name + ":" + string.Join("|", Enumerable.Range(0, events.GetPersistentEventCount())
            .Select(i => events.GetPersistentTarget(i)?.GetInstanceID() + "/" + events.GetPersistentMethodName(i)));
    }
}
