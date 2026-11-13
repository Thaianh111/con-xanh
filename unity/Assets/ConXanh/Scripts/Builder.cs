// Cồn Xanh – build tools: drag to shape land/pond/path, place buildings with a ghost preview, erase. VTA Games · Võ Thái Anh
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ConXanh
{
    public enum ToolKind { View, Land, Pond, Path, Erase, Place }

    /// <summary>What a placeable item is made of and how it sits in the world.</summary>
    public class PlaceDef
    {
        public string id, label, hint;
        public string[] models;      // Resources/Models/<name>
        public string[] packNodes;   // nodes inside thien_nhien_pack
        public float scale = 1, block = 0.2f, flatten = 0;
        public bool ghost, shore, water;
    }

    public class Builder : MonoBehaviour
    {
        public static Builder I;
        public ToolKind tool = ToolKind.View;
        public PlaceDef place;
        public readonly Dictionary<string, PlaceDef> defs = new Dictionary<string, PlaceDef>();
        public System.Action onChanged;
        public static int uiClickFrame = -10;   // a button acted this frame: don't treat the same click as a world tap

        Island island; ConXanhGame game;
        bool stroking; Vector3 last; Vector2 downPos; bool downOverUI;
        Vector3? lastBridge; Vector3? lastLand;
        GameObject ghost; PlaceDef ghostDef; Vector3 ghostPos; float ghostRot; Material ghostMat;
        public bool GhostActive => ghost != null;
        public bool GhostValid { get; private set; }
        public string GhostMsg { get; private set; }

        void Awake()
        {
            I = this;
            void D(string id, string label, string hint, float block, float flatten, bool ghost, params string[] models) =>
                defs[id] = new PlaceDef { id = id, label = label, hint = hint, block = block, flatten = flatten, ghost = ghost, models = models };
            D("nha_la", "Nhà lá", "Nhà lá trên cọc: hiên, võng, đèn dầu", 0.8f, 0.75f, true, "nha_la");
            D("nha_ngoi", "Nhà ngói", "Nhà ngói tường vàng, mái đỏ", 0.85f, 0.85f, true, "nha_ngoi");
            D("lu_nuoc", "Lu nước", "Lu nước cạnh nhà", 0.12f, 0, true, "lu_nuoc");
            D("cho", "Chợ", "Sạp chợ mái vải", 0.95f, 1.1f, true, "cho");
            D("dinh", "Đình", "Đình làng mái ngói đầu đao", 1.15f, 1.3f, true, "dinh");
            D("chua", "Chùa", "Chùa có tháp và tượng Quan Âm", 1.1f, 1.2f, true, "chua");
            D("truong", "Trường", "Trường học có cờ, kẻng", 1.1f, 1.2f, true, "truong");
            D("ruong", "Ruộng", "Ruộng lúa có trâu cày", 1.3f, 1.55f, true, "ruong");
            D("vuon_rau", "Vườn rau", "Vườn rau, giàn bầu", 0.8f, 0.95f, true, "vuon_rau");
            D("lo_keo", "Lò kẹo", "Lò kẹo dừa", 0.6f, 0.75f, true, "lo_keo");
            D("ben_ca", "Bến cá", "Đặt sát mé nước – bến tự quay ra nước", 0.3f, 0, true, "ben_ca"); defs["ben_ca"].shore = true;
            D("bui_tre", "Bụi tre", "Chạm để trồng tre", 0.3f, 0, false, "bui_tre_1", "bui_tre_2");
            D("chuoi", "Chuối", "Chạm để trồng chuối", 0.2f, 0, false, "chuoi_1", "chuoi_2");
            D("dua_nuoc", "Dừa nước", "Đẹp nhất ở mé ao, mé sông", 0.3f, 0, false, "dua_nuoc_1", "dua_nuoc_2");
            D("xuong", "Xuồng", "Chạm xuống ao hoặc biển", 0, 0, false, "xuong"); defs["xuong"].water = true;
            defs["dua"] = new PlaceDef { id = "dua", label = "Dừa", hint = "Chạm để trồng dừa", packNodes = new[] { "PalmTree_1", "PalmTree_2", "PalmTree_3" }, scale = 0.4f, block = 0.15f };
            defs["cay"] = new PlaceDef { id = "cay", label = "Cây", hint = "Chạm để trồng cây", packNodes = new[] { "NormalTree_3", "PineTree_1", "MapleTree_1" }, scale = 0.32f, block = 0.2f };
            defs["hoa"] = new PlaceDef { id = "hoa", label = "Bụi hoa", hint = "Chạm để trồng bụi hoa", packNodes = new[] { "Bush_Flowers", "Bush_Large_Flowers", "Plant_Flowers" }, scale = 0.5f, block = 0.2f };
            ghostMat = new Material(Shader.Find("ConXanh/Ghost"));
        }

        void Start() { island = ConXanhGame.I.island; game = ConXanhGame.I; }

        public void SetTool(ToolKind t, string placeId = null)
        {
            tool = t; place = placeId != null && defs.TryGetValue(placeId, out var d) ? d : null;
            if (ghost != null && (t != ToolKind.Place || ghostDef != place)) CancelGhost();
            OrbitCam.oneFingerRotate = t == ToolKind.View;
            onChanged?.Invoke();
        }

        static bool OverUI(Vector2 pos)
        {
            if (EventSystem.current == null) return false;
            var pe = new PointerEventData(EventSystem.current) { position = pos };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pe, hits);
            return hits.Count > 0;
        }

        Ray RayAt(Vector2 screen) => Camera.main.ScreenPointToRay(screen);

        void Update()
        {
            if (island == null) return;
            InputCompat.Pointer(out var pos, out bool down, out bool held, out bool up, out int count);
            if (count >= 2) { if (stroking) EndStroke(); return; }
            if (down) { downPos = pos; downOverUI = OverUI(pos) || Time.frameCount - uiClickFrame <= 1; if (!downOverUI && IsDrawTool) BeginStroke(pos); }
            if (held && stroking) ContinueStroke(pos);
            if (up)
            {
                if (stroking) EndStroke();
                else if (!downOverUI && Time.frameCount - uiClickFrame > 1 && (pos - downPos).magnitude < 12f) Tap(pos);
            }
        }

        bool IsDrawTool => tool == ToolKind.Land || tool == ToolKind.Pond || tool == ToolKind.Path;

        // ---------- strokes ----------
        void BeginStroke(Vector2 pos)
        {
            if (!island.Raycast(RayAt(pos), out var p)) return;
            stroking = true; last = p; lastBridge = null; lastLand = null;
            Brush(p, Vector3.forward);
        }
        void ContinueStroke(Vector2 pos)
        {
            if (!island.Raycast(RayAt(pos), out var p)) return;
            float stepLen = tool == ToolKind.Path ? 0.1f : 0.22f;
            var d = p - last; d.y = 0; float dist = d.magnitude; if (dist < stepLen) return;
            var dir = d / dist; int n = Mathf.FloorToInt(dist / stepLen);
            for (int k = 1; k <= n; k++) Brush(last + dir * stepLen * k, dir);
            last += dir * stepLen * n;
        }
        void EndStroke() { stroking = false; Sound.I?.Pluck(0, 0.12f); }

        void Brush(Vector3 p, Vector3 dir)
        {
            if (tool == ToolKind.Land) island.BrushLand(p.x, p.z);
            else if (tool == ToolKind.Pond) island.BrushPond(p.x, p.z);
            else if (tool == ToolKind.Path)
            {
                if (island.IsWater(p.x, p.z) && island.IsLandNear(p.x, p.z))
                {
                    if (lastBridge == null) lastBridge = lastLand ?? p;
                    if ((p - lastBridge.Value).magnitude > 0.45f) { SpawnBridge(lastBridge.Value, p); lastBridge = p; }
                    return;
                }
                if (lastBridge != null) { if ((p - lastBridge.Value).magnitude > 0.1f) SpawnBridge(lastBridge.Value, p); lastBridge = null; }
                lastLand = p; island.BrushPath(p.x, p.z);
            }
        }

        void SpawnBridge(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0; var mid = (a + b) / 2; var d = b - a; if (d.sqrMagnitude < 1e-4f) return;
            var go = game.Spawn("cau_khi", mid, Quaternion.LookRotation(d).eulerAngles.y, 1, 0);
            if (go != null) { var s = go.transform.localScale; go.transform.localScale = new Vector3(s.x, s.y, d.magnitude / 0.5f); go.name = "cau_khi"; game.AddPicker(go); var pl = go.GetComponent<Placed>(); pl.defId = "cau_khi"; pl.model = "cau_khi"; }
        }

        // ---------- taps ----------
        void Tap(Vector2 pos)
        {
            if (tool == ToolKind.Erase) { Erase(pos); return; }
            if (tool != ToolKind.Place || place == null) return;
            if (!island.Raycast(RayAt(pos), out var p)) return;
            if (place.ghost) ShowGhost(p);
            else Commit(place, p, Random.Range(0f, 360f));
        }

        string Check(PlaceDef d, Vector3 p, bool free = false)
        {
            if (!free && Economy.I != null) { var why = Economy.I.CantBuild(d.id); if (why != null) return why; }
            bool water = island.IsWater(p.x, p.z);
            if (d.water) return water ? null : "Xuồng phải thả dưới nước";
            if (!island.IsLand(p.x, p.z)) return "Chỗ này là nước, chọn chỗ đất nha";
            if (d.shore && ShoreAngle(p) == null) return "Bến cá phải đặt sát mé nước";
            return null;
        }

        float? ShoreAngle(Vector3 p)
        {
            float? best = null; float bestD = 99;
            for (float a = 0; a < 360; a += 12)
                for (float r = 0.4f; r <= 1.5f; r += 0.1f)
                {
                    var q = p + Quaternion.Euler(0, a, 0) * Vector3.forward * r;
                    if (island.IsWater(q.x, q.z)) { if (r < bestD) { bestD = r; best = a; } break; }
                }
            return best;
        }

        /// <summary>Builds d at p. free = no cost (start village); saved = restore exactly from a save.</summary>
        public GameObject Commit(PlaceDef d, Vector3 p, float rotY, bool free = false, SaveObj saved = null)
        {
            if (saved == null) { var err = Check(d, p, free); if (err != null) { GameUI.I?.Toast(err); return null; } }
            GameObject go; string model;
            if (d.water)
            {
                model = saved?.model ?? d.models[Random.Range(0, d.models.Length)];
                go = game.Spawn(model, new Vector3(p.x, island.InPond(p.x, p.z) ? Island.Pond : 0, p.z), rotY); if (go) go.AddComponent<Bob>();
            }
            else if (d.packNodes != null) { model = saved?.model ?? d.packNodes[Random.Range(0, d.packNodes.Length)]; go = game.SpawnPack(model, p, d.scale * Random.Range(0.85f, 1.2f), rotY); }
            else
            {
                if (d.flatten > 0 && saved == null) island.Flatten(p.x, p.z, d.flatten, p.y);
                if (d.shore && saved == null) rotY = ShoreAngle(p) ?? rotY;
                model = saved?.model ?? d.models[Random.Range(0, d.models.Length)];
                go = game.Spawn(model, p, rotY, 1, 0);
            }
            if (go == null) return null;
            if (saved != null) { if (!d.water) go.transform.position = saved.p; go.transform.rotation = saved.r; go.transform.localScale = saved.s; }
            game.Register(go, d.block);
            var pl = go.GetComponent<Placed>(); pl.defId = d.id; pl.model = model; if (saved != null) pl.prog = saved.prog;
            if (d.id == "ruong") game.AddPlowTeam(go);
            if (saved == null)
            {
                go.AddComponent<GrowIn>();
                if (!free) { Economy.I?.Pay(d.id); Sound.I?.Place(); }
            }
            onChanged?.Invoke();
            return go;
        }

        /// <summary>Restores a bridge plank from a save.</summary>
        public void RestoreBridge(SaveObj o)
        {
            var go = game.Spawn("cau_khi", o.p, 0, 1, 0); if (go == null) return;
            go.transform.rotation = o.r; go.transform.localScale = o.s; go.name = "cau_khi"; game.AddPicker(go);
            var pl = go.GetComponent<Placed>(); pl.defId = "cau_khi"; pl.model = "cau_khi";
        }

        // ---------- ghost ----------
        void ShowGhost(Vector3 p)
        {
            if (ghost == null || ghostDef != place)
            {
                CancelGhost();
                ghostDef = place;
                var src = Resources.Load<GameObject>("Models/" + place.models[0]);
                ghost = Instantiate(src); ghost.name = "Ghost";
                foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length]; for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                    r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                ghostRot = Camera.main.transform.eulerAngles.y + 180f;
            }
            ghostPos = p; UpdateGhost();
        }
        void UpdateGhost()
        {
            GhostMsg = Check(ghostDef, ghostPos); GhostValid = GhostMsg == null;
            ghost.transform.position = ghostPos;
            float r = ghostRot; if (ghostDef.shore) r = ShoreAngle(ghostPos) ?? r;
            ghost.transform.rotation = Quaternion.Euler(0, r, 0);
            ghostMat.SetColor("_Color", GhostValid ? new Color(0.4f, 1f, 0.55f, 0.45f) : new Color(1f, 0.4f, 0.35f, 0.45f));
            onChanged?.Invoke();
        }
        public void RotateGhost() { if (ghost == null) return; ghostRot += 45; UpdateGhost(); }
        public void ConfirmGhost() { if (ghost == null) return; UpdateGhost(); if (!GhostValid) { GameUI.I?.Toast(GhostMsg); return; } var d = ghostDef; var p = ghostPos; float r = ghost.transform.eulerAngles.y; CancelGhost(); Commit(d, p, r); }
        public void CancelGhost() { if (ghost != null) Destroy(ghost); ghost = null; ghostDef = null; onChanged?.Invoke(); }

        // ---------- erase ----------
        void Erase(Vector2 pos)
        {
            if (!Physics.Raycast(RayAt(pos), out var hit, 200f)) return;
            var root = hit.collider.GetComponentInParent<Placed>();
            if (root == null) return;
            Economy.I?.Refund(root.defId, root.transform.position);
            game.Unregister(root.gameObject); Destroy(root.gameObject); Sound.I?.Pluck(5, 0.12f); onChanged?.Invoke();
        }
    }

    /// <summary>Marks a placed object (for erasing / saving).</summary>
    public class Placed : MonoBehaviour
    {
        public float block; public string defId, model; public float prog;
        public static readonly List<Placed> All = new List<Placed>();
        BuildDef cached; bool looked;
        public BuildDef def { get { if (!looked && defId != null) { cached = GameData.Load().Build(defId); looked = true; } return cached; } }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public static int Count(string id) { int n = 0; foreach (var p in All) if (p.defId == id) n++; return n; }
    }

    /// <summary>Little bounce when something is placed.</summary>
    public class GrowIn : MonoBehaviour
    {
        Vector3 s0; float t;
        void Start() { s0 = transform.localScale; transform.localScale = s0 * 0.01f; }
        void Update() { t += Time.deltaTime * 2.8f; float g = Mathf.Min(1, t); transform.localScale = s0 * Mathf.Max(0.01f, g * (1 + Mathf.Sin(g * Mathf.PI) * 0.2f)); if (g >= 1) { transform.localScale = s0; Destroy(this); } }
    }
}
