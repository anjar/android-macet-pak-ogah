using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Macet
{
    public sealed class UIManager : MonoBehaviour
    {
        GameManager game;
        RectTransform safeRoot;
        TMP_FontAsset font;
        Vector2 lastSize;
        Rect lastSafeArea;
        readonly Color ink = new Color(0.09f, 0.17f, 0.19f);
        public void Initialize(GameManager manager)
        {
            game = manager;
            font = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = gameObject.AddComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(540, 960); scale.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            safeRoot = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(transform, false); UpdateSafeArea();
        }
        void Update()
        {
            if (safeRoot != null && (lastSize != new Vector2(Screen.width, Screen.height) || lastSafeArea != Screen.safeArea)) UpdateSafeArea();
        }
        void UpdateSafeArea()
        {
            lastSize = new Vector2(Screen.width, Screen.height); lastSafeArea = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }
        void Clear()
        {
            for (int i = safeRoot.childCount - 1; i >= 0; i--) { var child = safeRoot.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
        }
        RectTransform Rect(string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(safeRoot, false); rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = pos; rect.sizeDelta = size; return rect;
        }
        void Label(string message, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize = 28)
        {
            var label = Rect(message, anchor, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = message; label.fontSize = fontSize; label.color = ink;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        }
        void Button(string text, Vector2 anchor, Vector2 pos, Action action)
        {
            var rect = Rect(text, anchor, pos, new Vector2(210, 62));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(1, 0.81f, 0.25f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            var label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(rect, false); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = font; label.text = text; label.fontSize = 25; label.color = ink;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        }
        void Backdrop()
        {
            var rect = Rect("Panel", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.gameObject.AddComponent<Image>().color = new Color(0.90f, 0.94f, 0.86f, 0.97f);
        }
        public void ShowMenu(LevelData[] levels)
        {
            Clear(); Backdrop(); Label("MACET!", new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(450, 100), 64);
            Label("Beresin macetnya.\nTap mobil, perhatikan urutannya.", new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(480, 90), 24);
            for (int i = 0; i < levels.Length; i++)
            { int index = i; Button("Level " + levels[i].levelNumber, new Vector2(0.5f, 0.5f), new Vector2(0, 20 - i * 80), () => game.StartLevel(index)); }
            Label("PHASE 1 • PROTOTYPE", new Vector2(0.5f, 0), new Vector2(0, 45), new Vector2(450, 50), 18);
        }
        public void ShowGame(LevelData level)
        {
            Clear(); Label("LEVEL " + level.levelNumber, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(450, 60), 32);
            Button("Ulang", new Vector2(0, 1), new Vector2(120, -110), game.Restart);
            Button("Jeda", new Vector2(1, 1), new Vector2(-120, -110), game.Pause);
            Label(level.instruction, new Vector2(0.5f, 0), new Vector2(0, 75), new Vector2(480, 120), 24);
        }
        public void ShowPause()
        {
            Clear(); Backdrop(); Label("JEDA", new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(450, 80), 44);
            Button("Lanjut", new Vector2(0.5f, 0.5f), new Vector2(0, 30), game.Resume);
            Button("Ulang", new Vector2(0.5f, 0.5f), new Vector2(0, -50), game.Restart);
            Button("Menu", new Vector2(0.5f, 0.5f), new Vector2(0, -130), game.Menu);
        }
        public void ShowResult(bool completed, bool hasNext)
        {
            Clear(); Backdrop(); Label(completed ? "LANCAR!" : "TABRAKAN!", new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(480, 90), 44);
            Label(completed ? "Jalan sudah kosong." : "Coba urutan lain. Tunggu jalurnya aman.", new Vector2(0.5f, 0.5f), new Vector2(0, 85), new Vector2(480, 70), 23);
            if (completed) Button(hasNext ? "Level berikutnya" : "Selesai", new Vector2(0.5f, 0.5f), new Vector2(0, 0), game.Next);
            Button("Ulang", new Vector2(0.5f, 0.5f), new Vector2(0, completed ? -80 : 0), game.Restart);
            Button("Menu", new Vector2(0.5f, 0.5f), new Vector2(0, completed ? -160 : -80), game.Menu);
        }
    }
}
