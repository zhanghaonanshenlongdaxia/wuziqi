using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using Wuziqi.Game;
using Wuziqi.UI;

/// <summary>更新历史面板构建工具：菜单"仙喵五子棋/重建更新历史预制体"，幂等可重复执行。</summary>
public static class UhBuild
{
    private const string RowPrefabPath = "Assets/Prefabs/UI/UpdateHistoryRow.prefab";
    private const string PanelPrefabPath = "Assets/Prefabs/UI/UpdateHistoryPanel.prefab";
    private static TMP_FontAsset uhFont;

    [MenuItem("仙喵五子棋/重建更新历史预制体")]
    public static void BuildAll()
    {
        uhFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Codely/Fonts/NotoSansSC-Regular SDF.asset");
        var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI_Kit/BgPanelPopup.png");
        var rowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI_Kit/BgButtonMedium.png");
        var closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI_Kit/IconCloseX.png");
        var ink = new Color(0.23f, 0.22f, 0.20f, 1f);
        var inkFaded = new Color(0.23f, 0.22f, 0.20f, 0.65f);

        // Row
        var rowRoot = MakeChild(null, "UpdateHistoryRow", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(584f, 200f));
        var rowImg = rowRoot.AddComponent<Image>();
        rowImg.sprite = rowSprite; rowImg.type = Image.Type.Sliced;
        rowImg.color = new Color(1f, 0.99f, 0.92f, 0.9f);
        rowRoot.AddComponent<LayoutElement>().preferredHeight = 200f;

        MakeText(rowRoot.transform, "VerText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(220f, 38f), "v1.3.0", 28, ink, TextAlignmentOptions.Left);
        MakeText(rowRoot.transform, "DateText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -18f), new Vector2(180f, 30f), "2026.09.01", 20, inkFaded, TextAlignmentOptions.Right);
        MakeText(rowRoot.transform, "TitleText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(96f, -62f), new Vector2(400f, 36f), "灵智觉醒", 26, ink, TextAlignmentOptions.Left);
        MakeText(rowRoot.transform, "ChangesText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -106f), new Vector2(544f, 86f), "更新内容", 22, inkFaded, TextAlignmentOptions.Left);

        var rowComp = rowRoot.AddComponent<UpdateHistoryRow>();
        var soRow = new SerializedObject(rowComp);
        soRow.FindProperty("verText").objectReferenceValue = rowRoot.transform.Find("VerText").GetComponent<TMP_Text>();
        soRow.FindProperty("dateText").objectReferenceValue = rowRoot.transform.Find("DateText").GetComponent<TMP_Text>();
        soRow.FindProperty("titleText").objectReferenceValue = rowRoot.transform.Find("TitleText").GetComponent<TMP_Text>();
        soRow.FindProperty("changesText").objectReferenceValue = rowRoot.transform.Find("ChangesText").GetComponent<TMP_Text>();
        soRow.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(rowRoot, RowPrefabPath);
        Object.DestroyImmediate(rowRoot);

        // Panel
        var panel = MakeChild(null, "UpdateHistoryPanel", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1920f, 1080f));
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);

        var dim = MakeChild(panel.transform, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        dim.GetComponent<Image>().raycastTarget = true;

        var window = MakeChild(panel.transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 860f));
        var winImg = window.AddComponent<Image>();
        winImg.sprite = panelSprite; winImg.type = Image.Type.Sliced;
        winImg.color = new Color(1f, 0.98f, 0.94f, 0.98f);

        MakeText(window.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(400f, 52f), "更 新 历 史", 42, ink, TextAlignmentOptions.Center);

        var closeGo = MakeChild(window.transform, "Close", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -38f), new Vector2(52f, 52f));
        var cimg = closeGo.AddComponent<Image>();
        cimg.sprite = closeSprite; cimg.raycastTarget = true;
        closeGo.AddComponent<Button>().transition = Selectable.Transition.None;

        var scroll = MakeChild(window.transform, "Scroll", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var srt = scroll.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(28f, 32f); srt.offsetMax = new Vector2(-28f, -92f);
        var srect = scroll.AddComponent<ScrollRect>();
        srect.horizontal = false;
        srect.movementType = ScrollRect.MovementType.Clamped;
        srect.scrollSensitivity = 30f;

        var viewport = MakeChild(scroll.transform, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.AddComponent<RectMask2D>();

        var content = MakeChild(viewport.transform, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(584f, 0f));
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(6, 6, 4, 12);
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        srect.content = crt;
        srect.viewport = viewport.GetComponent<RectTransform>();

        var panelComp = panel.AddComponent<UpdateHistoryPanel>();
        var soPanel = new SerializedObject(panelComp);
        soPanel.FindProperty("listRoot").objectReferenceValue = content.transform;
        soPanel.FindProperty("closeButton").objectReferenceValue = closeGo.GetComponent<Button>();
        soPanel.FindProperty("rowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        soPanel.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(panel, PanelPrefabPath);
        Object.DestroyImmediate(panel);
    }

    private static GameObject MakeChild(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }

    private static TMP_Text MakeText(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size,
        string text, float fontSize, Color color, TextAlignmentOptions align)
    {
        var go = MakeChild(parent, name, aMin, aMax, pos, size);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = uhFont; t.text = text; t.fontSize = fontSize; t.color = color;
        t.alignment = align; t.raycastTarget = false;
        return t;
    }
}