// Cồn Xanh – seasons, rain, grey sky, flood water (mùa nước nổi) and a rainbow after the rain.
// VTA Games · Võ Thái Anh
using UnityEngine;
using UnityEngine.Rendering;

namespace ConXanh
{
    public class Weather : MonoBehaviour
    {
        public static Weather I;
        public static float Rain, Flood;
        public static SeasonDef Season;
        public int seasonIdx;
        public bool raining;

        Light sun; GameData gd; ParticleSystem rainPs; Transform sea, pond;
        float rainLeft, nextCheck = 8, rainbowT; Material rainbowMat; GameObject rainbow;

        public void Init(Light s)
        {
            I = this; sun = s; gd = GameData.Load(); Rain = 0; Flood = 0;
            Season = gd.seasons != null && gd.seasons.Length > 0 ? gd.seasons[0] : null;
            BuildRain(); BuildRainbow();
        }

        void Start()
        {
            sea = GameObject.Find("Water")?.transform; pond = GameObject.Find("PondWater")?.transform;
            if (DayNight.I != null) DayNight.I.onNewDay += OnNewDay;
            UpdateSeason(false);
        }

        /// <summary>Season comes from the day number: kho → mua → nuocnoi → kho …</summary>
        public void UpdateSeason(bool announce)
        {
            if (gd.seasons == null || gd.seasons.Length == 0 || DayNight.I == null) return;
            int total = 0; foreach (var s in gd.seasons) total += Mathf.Max(1, s.days);
            int d = (DayNight.I.day - 1) % total, idx = 0;
            while (d >= Mathf.Max(1, gd.seasons[idx].days)) { d -= Mathf.Max(1, gd.seasons[idx].days); idx++; }
            bool changed = idx != seasonIdx || Season != gd.seasons[idx];
            seasonIdx = idx; Season = gd.seasons[idx];
            if (changed && announce)
            {
                string extra = Season.id == "nuocnoi" ? " – nước lên, cá về nhiều, ruộng thấp bị ngập" : Season.id == "mua" ? " – hay mưa, rau tre tốt" : " – nắng ráo, lúa trúng";
                GameUI.I?.Toast("Sang " + Season.name + extra, 4f);
            }
        }
        void OnNewDay() { UpdateSeason(true); }

        void Update()
        {
            float dt = Time.deltaTime;
            // decide rain now and then
            nextCheck -= dt;
            if (!raining && nextCheck <= 0)
            {
                nextCheck = Random.Range(20f, 40f);
                if (Season != null && Random.value < Season.rain * 0.5f) { raining = true; rainLeft = Random.Range(25f, 60f); GameUI.I?.Toast("Trời đổ mưa rồi"); }
            }
            if (raining) { rainLeft -= dt; if (rainLeft <= 0) { raining = false; if (DayNight.Night < 0.3f) rainbowT = 22f; } }
            Rain = Mathf.MoveTowards(Rain, raining ? 1 : 0, dt / 4f);
            Flood = Mathf.MoveTowards(Flood, Season != null ? Season.flood : 0, dt * 0.004f);

            var em = rainPs.emission; em.rateOverTime = Rain * 900;
            var c = Camera.main; if (c) { var oc = c.GetComponent<OrbitCam>(); rainPs.transform.position = (oc ? oc.target : Vector3.zero) + Vector3.up * 7; }
            if (sea) sea.position = new Vector3(0, Flood, 0);
            if (pond) pond.position = new Vector3(0, Flood * 0.4f, 0);

            // rainbow fades in after rain, opposite the sun
            rainbowT -= dt;
            float a = Mathf.Clamp01(rainbowT / 4f) * Mathf.Clamp01((22f - rainbowT) / 3f) * (1 - DayNight.Night);
            rainbow.SetActive(a > 0.01f);
            if (a > 0.01f && c)
            {
                var away = -sun.transform.forward; away.y = 0; if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                var fwd = c.transform.forward; fwd.y = 0; fwd.Normalize();
                var dir = Vector3.Slerp(fwd, -away.normalized, 0.3f).normalized;
                rainbow.transform.position = new Vector3(0, -2f, 0) + dir * 30f;
                rainbow.transform.rotation = Quaternion.LookRotation(dir);
                rainbowMat.SetFloat("_Alpha", a * 0.75f);
            }
        }

        void LateUpdate()
        {
            // grey sky while raining (DayNight sets the clear values every frame, so this does not compound)
            if (Rain <= 0.001f) return;
            sun.intensity *= Mathf.Lerp(1, 0.5f, Rain);
            var grey = new Color(0.55f, 0.6f, 0.64f) * Mathf.Lerp(1, 0.25f, DayNight.Night);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, grey, Rain * 0.7f);
            RenderSettings.ambientIntensity *= Mathf.Lerp(1, 0.8f, Rain);
            if (Camera.main) Camera.main.backgroundColor = RenderSettings.fogColor;
        }

        void BuildRain()
        {
            var go = new GameObject("Rain"); rainPs = go.AddComponent<ParticleSystem>();
            rainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rainPs.main; main.startLifetime = 0.9f; main.startSpeed = 11f; main.startSize = 0.03f; main.maxParticles = 1500;
            main.startColor = new Color(0.8f, 0.88f, 1f, 0.5f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.loop = true; main.playOnAwake = true;
            var sh = rainPs.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(22, 22, 0.1f); sh.rotation = new Vector3(90, 0, 0);
            var em = rainPs.emission; em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 1.2f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var sh2 = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh2) { var m = new Material(sh2); m.SetFloat("_Surface", 1); m.SetColor("_BaseColor", new Color(0.85f, 0.9f, 1f, 0.45f)); m.renderQueue = 3000;
                m.SetOverrideTag("RenderType", "Transparent"); m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); r.sharedMaterial = m; }
            rainPs.Play();
        }

        /// <summary>A flat half-ring mesh; the shader paints the colour bands from uv.x.</summary>
        void BuildRainbow()
        {
            const int seg = 64; const float R = 18f, W = 2.2f;
            var v = new Vector3[(seg + 1) * 2]; var uv = new Vector2[v.Length]; var t = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * i / seg; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v[i * 2] = d * (R - W); v[i * 2 + 1] = d * R; uv[i * 2] = new Vector2(0, i / (float)seg); uv[i * 2 + 1] = new Vector2(1, i / (float)seg);
            }
            for (int i = 0, k = 0; i < seg; i++) { int a = i * 2; t[k++] = a; t[k++] = a + 1; t[k++] = a + 2; t[k++] = a + 1; t[k++] = a + 3; t[k++] = a + 2; }
            var m = new Mesh { vertices = v, uv = uv, triangles = t, name = "Rainbow" }; m.RecalculateBounds();
            rainbow = new GameObject("Rainbow"); rainbow.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = rainbow.AddComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
            rainbowMat = new Material(Shader.Find("ConXanh/Rainbow")); mr.sharedMaterial = rainbowMat;
            rainbow.SetActive(false);
        }
    }
}
