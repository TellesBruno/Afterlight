using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Bakes the approved GUI into the scene; no runtime rebuilding of the Canvas.</summary>
public static class AfterlightGUISetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Work = "Temp/AfterlightGUI";
    private static readonly Color Panel = new Color32(27, 34, 39, 250);
    private static readonly Color Text = new Color32(228, 231, 233, 255);
    private static readonly Color Muted = new Color32(173, 181, 188, 255);
    private static readonly Color Line = new Color32(60, 68, 75, 255);
    private static TMP_FontAsset font;

    [MenuItem("Afterlight/GUI/Apply approved layout")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply the GUI outside Play mode.");
        Directory.CreateDirectory(Work);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("LiberationSans SDF is missing.");

        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "Canvas");
        var playerHUD = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerHUD>(true)).First();
        var quests = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<QuestManager>(true)).First();
        string backup = Work + "/BeforeGUI-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up the scene.");
        var callbacks = CaptureCallbacks(canvas);
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply Afterlight GUI");
        Undo.RecordObject(playerHUD, "Connect health bar");

        foreach (string path in new[] { "Assets/Prefabs/Clue Button.prefab", "Assets/Prefabs/Quest Buttom.prefab", "Assets/Prefabs/Dialog Option Button.prefab" })
        {
            var prefabBackup=Work + "/" + Path.GetFileName(path) + ".before";
            if(!File.Exists(prefabBackup)) File.Copy(path, prefabBackup);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                StyleButton(prefab.GetComponent<Button>(), true);
                RecordPrefabOverrides(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true)) StyleText(text, 28);
        foreach (var button in canvas.GetComponentsInChildren<Button>(true)) StyleButton(button, false);

        StyleHUD(canvas.transform.Find("PlayerUi"), playerHUD);
        StyleDossier(canvas.transform.Find("Clue Menu"), "Clue");
        StyleDossier(canvas.transform.Find("Quest Menu"), "Quest");
        StyleDialog(canvas.transform.Find("Dialog Screen"));
        StylePause(canvas.transform.Find("PauseMenu"), quests);
        foreach (string name in new[] { "DeathScreen", "CivilianKillScreen", "FinishLevel" }) StyleModal(canvas.transform.Find(name));
        StyleTutorial(canvas.transform.Find("Tutorial"));

        RecordPrefabOverrides(canvas.gameObject);
        foreach (var component in canvas.GetComponentsInChildren<Component>(true)) if (component != null) EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(playerHUD);
        if (CaptureCallbacks(canvas) != callbacks) throw new InvalidOperationException("A pre-existing Button callback changed.");
        EditorSceneManager.MarkSceneDirty(scene);
        Validate(canvas, playerHUD);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save SampleScene.");
        AssetDatabase.SaveAssets();
        File.WriteAllText(Work + "/applied.txt", "Applied approved GUI at " + DateTime.Now + "\nScene backup: " + backup +
            "\nButton callbacks preserved. Health reference and lower-half dialog anchors validated.\n");
        Debug.Log("[Afterlight GUI] Layout saved in SampleScene. Backup: " + backup);
        AfterlightGUIReview.Run(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
    }

    private static RectTransform Rect(Transform t)
    {
        if (t == null) throw new InvalidOperationException("Expected UI object not found.");
        var go = t.gameObject;
        var rect = go.GetComponent<RectTransform>();
        return rect != null ? rect : go.AddComponent<RectTransform>();
    }

    private static void Bounds(Transform t, float x0, float y0, float x1, float y1, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        var r = Rect(t);
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
        r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1); r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
        var p = r.localPosition; p.z = 0; r.localPosition = p;
    }

    private static void Box(Transform t, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var r = Rect(t); r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
        r.anchorMin = r.anchorMax = anchor; r.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        r.sizeDelta = size; r.anchoredPosition3D = new Vector3(position.x, position.y, 0);
    }

    private static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null) return Rect(found);
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create GUI element");
        return (RectTransform)go.transform;
    }

    private static T Get<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    private static Image Paint(Transform target, Color color, bool raycast = false)
    {
        var image = Get<Image>(target.gameObject); image.sprite = null; image.type = Image.Type.Simple;
        image.color = color; image.raycastTarget = raycast; return image;
    }

    private static void StyleText(TMP_Text t, float size, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        t.font = font; t.fontSharedMaterial = font.material; t.fontSize = size;
        t.enableAutoSizing = false; t.fontStyle = FontStyles.Normal;
        t.color = color ?? Text; t.alignment = alignment;
        t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
        t.margin = Vector4.zero; t.raycastTarget = false; t.rectTransform.localScale = Vector3.one;
    }

    private static TMP_Text Label(Transform parent, string name, string content, float size, Color? color = null)
    {
        var t = Get<TextMeshProUGUI>(Child(parent, name).gameObject);
        StyleText(t, size, color); t.text = content; return t;
    }

    private static void Reference(Object component, string field, Object value)
    {
        var serialized = new SerializedObject(component);
        var property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(component.GetType().Name + "." + field + " not found.");
        property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleButton(Button button, bool wrap)
    {
        if (button == null) throw new InvalidOperationException("Button prefab is missing its Button.");
        var label = button.GetComponentInChildren<TMP_Text>(true);
        var image = Paint(button.transform, new Color32(32, 38, 43, 32), true);
        button.targetGraphic = image; button.transition = Selectable.Transition.None;
        // Decorative children of older cyberpunk prefabs must not tint the flat button.
        foreach (var other in button.GetComponentsInChildren<Image>(true))
            if (other != image && other.name != "SelectionMark") { other.color = Color.clear; other.raycastTarget = false; }
        StyleText(label, 26, Text, TextAlignmentOptions.MidlineLeft);
        Bounds(label.transform, 0, 0, 1, 1, 22, 10, 18, 10);
        var mark = Child(button.transform, "SelectionMark"); Bounds(mark, 0, 0, 0, 1, 0, 6, -3, 6);
        var feedback = Get<GUIButtonFeedback>(button.gameObject);
        Reference(feedback, "background", image); Reference(feedback, "selectionMark", Paint(mark, Color.clear)); Reference(feedback, "label", label);
        if (wrap)
        {
            Box(button.transform, new Vector2(0, 1), Vector2.zero, new Vector2(600, 60), new Vector2(0, 1));
            var layout = Get<GUITextButtonLayout>(button.gameObject); Reference(layout, "label", label);
        }
    }

    private static void StyleHUD(Transform hud, PlayerHUD player)
    {
        Bounds(hud, 0, 0, 1, 1);
        foreach (var img in hud.GetComponentsInChildren<Image>(true))
        {
            img.raycastTarget = false;
            if (img.name == "Panel") img.color = Color.clear;
        }
        var life = hud.Find("LifeStatus"); Box(life, Vector2.zero, new Vector2(56, 44), new Vector2(260, 94), Vector2.zero);
        var value = life.Find("LifeCount"); Bounds(value, 0, 0, 1, 1, 0, 17, 0, 28); StyleText(value.GetComponent<TMP_Text>(), 36);
        var lifeLabel = Label(life, "HealthLabel", "Health", 21, Muted); Bounds(lifeLabel.transform, 0, 1, 1, 1, 0, -28, 0, 0);
        var track = Child(life, "HealthBar"); Bounds(track, 0, 0, 1, 0, 0, 0, 0, -6); Paint(track, Line);
        var fill = Child(track, "Fill"); Bounds(fill, 0, 0, 1, 1);
        var fillImage = Paint(fill, new Color32(139, 220, 202, 255));
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Filled; fillImage.fillMethod = Image.FillMethod.Horizontal; fillImage.fillOrigin = 0; fillImage.fillAmount = 1;
        Reference(player, "healthFill", fillImage);
        var ammo = hud.Find("AmmoStatus"); Box(ammo, new Vector2(1, 0), new Vector2(-56, 44), new Vector2(240, 94), new Vector2(1, 0));
        var ammoLabel = Label(ammo, "AmmoLabel", "Ammo", 21, Muted); Bounds(ammoLabel.transform, 0, 1, 1, 1, 0, -28, 0, 0); ammoLabel.alignment = TextAlignmentOptions.TopRight;
        Bounds(ammo.Find("CurrentAmmo"), 0, 0, 0.5f, 1, 0, 15, 8, 28); StyleText(ammo.Find("CurrentAmmo").GetComponent<TMP_Text>(), 36, Text, TextAlignmentOptions.TopRight);
        Bounds(ammo.Find("Splitter"), 0.5f, 0, 0.64f, 1, 0, 15, 0, 31); StyleText(ammo.Find("Splitter").GetComponent<TMP_Text>(), 28, Muted, TextAlignmentOptions.Top);
        Bounds(ammo.Find("TotalAmmo"), 0.64f, 0, 1, 1, 0, 15, 0, 31); StyleText(ammo.Find("TotalAmmo").GetComponent<TMP_Text>(), 28, Muted, TextAlignmentOptions.TopRight);
        Box(hud.Find("Crosshair"), new Vector2(.5f,.5f), Vector2.zero, new Vector2(5,5));
        foreach (string name in new[] { "Interaction", "FinishLevelInteraction", "DoorInteraction" })
        {
            var tr = hud.Find(name); Box(tr, new Vector2(.5f,.5f), new Vector2(0,-85), new Vector2(50,50));
            foreach (var img in tr.GetComponentsInChildren<Image>(true))
            {
                if (img.name == "Panel") continue;
                Bounds(img.transform, 0, 0, 1, 1); img.preserveAspect = true;
            }
            if (name == "DoorInteraction" && tr.Find("Lockpick") != null) Bounds(tr.Find("Lockpick"), 0, 0, 1, 1);
        }
        var collected = hud.Find("CollectedClue"); Box(collected, Vector2.zero, new Vector2(56,165), new Vector2(420,40), Vector2.zero);
        var collectedText = collected.GetComponentInChildren<TMP_Text>(true); Bounds(collectedText.transform,0,0,1,1); StyleText(collectedText,24);
        var saving = hud.Find("SavingGame"); Box(saving, new Vector2(1,0),new Vector2(-56,210),new Vector2(36,36),new Vector2(1,0));
        Bounds(saving.Find("Image"),0,0,1,1);
        var objectives = hud.Find("ActiveQuests"); Box(objectives,new Vector2(1,0),new Vector2(-56,240),new Vector2(450,220),new Vector2(1,0));
        var objectiveText = objectives.GetComponentInChildren<TMP_Text>(true); Bounds(objectiveText.transform,0,0,1,1); StyleText(objectiveText,24);
        var weapons = hud.Find("WeaponIcons"); Box(weapons,new Vector2(1,0),new Vector2(-56,160),new Vector2(340,65),new Vector2(1,0));
        int slotIndex = 0;
        foreach (Transform slot in weapons)
        {
            var slotObject=slot.gameObject;
            Box(slot,Vector2.zero,new Vector2(slotIndex++*84,0),new Vector2(72,65),Vector2.zero);
            foreach (var img in slotObject.GetComponentsInChildren<Image>(true))
            {
                Bounds(img.transform,0,0,1,1,5,5,5,5); img.preserveAspect=true;
                if (img.name != "Icon") img.color = new Color(1,1,1,0.15f);
            }
        }
    }

    private static void StyleDossier(Transform root, string kind)
    {
        Bounds(root,.08f,.1f,.92f,.9f);
        var panel=root.Find("Panel"); Bounds(panel,0,0,1,1); Paint(panel,Panel,true); panel.SetAsFirstSibling();
        var title=root.Find("Title"); Bounds(title,0,.88f,1,1,40,0,40,22); StyleText(title.GetComponent<TMP_Text>(),38);
        var list=root.Find(kind+" List"); Bounds(list,0,0,.34f,.86f,28,30,16,0);
        var info=root.Find(kind+" Info"); Bounds(info,.34f,0,1,.86f,26,30,36,0);
        var detailTitle=info.Find(kind+" Title"); Bounds(detailTitle,0,.88f,1,1,0,0,0,0); StyleText(detailTitle.GetComponent<TMP_Text>(),32);
        detailTitle.GetComponent<TMP_Text>().text=string.Empty;
        var detailScroll=info.Find(kind+" Text").GetComponent<ScrollRect>(); Bounds(detailScroll.transform,0,0,1,.86f);
        var listScroll=list.GetComponentInChildren<ScrollRect>(true); Bounds(listScroll.transform,0,0,1,1); SetupScroll(listScroll,true);
        SetupScroll(detailScroll,false);
        var description=detailScroll.content.GetComponentInChildren<TMP_Text>(true); StyleText(description,28); description.text=string.Empty;
        Bounds(description.transform,0,0,1,1);
        var group=Get<VerticalLayoutGroup>(detailScroll.content.gameObject); group.padding=new RectOffset(0,14,0,0); group.childControlHeight=true; group.childControlWidth=true; group.childForceExpandHeight=false;
        var divider=Child(root,"Divider"); Bounds(divider,.34f,0,.34f,.86f,0,30,-1,0); Paint(divider,Line);
        var back=Label(root,"CloseHint","Esc  Back",21,Muted); Bounds(back.transform,.75f,.9f,1,1,0,0,35,25); back.alignment=TextAlignmentOptions.TopRight;
    }

    private static void SetupScroll(ScrollRect scroll, bool list)
    {
        scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=32;
        if (scroll.horizontalScrollbar!=null) { scroll.horizontalScrollbar.gameObject.SetActive(false); scroll.horizontalScrollbar=null; }
        if (scroll.verticalScrollbar!=null)
        {
            var bar=scroll.verticalScrollbar;
            Bounds(bar.transform,1,0,1,1,-7,0,0,0);
            foreach(var img in bar.GetComponentsInChildren<Image>(true)) { img.sprite=null; img.type=Image.Type.Simple; img.color=img==bar.targetGraphic?new Color32(110,120,128,200):new Color(0,0,0,0.05f); }
            scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        }
        Bounds(scroll.viewport,0,0,1,1,0,0,14,0);
        var mask=scroll.viewport.GetComponent<Mask>(); if(mask!=null) mask.showMaskGraphic=false;
        var viewportImage=scroll.viewport.GetComponent<Image>(); if(viewportImage!=null) { viewportImage.sprite=null; viewportImage.type=Image.Type.Simple; }
        var background=scroll.GetComponent<Image>(); if(background!=null) { background.sprite=null; background.color=Color.clear; }
        var content=scroll.content; Bounds(content,0,1,1,1); content.pivot=new Vector2(0,1); content.anchoredPosition=Vector2.zero;
        var fitter=Get<ContentSizeFitter>(content.gameObject); fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        if(list)
        {
            var group=Get<VerticalLayoutGroup>(content.gameObject); group.padding=new RectOffset(0,0,0,0); group.spacing=5;
            group.childAlignment=TextAnchor.UpperLeft; group.childControlWidth=group.childControlHeight=true;
            group.childForceExpandWidth=true; group.childForceExpandHeight=false;
        }
        scroll.verticalNormalizedPosition=1;
    }

    private static void StyleDialog(Transform root)
    {
        Bounds(root,0,0,1,.5f);
        var backdrop=Child(root,"DialogBackground"); Bounds(backdrop,0,0,1,1); Paint(backdrop,Panel,true); backdrop.SetAsFirstSibling();
        var response=root.Find("Dialog Response"); Bounds(response,.06f,.59f,.94f,1,0,8,0,22);
        var responsePanel=response.Find("Panel"); Bounds(responsePanel,0,0,1,1); Paint(responsePanel,Color.clear);
        var text=(TMP_Text)new SerializedObject(root.GetComponent<DialogUI>()).FindProperty("responseText").objectReferenceValue;
        StyleText(text,28); text.text=string.Empty;
        var view=Child(response,"ResponseViewport"); Bounds(view,0,0,1,1); Paint(view,Color.white,true); Get<Mask>(view.gameObject).showMaskGraphic=false;
        text.transform.SetParent(view,false); Bounds(text.transform,0,1,1,1); text.rectTransform.pivot=new Vector2(.5f,1);
        var fitter=Get<ContentSizeFitter>(text.gameObject); fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=Get<ScrollRect>(response.gameObject); scroll.viewport=view; scroll.content=text.rectTransform;
        scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=32;
        Reference(root.GetComponent<DialogUI>(),"responseScroll",scroll);
        var options=root.Find("Dialog Options"); Bounds(options,.06f,0,.94f,.59f,0,22,0,0);
        var panel=options.Find("Panel"); Bounds(panel,0,0,1,1); Paint(panel,Color.clear);
        var list=options.GetComponentInChildren<ScrollRect>(true); Bounds(list.transform,0,0,1,1); SetupScroll(list,true);
        var divider=Child(root,"Divider"); Bounds(divider,.06f,.59f,.94f,.59f,0,0,0,-1); Paint(divider,Line);
        // Response TMP reference is preserved even though it now lives inside a viewport.
    }

    private static void StylePause(Transform root, QuestManager quests)
    {
        var go=root.gameObject; Bounds(root,.2f,.17f,.8f,.83f); root=go.transform;
        var panel=root.Find("Panel"); Bounds(panel,0,0,1,1); Paint(panel,Panel,true); panel.SetAsFirstSibling();
        var title=panel.GetComponentInChildren<TMP_Text>(true); Bounds(title.transform,0,.8f,.45f,1,40,0,0,35); StyleText(title,38);
        var buttons=root.GetComponentsInChildren<Button>(true);
        for(int i=0;i<buttons.Length;i++) Bounds(buttons[i].transform,0,.64f-i*.125f,.45f,.75f-i*.125f,28,0,24,0);
        var info=Child(root,"PauseInfo"); Bounds(info,.51f,.08f,1,.94f,0,0,35,0);
        var line=Child(root,"InfoDivider"); Bounds(line,.48f,.08f,.48f,.94f,0,0,-1,0); Paint(line,Line);
        var caption=Label(info,"ObjectiveLabel","Current objective",23,Muted); Bounds(caption.transform,0,.9f,1,1);
        var objective=Label(info,"CurrentObjective","No active objectives.",28); Bounds(objective.transform,0,.64f,1,.9f);
        var heading=Label(info,"ControlsLabel","Basic controls",23,Muted); Bounds(heading.transform,0,.5f,1,.61f);
        var controls=Label(info,"Controls","W A S D    Move\nE    Interact\nF    Lockpick\nTab    Highlight objectives\nI    Clues\nJ    Objectives",24);
        Bounds(controls.transform,0,0,1,.5f);
        var close=Label(root,"CloseHint","Esc  Resume",23,Muted); Bounds(close.transform,0,0,.45f,.14f,40,0,0,0);
        var controller=Get<PauseMenuHUD>(go); Reference(controller,"questManager",quests); Reference(controller,"objectiveText",objective);
    }

    private static void StyleModal(Transform root)
    {
        var go=root.gameObject; Bounds(root,.27f,.24f,.73f,.76f); root=go.transform;
        var panel=root.Find("Panel"); Bounds(panel,0,0,1,1); Paint(panel,Panel,true); panel.SetAsFirstSibling();
        foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))
            if(t.GetComponentInParent<Button>(true)==null) { Bounds(t.transform,0,.67f,1,1,35,0,35,35); StyleText(t,36); }
        var buttons=root.GetComponentsInChildren<Button>(true);
        for(int i=0;i<buttons.Length;i++) Bounds(buttons[i].transform,0,.43f-i*.18f,1,.58f-i*.18f,30,0,30,0);
    }

    private static void StyleTutorial(Transform root)
    {
        Bounds(root,.06f,.05f,.94f,.95f);
        var panel=root.Find("Panel"); Bounds(panel,0,0,1,1); Paint(panel,Panel,true);
        var title=Label(panel,"TutorialHeading","How to play",38); Bounds(title.transform,0,.9f,1,1,36,0,36,24);
        foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.transform.parent!=panel || text==title) continue;
            StyleText(text,26);
            if(text.text.Contains("Movement")) Bounds(text.transform,0,.13f,.29f,.86f,36,0,20,0);
            else Bounds(text.transform,.68f,.13f,1,.86f,15,0,36,0);
        }
        var icons=panel.Find("Icons"); Bounds(icons,.3f,.13f,.67f,.86f,0,0,0,0);
        var names=new[]{"ClosedDoor","OpennedDoor","PickLock","ClueNerby","ExitBlocked","MoveNextArea","SavingGame"};
        for(int i=0;i<names.Length;i++)
        {
            var icon=icons.Find(names[i]);
            Box(icon,new Vector2(0,1f-(i+.5f)/names.Length),Vector2.zero,new Vector2(54,54),new Vector2(0,.5f));
            var image=icon.GetComponent<Image>(); if(image!=null) {image.preserveAspect=true;image.raycastTarget=false;}
            var label=icon.GetComponentInChildren<TMP_Text>(true); StyleText(label,24,Text,TextAlignmentOptions.MidlineLeft);
            Box(label.transform,new Vector2(1,.5f),new Vector2(18,0),new Vector2(Mathf.Max(180,Rect(icons).rect.width-76),80),new Vector2(0,.5f));
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true)) Bounds(button.transform,.68f,.03f,1,.1f,15,0,36,0);
    }

    private static string CaptureCallbacks(Canvas canvas)
    {
        return string.Join("\n",canvas.GetComponentsInChildren<Button>(true).OrderBy(b=>b.GetEntityId()).Select(b=>
            b.GetEntityId()+":"+string.Join(";",Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Select(i=>
                (b.onClick.GetPersistentTarget(i)!=null?b.onClick.GetPersistentTarget(i).GetEntityId().ToString():"null")+"/"+b.onClick.GetPersistentMethodName(i)+"/"+b.onClick.GetPersistentListenerState(i)))));
    }

    private static void RecordPrefabOverrides(GameObject root)
    {
        foreach(var c in root.GetComponentsInChildren<Component>(true))
            if(c!=null && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    private static void Validate(Canvas canvas, PlayerHUD player)
    {
        var dialog=Rect(canvas.transform.Find("Dialog Screen"));
        if(dialog.anchorMin!=Vector2.zero || dialog.anchorMax!=new Vector2(1,.5f)) throw new InvalidOperationException("Dialog must stay in the bottom half.");
        if(new SerializedObject(player).FindProperty("healthFill").objectReferenceValue==null) throw new InvalidOperationException("Health bar is not connected.");
        foreach(var b in canvas.GetComponentsInChildren<Button>(true))
            if(b.GetComponent<GUIButtonFeedback>()==null) throw new InvalidOperationException("Missing button feedback: "+b.name);
        foreach(var r in canvas.GetComponentsInChildren<ScrollRect>(true))
            if(r.content==null || r.viewport==null) throw new InvalidOperationException("Incomplete ScrollRect: "+r.name);
    }
}
