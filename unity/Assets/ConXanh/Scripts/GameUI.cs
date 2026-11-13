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
        Text hint, toast, ghostMsg, resText, infoText, goalText, orderText; Button soundBtn, timeBtn, newBtn, giveBtn, orderPill; RectTransform orderCard; float newConfirmT;
        readonly List<(RectTransform rt, Vector3 world, float t)> floaters = new List<(RectTransform, Vector3, float)>();
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
            newBtn = Btn(top, "Làng mới", NewVillageTap, new Vector2(555, 0), new Vector2(190, 72), 24);
            RefreshTop();

            // HUD: resources, people/happiness/day, goal
            var hud = Panel(canvasRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), new Vector2(-40, 150), new Color(0.08f, 0.2f, 0.12f, 0.55f));
            hud.pivot = new Vector2(0.5f, 1); hud.anchoredPosition = new Vector2(0, -118);
            resText = Label(hud, "", 27, Color.white, TextAnchor.UpperLeft); Place(resText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -8), new Vector2(-30, 40)); resText.rectTransform.pivot = new Vector2(0.5f, 1);
            infoText = Label(hud, "", 25, new Color(0.9f, 1f, 0.85f), TextAnchor.UpperLeft); Place(infoText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -52), new Vector2(-30, 40)); infoText.rectTransform.pivot = new Vector2(0.5f, 1);
            goalText = Label(hud, "", 25, new Color(1f, 0.93f, 0.6f), TextAnchor.UpperLeft); Place(goalText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -96), new Vector2(-30, 40)); goalText.rectTransform.pivot = new Vector2(0.5f, 1);

            // trader order card
            orderCard = Panel(canvasRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -360), new Vector2(760, 190), Paper);
            orderText = Label(orderCard, "", 28, Ink, TextAnchor.MiddleCenter); Place(orderText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -12), new Vector2(-30, 80)); orderText.rectTransform.pivot = new Vector2(0.5f, 1);
            giveBtn = Btn(orderCard, "Giao hàng", () => { if (TraderBoat.I == null || !TraderBoat.I.Deliver()) Toast("Chưa đủ hàng để giao"); RefreshOrder(); }, new Vector2(150, 14), new Vector2(260, 80), 28, Green, Color.white);
            Btn(orderCard, "Để sau", () => { orderCard.gameObject.SetActive(false); RefreshOrder(); }, new Vector2(430, 14), new Vector2(180, 80), 26);
            orderCard.gameObject.SetActive(false);
            orderPill = Btn(canvasRt, "Đơn của ghe", () => { orderCard.gameObject.SetActive(true); RefreshOrder(); }, Vector2.zero, new Vector2(260, 70), 25, new Color(1f, 0.85f, 0.45f, 0.95f));
            Place(orderPill.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -290), new Vector2(260, 70));
            orderPill.gameObject.SetActive(false);

            // toast + hint
            toast = Label(canvasRt, "", 30, Color.white, TextAnchor.MiddleCenter); Place(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(980, 70));
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
            if (Economy.I != null) Economy.I.onChanged += RefreshHud;
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
                var b = Btn(drawerContent, ItemLabel(item), () =>
                {
                    Builder.I.SetTool(item.tool, item.place);
                    if (item.tool == ToolKind.Place) { var why = Economy.I?.CantBuild(item.place); ShowHintText(why ?? Builder.I.defs[item.place].hint); } else ShowHint(item.key);
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
            foreach (var (b, key) in drawerButtons)
            {
                Tint(b, key == cur);
                var e = Economy.I; if (e == null || !Builder.I.defs.ContainsKey(key)) continue;
                bool locked = !e.Unlocked(key), poor = e.CantBuild(key) != null;
                var txt = b.GetComponentInChildren<Text>(); var item = FindItem(key); txt.text = ItemLabel(item);
                txt.color = locked ? new Color(0.55f, 0.55f, 0.5f) : poor ? new Color(0.7f, 0.3f, 0.2f) : Ink;
            }
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
        string ItemLabel(Item it)
        {
            if (it.tool != ToolKind.Place || Economy.I == null) return it.label;
            var lk = Economy.I.LockText(it.place); if (lk != null) return it.label + "\n<size=19>" + lk + "</size>";
            return it.label + "\n<size=20>" + Economy.I.CostText(it.place) + "</size>";
        }
        static Item FindItem(string key) { foreach (var c in Cats.Values) foreach (var it in c) if (it.key == key) return it; return default; }

        void NewVillageTap()
        {
            if (newConfirmT > 0) { newConfirmT = 0; newBtn.GetComponentInChildren<Text>().text = "Làng mới"; SaveLoad.I?.NewGame(); return; }
            newConfirmT = 3f; newBtn.GetComponentInChildren<Text>().text = "Chắc chưa?"; Toast("Chạm lần nữa để bỏ làng này, làm lại từ đầu", 3f);
        }

        void RefreshHud()
        {
            var e = Economy.I; if (e == null || resText == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Economy.Keys.Length; i++) { if (i > 0) sb.Append("   "); sb.Append(Economy.Names[i]).Append(" <b>").Append(Mathf.FloorToInt(e.Get(Economy.Keys[i]))).Append("</b>"); }
            resText.text = sb.ToString();
            string season = Weather.Season != null ? Weather.Season.name : "";
            string sky = Weather.I != null && Weather.I.raining ? " · Mưa" : "";
            string mood = e.happy >= 70 ? "vui" : e.happy >= 45 ? "ổn" : e.happy >= 25 ? "buồn" : "rất buồn";
            infoText.text = e.RankName(e.rank) + " · Dân " + Mathf.FloorToInt(e.pop) + "/" + e.Housing + " · Vui " + Mathf.RoundToInt(e.happy) + " (" + mood + ")" + (e.hungry ? " · THIẾU ĂN" : "") + "   |   Ngày " + (DayNight.I ? DayNight.I.day : 1) + " · " + season + sky;
            goalText.text = "Mục tiêu: " + e.GoalText();
            RefreshOrder();
        }

        /// <summary>Shows the trader's order while the boat is docked.</summary>
        public void RefreshOrder()
        {
            if (orderCard == null) return;
            var tb = TraderBoat.I; var o = tb != null && tb.Docked ? tb.order : null;
            if (o == null) { orderCard.gameObject.SetActive(false); orderPill.gameObject.SetActive(false); return; }
            if (!ReferenceEquals(orderShownFor, o)) { orderCard.gameObject.SetActive(true); orderShownFor = o; }
            orderPill.gameObject.SetActive(!orderCard.gameObject.activeSelf);
            float have = Economy.I.Get(o.res);
            orderText.text = "Ghe thương lái cần <b>" + o.amount + " " + Economy.NameOf(o.res).ToLower() + "</b> – trả <b>" + o.reward + " đ</b>\n<size=23>Kho đang có " + Mathf.FloorToInt(have) + "</size>";
            giveBtn.interactable = have >= o.amount;
        }
        object orderShownFor = new object();

        /// <summary>Little "+14 lúa" text that rises above a building.</summary>
        public void Floater(Vector3 world, string text)
        {
            if (canvasRt == null || floaters.Count > 24) return;
            var t = Label(canvasRt, text, 26, new Color(1f, 0.97f, 0.7f), TextAnchor.MiddleCenter);
            t.gameObject.AddComponent<Outline>().effectColor = new Color(0.15f, 0.25f, 0.1f, 0.9f);
            t.rectTransform.sizeDelta = new Vector2(320, 44);
            floaters.Add((t.rectTransform, world, 0));
        }

        void ShowHint(string key) { if (Hints.TryGetValue(key, out var h)) ShowHintText(h); }
        void ShowHintText(string h) { hint.text = h; hintT = 3.5f; }
        public void Toast(string t, float secs = 2f) { toast.text = t; toastT = secs; }

        void Update()
        {
            hintT -= Time.deltaTime; toastT -= Time.deltaTime;
            hint.color = new Color(1, 1, 1, Mathf.Clamp01(hintT * 2));
            toast.color = new Color(1, 1, 1, Mathf.Clamp01(toastT * 2));
            if (newConfirmT > 0) { newConfirmT -= Time.deltaTime; if (newConfirmT <= 0) newBtn.GetComponentInChildren<Text>().text = "Làng mới"; }
            var cam = Camera.main;
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i]; f.t += Time.deltaTime;
                if (f.t > 1.6f || cam == null) { Destroy(f.rt.gameObject); floaters.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(f.world + Vector3.up * f.t * 0.35f);
                f.rt.gameObject.SetActive(sp.z > 0); f.rt.position = sp;
                f.rt.GetComponent<Text>().color = new Color(1f, 0.97f, 0.7f, Mathf.Clamp01((1.6f - f.t) * 2));
                floaters[i] = f;
            }
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
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.supportRichText = true; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
        Button Btn(RectTransform parent, string s, UnityEngine.Events.UnityAction a, Vector2 pos, Vector2 size, int fontSize, Color? bg = null, Color? fg = null)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            Place(rt, new Vector2(0, 0), new Vector2(0, 0), pos, size);
            go.GetComponent<Image>().color = bg ?? Paper;
            var b = go.GetComponent<Button>(); b.onClick.AddListener(() => Builder.uiClickFrame = Time.frameCount); b.onClick.AddListener(a);
            var t = Label(rt, s, fontSize, fg ?? Ink, TextAnchor.MiddleCenter); var trt = t.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
            t.fontStyle = FontStyle.Bold;
            return b;
        }
    }
}
