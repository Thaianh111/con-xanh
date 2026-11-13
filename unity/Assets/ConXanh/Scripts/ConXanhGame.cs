// Cồn Xanh – Unity URP sample scene (GĐ3, bước 1). VTA Games · Võ Thái Anh
// Press Play in any scene: the bootstrap builds the island, water, lighting, post FX and the village.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ConXanh
{
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Object.FindFirstObjectByType<ConXanhGame>() == null)
                new GameObject("ConXanh").AddComponent<ConXanhGame>();
        }
    }

    /// <summary>Builds and runs the sample village scene.</summary>
    public class ConXanhGame : MonoBehaviour
    {
        public static ConXanhGame I;
        public Island island;
        public readonly List<Obstacle> obstacles = new List<Obstacle>();
        public Builder builder;
        GameObject pack;
        Camera cam;
        Light sun;

        public class Obstacle { public Vector3 p; public float r; public GameObject go; }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            SetupCamera();
            SetupLight();
            SetupPost();
            island = new GameObject("Island").AddComponent<Island>();
            island.Build();
            BuildWater();
            pack = Resources.Load<GameObject>("Models/thien_nhien_pack");
            builder = gameObject.AddComponent<Builder>();
            Populate();
            gameObject.AddComponent<DayNight>().Init(sun);
            gameObject.AddComponent<Sound>();
            gameObject.AddComponent<GameUI>();
        }

        // ---------- camera / light / post ----------
        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.fieldOfView = Screen.height > Screen.width ? 55 : 40;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.requiresDepthTexture = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var oc = cam.GetComponent<OrbitCam>() ?? cam.gameObject.AddComponent<OrbitCam>();
            oc.target = new Vector3(0, 0.3f, 0);
        }

        void SetupLight()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) { sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; }
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.color = new Color(1f, 0.89f, 0.74f);
            sun.intensity = 1.6f;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) urp.shadowDistance = 45f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.76f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.62f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.36f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 90;
            RenderSettings.fogColor = new Color(0.66f, 0.84f, 0.98f);
        }

        void SetupPost()
        {
            var vol = new GameObject("PostFX").AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 50;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bl = prof.Add<Bloom>(true); bl.intensity.Override(0.55f); bl.threshold.Override(1.0f); bl.scatter.Override(0.65f);
            var ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.05f); ca.saturation.Override(18f); ca.contrast.Override(10f);
            var wb = prof.Add<WhiteBalance>(true); wb.temperature.Override(6f);
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.2f); vg.smoothness.Override(0.5f);
            vol.sharedProfile = prof;
        }

        // ---------- water ----------
        void BuildWater()
        {
            const int n = 160; const float size = 90f;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var tris = new int[n * n * 6];
            for (int z = 0, i = 0; z <= n; z++) for (int x = 0; x <= n; x++, i++) verts[i] = new Vector3(-size / 2 + x * size / n, 0, -size / 2 + z * size / n);
            for (int z = 0, t = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                int a = z * (n + 1) + x, b = a + n + 1;
                tris[t++] = a; tris[t++] = b; tris[t++] = a + 1; tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32, vertices = verts, triangles = tris };
            m.RecalculateNormals(); m.RecalculateBounds();
            var go = new GameObject("Water");
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("ConXanh/Water"));
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- helpers ----------
        public GameObject Spawn(string model, Vector3 pos, float rotY = 0, float scale = 1, float block = 0)
        {
            var src = Resources.Load<GameObject>("Models/" + model);
            if (src == null) { Debug.LogWarning("Thiếu model " + model); return null; }
            var go = Instantiate(src, pos, Quaternion.Euler(0, rotY, 0));
            go.name = model; go.transform.localScale = Vector3.one * scale;
            SetShadows(go);
            if (block > 0) Register(go, block);
            return go;
        }

        public GameObject SpawnPack(string node, Vector3 pos, float scale, float rotY = -1)
        {
            if (pack == null) return null;
            var t = FindDeep(pack.transform, node);
            if (t == null) { Debug.LogWarning("Thiếu cây " + node); return null; }
            var go = Instantiate(t.gameObject, pos, Quaternion.Euler(0, rotY < 0 ? Random.Range(0, 360f) : rotY, 0) * t.localRotation);
            go.transform.localScale = t.localScale * scale;
            SetShadows(go);
            go.AddComponent<Sway>();
            return go;
        }

        /// <summary>Marks a placed object: blocks walking and can be erased.</summary>
        public void Register(GameObject go, float block)
        {
            var pl = go.GetComponent<Placed>() ?? go.AddComponent<Placed>(); pl.block = block;
            if (block > 0) obstacles.Add(new Obstacle { p = go.transform.position, r = block, go = go });
            AddPicker(go);
        }
        public void Unregister(GameObject go) { obstacles.RemoveAll(o => o.go == go); }

        /// <summary>Adds a box collider around the object's visuals so it can be tapped.</summary>
        public void AddPicker(GameObject go)
        {
            if (go.GetComponent<Placed>() == null) go.AddComponent<Placed>();
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var pick = new GameObject("Pick"); pick.transform.SetParent(go.transform, false);
            pick.transform.position = b.center; pick.transform.rotation = Quaternion.identity;
            var ls = go.transform.lossyScale; pick.transform.localScale = new Vector3(1 / ls.x, 1 / ls.y, 1 / ls.z);
            pick.AddComponent<BoxCollider>().size = b.size;
        }

        /// <summary>Buffalo + farmer plowing a rice field.</summary>
        public void AddPlowTeam(GameObject field)
        {
            var buf = Spawn("trau", field.transform.position + field.transform.right * -0.8f + Vector3.up * 0.03f, field.transform.eulerAngles.y + 90);
            if (buf) { var p = buf.AddComponent<Plow>(); p.field = field.transform; }
            var farmer = Spawn("nguoi_nam_2", field.transform.position + field.transform.right * -1.2f + Vector3.up * 0.03f, field.transform.eulerAngles.y + 90);
            if (farmer && buf) { var p = farmer.AddComponent<Plow>(); p.field = field.transform; p.leader = buf.transform; }
        }

        static void SetShadows(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        }

        public static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
            return null;
        }

        public bool Walkable(Vector3 p)
        {
            if (island.HeightAt(p.x, p.z) < 0.12f) return false;
            foreach (var o in obstacles) if ((new Vector2(o.p.x - p.x, o.p.z - p.z)).sqrMagnitude < o.r * o.r) return false;
            return true;
        }

        Vector3 Ground(float x, float z) => new Vector3(x, island.HeightAt(x, z), z);

        Vector3 ShorePoint(float angle, float extra)
        {
            for (float r = 4; r < 11; r += 0.1f)
            {
                float x = Mathf.Cos(angle) * r, z = Mathf.Sin(angle) * r;
                if (island.HeightAt(x, z) < -0.25f) return new Vector3(Mathf.Cos(angle) * (r + extra), 0, Mathf.Sin(angle) * (r + extra));
            }
            return new Vector3(9, 0, 4);
        }

        // ---------- village ----------
        void Populate()
        {
            Random.InitState(42);
            // buildings (same layout spirit as the web prototype)
            Spawn("nha_la", Ground(2.4f, -1.4f), 200, 1, 0.8f);
            Spawn("nha_ngoi", Ground(-1.6f, -2.6f), 160, 1, 0.9f);
            Spawn("dinh", Ground(-0.6f, 1.0f), 180, 1, 1.2f);
            Spawn("chua", Ground(-3.9f, 0.6f), 120, 1, 1.2f);
            Spawn("truong", Ground(1.0f, -3.9f), 190, 1, 1.1f);
            Spawn("cho", Ground(2.0f, 1.3f), 150, 1, 1.0f);
            Spawn("lo_keo", Ground(3.8f, 0.4f), 230, 1, 0.7f);
            Spawn("vuon_rau", Ground(-3.4f, -2.0f), 140, 1, 0.9f);
            var field = Spawn("ruong", Ground(0.6f, 3.6f), 180, 1, 1.4f);
            Spawn("lu_nuoc", Ground(1.5f, -0.8f), 0, 1, 0.12f);
            for (int k = 0; k < 2; k++) Spawn("bui_tre_" + (k + 1), Ground(-5.0f + k * 0.6f, -0.9f + k * 0.5f), Random.Range(0, 360f), 1, 0.3f);
            for (int k = 0; k < 4; k++) { float a = 2.0f + k * 0.5f; Spawn("chuoi_" + (k % 2 + 1), Ground(Mathf.Cos(a) * 4.6f, Mathf.Sin(a) * 4.6f), Random.Range(0, 360f), 1, 0.2f); }

            // fishery on the shore, facing the sea
            float fa = -0.9f; var shore = ShorePoint(fa, -0.9f);
            Spawn("ben_ca", Ground(shore.x, shore.z), Mathf.Atan2(Mathf.Cos(fa), Mathf.Sin(fa)) * Mathf.Rad2Deg, 1, 0.4f);

            // palms + nipa around the beach, trees inland (Quaternius CC0)
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2 + Random.Range(-0.2f, 0.2f);
                var p = ShorePoint(a, -1.3f - Random.Range(0, 0.4f));
                if (k % 3 == 0) Spawn("dua_nuoc_" + (k % 2 + 1), Ground(p.x, p.z), Random.Range(0, 360f), 0.8f);
                else SpawnPack("PalmTree_" + (k % 3 + 1), Ground(p.x, p.z), 0.4f);
            }
            string[] trees = { "NormalTree_3", "PineTree_1", "MapleTree_1", "NormalTree_3" };
            float[] tscale = { 0.34f, 0.4f, 0.26f, 0.3f };
            for (int k = 0; k < 7; k++)
            {
                for (int tries = 0; tries < 20; tries++)
                {
                    float a = Random.Range(0, Mathf.PI * 2), r = Random.Range(3.5f, 5.4f);
                    var p = Ground(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                    if (!Walkable(p)) continue;
                    var tree = SpawnPack(trees[k % 4], p, tscale[k % 4]); if (tree) Register(tree, 0.25f); break;
                }
            }
            for (int k = 0; k < 60; k++)
            {
                float a = Random.Range(0, Mathf.PI * 2), r = Random.Range(0.5f, 5.6f);
                var p = Ground(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                if (!Walkable(p)) continue;
                SpawnPack(k % 3 == 0 ? "Flower_3_Clump" : k % 3 == 1 ? "Grass_Small" : "Flower_4_Clump", p, 0.35f);
            }
            for (int k = 0; k < 6; k++) { float a = Random.Range(0, Mathf.PI * 2); var p = Ground(Mathf.Cos(a) * 3.2f, Mathf.Sin(a) * 3.2f); if (Walkable(p)) SpawnPack("Bush_Flowers", p, 0.5f); }

            // boats on the water
            var b1 = Spawn("xuong", ShorePoint(0.5f, 0.6f), 30); if (b1) b1.AddComponent<Bob>();
            var tr = Spawn("ghe_thuong_lai", ShorePoint(2.6f, 1.4f), 110); if (tr) tr.AddComponent<TraderBoat>().dock = ShorePoint(2.6f, 1.4f);
            for (int k = 0; k < 5; k++) { var d = Spawn("vit", ShorePoint(1.2f + k * 0.12f, 0.5f + k * 0.15f), Random.Range(0, 360f)); if (d) d.AddComponent<Bob>().amp = 0.01f; }

            // people, husky, buffalo, chickens
            string[] people = { "nguoi_nam", "nguoi_nu", "nguoi_nu_2", "nguoi_nam_2", "nguoi_nam", "nguoi_nu" };
            Walker lead = null;
            for (int k = 0; k < people.Length; k++)
            {
                var p = FindWalkable(new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f)));
                var go = Spawn(people[k], p, Random.Range(0, 360f));
                if (go == null) continue;
                var w = go.AddComponent<Walker>(); w.speed = 0.55f; w.wanderRadius = 3.5f;
                if (lead == null) lead = w;
            }
            var dog = Spawn("husky", FindWalkable(new Vector3(1.6f, 0, -0.3f)), 0);
            if (dog) { var w = dog.AddComponent<Walker>(); w.quad = true; w.follow = lead; w.speed = 1.2f; }
            for (int k = 0; k < 4; k++)
            {
                var ch = Spawn(k % 2 == 0 ? "ga" : "ga_nau", FindWalkable(new Vector3(2.6f + Random.Range(-1f, 1f), 0, -0.4f + Random.Range(-1f, 1f))), Random.Range(0, 360f));
                if (ch) { var w = ch.AddComponent<Walker>(); w.speed = 0.4f; w.wanderRadius = 1.0f; w.peck = true; }
            }
            if (field != null) AddPlowTeam(field);
        }

        Vector3 FindWalkable(Vector3 p)
        {
            for (float r = 0; r < 4; r += 0.2f)
                for (float a = 0; a < Mathf.PI * 2; a += 0.6f)
                {
                    var q = Ground(p.x + Mathf.Cos(a) * r, p.z + Mathf.Sin(a) * r);
                    if (Walkable(q)) return q;
                }
            return Ground(p.x, p.z);
        }
    }
}
