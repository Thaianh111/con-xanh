// Cồn Xanh – compact phone UI built in code (uGUI): main bar, tool drawer, ghost buttons, hint, toast.
// VTA Games · Võ Thái Anh
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ConXanh
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI I;
        Font font; RectTransform canvasRt, drawerContent, ghostBar; GameObject drawer;
        Text hint, toast, ghostMsg; Button soundBtn, timeBtn;
        float hintT, toastT; string openCat;
        readonly List<(Button b, string key)> drawerButtons = new List<(Button, string)>();
        readonly List<(Button b, string key)> mainButtons = new List<(Button, string)>();
        static readonly Color Paper = new Color(1f, 0.99f, 0.96f, 0.95f), Green = new Color(0.18f, 0.49f, 0.29f), Ink = new Color(0.2f, 0.25f, 0.17f), OnBg = new Color(0.86f, 0.95f, 0.82f);

        struct Item { public string key, label; public ToolKind tool; public string place; }
        static readonly Dictionary<string, Item[]> Cats = new Dictionary<string, Item[]>
        {
            { "dia", new[] { It("land", "Đất", ToolKind.Land), It("pond", "Ao", ToolKind.Pond), It("path", "Đường", ToolKind.Path), It("erase", "Dỡ", ToolKind.Erase) } },
            { "nha", new[] { P("nha_la", "Nhà lá"), P("nha_ngoi", "Nhà ngói"), P("lu_nuoc", "Lu nước"), P("cho", "Chợ"), P("dinh", "Đình"), P("chua", "Chùa"), P("truong", "Trường") } },
            { "sx", new[] { P("ruong", "Ruộng"), P("vuon_rau", "Vườn rau"), P("ben_ca", "Bến cá"), P("bui_tre", "Bụi tre"), P("dua", "Dừa"), P("lo_keo", "Lò kẹo") } },
            { "deco", new[] { P("cay", "Cây"), P("chuoi", "Chuối"), P("dua_nuoc", "Dừa nước"), P("hoa", "Bụi hoa"), P("xuong", "Xuồng") } },
        };
        static Item It(string k, string l, ToolKind t) => new Item { key = k, label = l, tool = t };
        static Item P(string k, string l) => new Item { key = k, label = l, tool = ToolKind.Place, place = k };
        static readonly Dictionary<string, string> Hints = new Dictionary<string, string>
        {
            { "view", "Kéo 1 ngón để xoay · 2 ngón để zoom và xoay" },
            { "land", "Kéo để đắp đất: ra biển thành bãi mới, kéo tiếp thành đồi" },
            { "pond", "Kéo để đào ao – nước tự chảy vào" },
            { "path", "Kéo đường đất – băng qua nước tự bắc cầu khỉ" },
            { "erase", "Chạm vào công trình hoặc cây để dỡ" },
        };

        void Start()
        {
            I = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem").AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
                es.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }
            var cgo = new GameObject("UI"); var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var sc = cgo.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080, 1920); sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>(); canvasRt = cgo.GetComponent<RectTransform>();

            // top-left: title + sound + time
            var top = Panel(canvasRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(560, 84), Color.clear);
            var title = Label(top, "Cồn Xanh", 34, Color.white, TextAnchor.MiddleLeft); Place(title.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(190, 0));
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);
            soundBtn = Btn(top, "", () => { Sound.I.Toggle(); RefreshTop(); }, new Vector2(195, 0), new Vector2(170, 72), 24);
            timeBtn = Btn(top, "", () => { DayNight.I.auto = !DayNight.I.auto; RefreshTop(); }, new Vector2(375, 0), new Vector2(170, 72), 24);
            RefreshTop();

            // toast + hint
            toast = Label(canvasRt, "", 30, Color.white, TextAnchor.MiddleCenter); Place(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(980, 70));
            toast.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.3f, 0.15f, 0.9f);
            hint = Label(canvasRt, "", 28, Color.white, TextAnchor.MiddleCenter); Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 380), new Vector2(1000, 60));
            hint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            // main bar
            var bar = Panel(canvasRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1000, 130), Paper);
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.padding = new RectOffset(10, 10, 10, 10); hl.spacing = 8; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            AddMain(bar, "view", "Xem", () => { CloseDrawer(); Builder.I.SetTool(ToolKind.View); ShowHint("view"); });
            AddMain(bar, "dia", "Địa hình", () => OpenCat("dia"));
            AddMain(bar, "nha", "Nhà", () => OpenCat("nha"));
            AddMain(bar, "sx", "Sản xuất", () => OpenCat("sx"));
            AddMain(bar, "deco", "Trang trí", () => OpenCat("deco"));

            // drawer (scrollable row)
            drawer = new GameObject("Drawer", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            var drt = drawer.GetComponent<RectTransform>(); drt.SetParent(canvasRt, false); Place(drt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 168), new Vector2(1040, 140));
            drawer.GetComponent<Image>().color = new Color(1, 1, 1, 0.0f);
            drawerContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); drawerContent.SetParent(drt, false);
            drawerContent.anchorMin = new Vector2(0, 0); drawerContent.anchorMax = new Vector2(0, 1); drawerContent.pivot = new Vector2(0, 0.5f);
            var dl = drawerContent.gameObject.AddComponent<HorizontalLayoutGroup>(); dl.spacing = 8; dl.padding = new RectOffset(6, 6, 6, 6); dl.childForceExpandWidth = false; dl.childForceExpandHeight = true; dl.childControlWidth = true; dl.childControlHeight = true;
            drawerContent.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = drawer.GetComponent<ScrollRect>(); sr.content = drawerContent; sr.horizontal = true; sr.vertical = false; sr.movementType = ScrollRect.MovementType.Clamped;
            drawer.SetActive(false);

            // ghost bar
            ghostBar = Panel(canvasRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 470), new Vector2(620, 120), Color.clear);
            Btn(ghostBar, "Đặt", () => Builder.I.ConfirmGhost(), new Vector2(0, 30), new Vector2(220, 84), 32, Green, Color.white);
            Btn(ghostBar, "Xoay", () => Builder.I.RotateGhost(), new Vector2(230, 30), new Vector2(170, 84), 30);
            Btn(ghostBar, "Hủy", () => Builder.I.CancelGhost(), new Vector2(410, 30), new Vector2(170, 84), 30);
            ghostMsg = Label(ghostBar, "", 26, Color.white, TextAnchor.MiddleCenter); Place(ghostMsg.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -6), new Vector2(0, 36));
            ghostMsg.gameObject.AddComponent<Outline>().effectColor = Color.black;
            ghostBar.gameObject.SetActive(false);

            Builder.I.onChanged += Refresh;
            Refresh(); ShowHint("view");
            Toast("Chào mừng về Cồn Xanh! Chọn Địa hình hoặc Nhà để bắt đầu", 3.5f);
        }

        // ---------- behaviour ----------
        void OpenCat(string cat)
        {
            if (openCat == cat && drawer.activeSelf) { CloseDrawer(); return; }
            openCat = cat; drawer.SetActive(true);
            foreach (Transform c in drawerContent) Destroy(c.gameObject);
            drawerButtons.Clear();
            foreach (var it in Cats[cat])
            {
                var item = it;
                var b = Btn(drawerContent, item.label, () =>
                {
                    Builder.I.SetTool(item.tool, item.place);
                    if (item.tool == ToolKind.Place) ShowHintText(Builder.I.defs[item.place].hint); else ShowHint(item.key);
                }, Vector2.zero, new Vector2(150, 120), 27);
                var le = b.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 150; le.preferredHeight = 120;
                drawerButtons.Add((b, item.key));
            }
            Refresh();
        }
        void CloseDrawer() { openCat = null; if (drawer) drawer.SetActive(false); Refresh(); }

        void Refresh()
        {
            var bld = Builder.I;
            string cur = bld.tool == ToolKind.Place && bld.place != null ? bld.place.id : bld.tool.ToString().ToLower();
            foreach (var (b, key) in drawerButtons) Tint(b, key == cur);
            foreach (var (b, key) in mainButtons) Tint(b, key == "view" ? bld.tool == ToolKind.View : key == openCat);
            if (ghostBar)
            {
                ghostBar.gameObject.SetActive(bld.GhostActive);
                if (bld.GhostActive) { ghostMsg.text = bld.GhostMsg ?? ""; ghostBar.GetChild(0).GetComponent<Button>().interactable = bld.GhostValid; }
            }
        }
        void RefreshTop()
        {
            soundBtn.GetComponentInChildren<Text>().text = Sound.I != null && Sound.I.on ? "Tiếng: bật" : "Tiếng: tắt";
            timeBtn.GetComponentInChildren<Text>().text = DayNight.I != null && DayNight.I.auto ? "Giờ: chạy" : "Giờ: dừng";
        }
        void ShowHint(string key) { if (Hints.TryGetValue(key, out var h)) ShowHintText(h); }
        void ShowHintText(string h) { hint.text = h; hintT = 3.5f; }
        public void Toast(string t, float secs = 2f) { toast.text = t; toastT = secs; }

        void Update()
        {
            hintT -= Time.deltaTime; toastT -= Time.deltaTime;
            hint.color = new Color(1, 1, 1, Mathf.Clamp01(hintT * 2));
            toast.color = new Color(1, 1, 1, Mathf.Clamp01(toastT * 2));
        }

        // ---------- widgets ----------
        void AddMain(RectTransform bar, string key, string label, UnityEngine.Events.UnityAction a)
        {
            var b = Btn(bar, label, a, Vector2.zero, new Vector2(180, 110), 28); mainButtons.Add((b, key));
        }
        static void Tint(Button b, bool on) { b.GetComponent<Image>().color = on ? OnBg : Paper; }
        static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(aMin.x == aMax.x ? aMin.x : 0.5f, aMin.y == aMax.y ? aMin.y : 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
        RectTransform Panel(RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            Place(rt, aMin, aMax, pos, size); var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = c.a > 0.01f;
            return rt;
        }
        Text Label(RectTransform parent, string s, int size, Color c, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.GetComponent<RectTransform>().SetParent(parent, false);
            var t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.color = c; t.alignment = anchor; t.text = s; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
        Button Btn(RectTransform parent, string s, UnityEngine.Events.UnityAction a, Vector2 pos, Vector2 size, int fontSize, Color? bg = null, Color? fg = null)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            Place(rt, new Vector2(0, 0), new Vector2(0, 0), pos, size);
            go.GetComponent<Image>().color = bg ?? Paper;
            var b = go.GetComponent<Button>(); b.onClick.AddListener(a);
            var t = Label(rt, s, fontSize, fg ?? Ink, TextAnchor.MiddleCenter); var trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
            t.fontStyle = FontStyle.Bold;
            return b;
        }
    }
}
