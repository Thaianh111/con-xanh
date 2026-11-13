// Cồn Xanh – save / load the village as JSON in persistentDataPath, with autosave. VTA Games · Võ Thái Anh
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ConXanh
{
    [Serializable] public class SaveObj { public string id, model; public Vector3 p, s; public Quaternion r; public float prog; }

    [Serializable]
    public class SaveFile
    {
        public int ver = 1;
        public string[] resKeys; public float[] resVals;
        public float pop, happy, time, flood; public int day, goal, rank, ordersDone;
        public string h, b, carved, paint;
        public SaveObj[] objs;
    }

    public class SaveLoad : MonoBehaviour
    {
        public static SaveLoad I;
        public float autosaveEvery = 30f;
        static string FilePath => Path.Combine(Application.persistentDataPath, "conxanh_save.json");
        float t; bool ready;

        void Awake() { I = this; }

        IEnumerator Start()
        {
            yield return null;   // let the other systems finish Start
            bool loaded = false;
            try { loaded = Load(); } catch (Exception e) { Debug.LogWarning("Không đọc được bản lưu: " + e.Message); }
            if (!loaded) { ConXanhGame.I.NewVillage(); GameUI.I?.Toast("Chào mừng về Cồn Xanh! Làm theo mục tiêu ở trên nha", 4f); }
            else GameUI.I?.Toast("Đã mở lại làng – ngày " + DayNight.I.day, 3f);
            ConXanhGame.I.SpawnAmbient();
            Weather.I?.UpdateSeason(false);
            Builder.I.onChanged?.Invoke();
            ready = true;
        }

        void Update() { if (!ready) return; t += Time.unscaledDeltaTime; if (t >= autosaveEvery) { t = 0; Save(); } }
        void OnApplicationPause(bool p) { if (p && ready) Save(); }
        void OnApplicationQuit() { if (ready) Save(); }

        public void Save()
        {
            var e = Economy.I; var isl = ConXanhGame.I.island; var dn = DayNight.I;
            var f = new SaveFile
            {
                resKeys = Economy.Keys, resVals = Array.ConvertAll(Economy.Keys, k => e.Get(k)),
                pop = e.pop, happy = e.happy, goal = e.goal, rank = e.rank, ordersDone = e.ordersDone,
                day = dn.day, time = dn.time, flood = Weather.Flood,
                h = Floats(isl.H), b = Floats(isl.B), carved = Convert.ToBase64String(isl.carved), paint = Convert.ToBase64String(isl.paint),
            };
            var list = new List<SaveObj>();
            foreach (var p in Placed.All)
            {
                if (string.IsNullOrEmpty(p.defId)) continue;
                var tr = p.transform;
                list.Add(new SaveObj { id = p.defId, model = p.model, p = tr.position, r = tr.rotation, s = tr.localScale, prog = p.prog });
            }
            f.objs = list.ToArray();
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(f)); }
            catch (Exception ex) { Debug.LogWarning("Không lưu được: " + ex.Message); }
        }

        bool Load()
        {
            if (!File.Exists(FilePath)) return false;
            var f = JsonUtility.FromJson<SaveFile>(File.ReadAllText(FilePath));
            if (f == null || f.objs == null) return false;
            var e = Economy.I; var g = ConXanhGame.I; var bld = Builder.I;
            for (int i = 0; i < f.resKeys.Length && i < f.resVals.Length; i++) e.res[f.resKeys[i]] = f.resVals[i];
            e.pop = f.pop; e.happy = f.happy; e.goal = f.goal; e.rank = f.rank; e.ordersDone = f.ordersDone;
            DayNight.I.day = Mathf.Max(1, f.day); DayNight.I.time = f.time; Weather.Flood = f.flood;
            g.island.Restore(ToFloats(f.h), ToFloats(f.b), Convert.FromBase64String(f.carved), Convert.FromBase64String(f.paint));
            foreach (var o in f.objs)
            {
                if (o.id == "cau_khi") { bld.RestoreBridge(o); continue; }
                if (bld.defs.TryGetValue(o.id, out var d)) bld.Commit(d, o.p, o.r.eulerAngles.y, true, o);
            }
            return true;
        }

        /// <summary>Deletes the save and starts a fresh village in place.</summary>
        public void NewGame()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
            var g = ConXanhGame.I;
            Builder.I.CancelGhost();
            foreach (var p in Placed.All.ToArray()) { g.Unregister(p.gameObject); Destroy(p.gameObject); }
            g.island.ResetTerrain();
            Economy.I.ResetState();
            DayNight.I.day = 1; DayNight.I.time = 0.3f; Weather.Flood = 0;
            g.ClearVillagers();
            StartCoroutine(Fresh());
        }
        IEnumerator Fresh()
        {
            yield return null;   // let Destroy finish so Placed.All is empty
            ConXanhGame.I.NewVillage(); Weather.I?.UpdateSeason(false); Builder.I.onChanged?.Invoke();
            GameUI.I?.Toast("Làng mới đã sẵn sàng!", 3f); Save();
        }

        static string Floats(float[] a) { var bytes = new byte[a.Length * 4]; Buffer.BlockCopy(a, 0, bytes, 0, bytes.Length); return Convert.ToBase64String(bytes); }
        static float[] ToFloats(string s) { var bytes = Convert.FromBase64String(s); var a = new float[bytes.Length / 4]; Buffer.BlockCopy(bytes, 0, a, 0, bytes.Length); return a; }
    }
}
