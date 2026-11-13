// Cồn Xanh – day/night cycle with warm sunset and moonlit night. VTA Games · Võ Thái Anh
using UnityEngine;

namespace ConXanh
{
    public class DayNight : MonoBehaviour
    {
        public float dayLength = 240f;     // giây cho một ngày
        [Range(0, 1)] public float time = 0.3f;
        public bool auto = true;
        public static float Night;
        public static DayNight I;
        void Awake() { I = this; }
        Light sun; Light[] lamps;
        static readonly Color sunDay = new Color(1f, 0.89f, 0.74f), sunDusk = new Color(1f, 0.6f, 0.36f), sunNight = new Color(0.45f, 0.55f, 1f);
        static readonly Color fogDay = new Color(0.66f, 0.84f, 0.98f), fogNight = new Color(0.05f, 0.09f, 0.2f);

        public void Init(Light s)
        {
            sun = s;
            // small warm lamps at houses that glow at night
            var list = new System.Collections.Generic.List<Light>();
            foreach (var n in new[] { "nha_la", "nha_ngoi", "dinh", "chua", "cho" })
            {
                var go = GameObject.Find(n); if (go == null) continue;
                var l = new GameObject("Lamp").AddComponent<Light>(); l.type = LightType.Point; l.range = 2.2f; l.color = new Color(1f, 0.7f, 0.35f);
                l.transform.position = go.transform.position + go.transform.forward * 0.7f + Vector3.up * 0.7f; list.Add(l);
            }
            lamps = list.ToArray();
        }

        void Update()
        {
            if (InputCompat.KeyT()) auto = !auto;
            if (auto) time = (time + Time.deltaTime / dayLength) % 1f;
            // 0..0.5 day, 0.5..0.6 dusk, 0.6..0.95 night, 0.95..1 dawn
            float night = Mathf.Clamp01(Mathf.InverseLerp(0.52f, 0.62f, time)) * (1 - Mathf.Clamp01(Mathf.InverseLerp(0.93f, 1f, time)));
            float dusk = Mathf.Clamp01(1 - Mathf.Abs(time - 0.55f) / 0.06f) + Mathf.Clamp01(1 - Mathf.Abs(time - 0.97f) / 0.04f);
            float elev = Mathf.Lerp(55f, 12f, Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.55f, time)));
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(elev, 40f, night), -35f + time * 140f, 0);
            sun.color = Color.Lerp(Color.Lerp(sunDay, sunDusk, Mathf.Clamp01(dusk)), sunNight, night);
            sun.intensity = Mathf.Lerp(1.6f, 0.28f, night);
            RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.35f, night);
            RenderSettings.fogColor = Color.Lerp(fogDay, fogNight, night);
            if (Camera.main) { Camera.main.backgroundColor = RenderSettings.fogColor; }
            foreach (var l in lamps) l.intensity = night * 2.2f;
            Night = night;
        }

    }
}
