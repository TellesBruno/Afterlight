using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Renders an isolated copy, never toggling menus or quest state in the open scene.</summary>
public static class AfterlightGUIReview
{
    [MenuItem("Afterlight/GUI/Validate and capture previews")]
    public static void RunOpenScene() => Run(SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity"));

    public static void Run(Scene source)
    {
        const string output="Temp/AfterlightGUI";
        Directory.CreateDirectory(output);
        var report=new StringBuilder();
        var preview=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null;
        try
        {
            var original=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).First(c=>c.name=="Canvas");
            var clone=Object.Instantiate(original.gameObject); clone.name="GUI Review";
            SceneManager.MoveGameObjectToScene(clone,preview);
            var canvas=clone.GetComponent<Canvas>();
            foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                if(behaviour.GetType().Assembly==typeof(PlayerHUD).Assembly) behaviour.enabled=false;
            var cameraObject=new GameObject("GUI Preview Camera",typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject,preview);
            var camera=cameraObject.GetComponent<Camera>(); camera.scene=preview;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(17,22,26,255);
            camera.cullingMask=1<<5; camera.orthographic=true; camera.orthographicSize=540;
            camera.nearClipPlane=.1f; camera.farClipPlane=20; camera.enabled=false;
            var data=camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing=false; data.renderShadows=false;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            foreach(Transform child in clone.transform) child.gameObject.SetActive(false);

            CheckRuntimeBindings(clone,preview,report);
            foreach(string screenName in new[]{"PauseMenu","PlayerUi","Clue Menu","Quest Menu","Dialog Screen","Tutorial","DeathScreen","CivilianKillScreen","FinishLevel"})
            {
                var screen=clone.transform.Find(screenName); screen.gameObject.SetActive(true);
                if(screenName=="PlayerUi")
                {
                    foreach(var group in screen.GetComponentsInChildren<CanvasGroup>(true)) group.alpha=0;
                    screen.Find("LifeStatus/LifeCount").GetComponent<TMP_Text>().text="82";
                    screen.Find("LifeStatus/HealthBar/Fill").GetComponent<Image>().fillAmount=.82f;
                    screen.Find("AmmoStatus/CurrentAmmo").GetComponent<TMP_Text>().text="12";
                    screen.Find("AmmoStatus/TotalAmmo").GetComponent<TMP_Text>().text="48";
                }
                if(screenName=="PauseMenu") screen.Find("PauseInfo/CurrentObjective").GetComponent<TMP_Text>().text="Investigate the Area";
                if(screenName=="Clue Menu" || screenName=="Quest Menu")
                {
                    bool clue=screenName=="Clue Menu";
                    var ui=new SerializedObject(clue?(Object)screen.GetComponent<ClueListUI>():screen.GetComponent<QuestListUI>());
                    ((TMP_Text)ui.FindProperty("titleText").objectReferenceValue).text=clue?"Shipments from Local Warehouse":"Investigate the Area";
                    ((TMP_Text)ui.FindProperty("descriptionText").objectReferenceValue).text=clue?AssetDatabase.LoadAssetAtPath<ClueData>("Assets/DataStore/Clues/IndustrialDocs.asset").description:AssetDatabase.LoadAssetAtPath<QuestData>("Assets/DataStore/Quest/QuestInvestigateArea01.asset").description;
                    var content=(Transform)ui.FindProperty("buttonContainer").objectReferenceValue;
                    var prefab=(GameObject)ui.FindProperty(clue?"clueButtonPrefab":"questButtonPrefab").objectReferenceValue;
                    foreach(string label in clue?new[]{"Shipments from Local Warehouse","Physical Picture","???"}:new[]{"Investigate the Area (Active)","???"}) Spawn(prefab,content,label);
                }
                if(screenName=="Dialog Screen")
                {
                    var ui=new SerializedObject(screen.GetComponent<DialogUI>());
                    var npc=AssetDatabase.LoadAssetAtPath<NPCData>("Assets/DataStore/NPC/JohnNPC.asset");
                    ((TMP_Text)ui.FindProperty("responseText").objectReferenceValue).text=npc.options[2].responseText;
                    var content=(Transform)ui.FindProperty("optionsContainer").objectReferenceValue;
                    var prefab=(GameObject)ui.FindProperty("optionButtonPrefab").objectReferenceValue;
                    foreach(var option in npc.options.Take(3)) Spawn(prefab,content,option.optionText);
                    Spawn(prefab,content,npc.closeOptionText);
                }
                var highlighted=screen.GetComponentsInChildren<GUIButtonFeedback>(true).FirstOrDefault();
                if(highlighted!=null)
                {
                    Invoke(highlighted,"Awake");
                    highlighted.OnSelect(null);
                    var so=new SerializedObject(highlighted);
                    var label=(TMP_Text)so.FindProperty("label").objectReferenceValue;
                    if(label.color!=so.FindProperty("accentColor").colorValue) throw new InvalidOperationException("Selected text did not receive its accent.");
                }
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2560,1080)})
                {
                    target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture=target;
                    camera.aspect=(float)size.x/size.y;
                    Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)clone.transform); Canvas.ForceUpdateCanvases();
                    foreach(var scrolling in screen.GetComponentsInChildren<ScrollRect>(true)) scrolling.Rebuild(CanvasUpdate.PostLayout);
                    if(GraphicsSettings.currentRenderPipeline!=null)
                        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                    else camera.Render();
                    Canvas.ForceUpdateCanvases();
                    CheckBounds((RectTransform)screen,canvas,screenName,report,size);
                    if(size.x==1920)
                    {
                        var prior=RenderTexture.active;
                        try
                        {
                            RenderTexture.active=target;
                            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                            image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                            File.WriteAllBytes(output+"/"+screenName.Replace(" ","")+".png",image.EncodeToPNG()); Object.DestroyImmediate(image);
                        }
                        finally { RenderTexture.active=prior; }
                    }
                    camera.targetTexture=null; Object.DestroyImmediate(target); target=null;
                }
                screen.gameObject.SetActive(false);
            }
            File.WriteAllText(output+"/validation.txt",report.ToString());
        }
        catch(Exception e) { File.WriteAllText(output+"/validation.txt",report+"\nFAILED: "+e); throw; }
        finally { if(target!=null) Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void Spawn(GameObject prefab,Transform parent,string text)
    {
        var instance=Object.Instantiate(prefab,parent); instance.GetComponentInChildren<TMP_Text>(true).text=text;
    }

    private static void Invoke(object instance,string method,params object[] args) => instance.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(instance,args);

    private static void CheckRuntimeBindings(GameObject clone,Scene preview,StringBuilder report)
    {
        var hud=clone.GetComponentInChildren<PlayerHUD>(true);
        var image=(Image)new SerializedObject(hud).FindProperty("healthFill").objectReferenceValue;
        Invoke(hud,"RefreshHealth",82f,100f);
        if(Mathf.Abs(image.fillAmount-.82f)>.001f) throw new InvalidOperationException("Health ratio failed.");
        Invoke(hud,"RefreshHealth",10f,0f);
        if(image.fillAmount!=0) throw new InvalidOperationException("Zero maximum health is not handled.");
        Invoke(hud,"RefreshHealth",150f,100f);
        if(image.fillAmount!=1) throw new InvalidOperationException("Health bar did not clamp.");
        report.AppendLine("Health fill: current/max, zero maximum, and clamping passed.");

        var go=new GameObject("Isolated quest test"); SceneManager.MoveGameObjectToScene(go,preview);
        var manager=go.AddComponent<QuestManager>(); manager.enabled=false;
        var registry=ScriptableObject.CreateInstance<QuestRegistry>();
        var primary=ScriptableObject.CreateInstance<QuestData>(); primary.id="ui-primary"; primary.title="Primary preview"; primary.questType=QuestType.Primary;
        var secondary=ScriptableObject.CreateInstance<QuestData>(); secondary.id="ui-secondary"; secondary.title="Secondary preview"; secondary.questType=QuestType.Secondary;
        primary.completeCondition=new QuestCondition(); primary.failCondition=new QuestCondition();
        secondary.completeCondition=new QuestCondition(); secondary.failCondition=new QuestCondition();
        registry.allQuests=new System.Collections.Generic.List<QuestData>{secondary,primary};
        var managerSO=new SerializedObject(manager); managerSO.FindProperty("registry").objectReferenceValue=registry; managerSO.ApplyModifiedPropertiesWithoutUndo();
        var pause=clone.GetComponentInChildren<PauseMenuHUD>(true);
        var pauseSO=new SerializedObject(pause); pauseSO.FindProperty("questManager").objectReferenceValue=manager; pauseSO.ApplyModifiedPropertiesWithoutUndo();
        var text=(TMP_Text)pauseSO.FindProperty("objectiveText").objectReferenceValue;
        try
        {
            Invoke(pause,"OnEnable");
            if(text.text!="No active objectives.") throw new InvalidOperationException("Empty quest state failed.");
            manager.StartQuest(secondary);
            if(text.text!=secondary.title) throw new InvalidOperationException("Secondary fallback failed.");
            manager.StartQuest(primary);
            if(text.text!=primary.title) throw new InvalidOperationException("Primary priority failed.");
            manager.CompleteQuest(primary);
            if(text.text!=secondary.title) throw new InvalidOperationException("Quest completion refresh failed.");
            report.AppendLine("Pause objective: empty state, event updates, primary priority, completion fallback passed.");
        }
        finally { Invoke(pause,"OnDisable"); Object.DestroyImmediate(registry);Object.DestroyImmediate(primary);Object.DestroyImmediate(secondary);Object.DestroyImmediate(go); }
    }

    private static void CheckBounds(RectTransform screen,Canvas canvas,string name,StringBuilder report,Vector2Int size)
    {
        var corners=new Vector3[4]; screen.GetWorldCorners(corners);
        var canvasRect=(RectTransform)canvas.transform;
        var bottom=canvasRect.InverseTransformPoint(corners[0]); var top=canvasRect.InverseTransformPoint(corners[2]);
        var rect=canvasRect.rect;
        bool contained=bottom.x>=rect.xMin-1 && bottom.y>=rect.yMin-1 && top.x<=rect.xMax+1 && top.y<=rect.yMax+1;
        if(name=="Dialog Screen") contained &= top.y<=rect.center.y+1;
        if(!contained) throw new InvalidOperationException(name+" exceeds its allowed bounds at "+size);
        foreach(var scroll in screen.GetComponentsInChildren<ScrollRect>(true))
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            if(scroll.content.rect.width>scroll.viewport.rect.width+2) throw new InvalidOperationException("Horizontal content overflow: "+scroll.name);
        }
        report.AppendLine(name+" "+size+": within bounds; scroll content fits width.");
    }
}
