// Cồn Xanh – procedural audio (no files): sea, birds, crickets, đàn kìm on the ngũ cung scale, bell, UI plucks.
// VTA Games · Võ Thái Anh
using UnityEngine;

namespace ConXanh
{
    public class Sound : MonoBehaviour
    {
        public static Sound I;
        public bool on = true;
        const int SR = 44100;
        AudioSource waves, sfx;
        AudioClip[] notes, chirps; AudioClip cricket, bell;
        float musicT = 3, birdT = 2, crickT = 1;
        static readonly float[] Scale = { 261.6f, 293.7f, 349.2f, 392.0f, 440.0f, 523.3f, 587.3f, 698.5f };

        void Awake()
        {
            I = this;
            on = PlayerPrefs.GetInt("conxanh_sound", 1) == 1;
            if (FindFirstObjectByType<AudioListener>() == null && Camera.main) Camera.main.gameObject.AddComponent<AudioListener>();
            waves = gameObject.AddComponent<AudioSource>(); waves.clip = MakeWaves(); waves.loop = true; waves.volume = 0.32f; waves.Play();
            sfx = gameObject.AddComponent<AudioSource>();
            notes = new AudioClip[Scale.Length]; for (int i = 0; i < Scale.Length; i++) notes[i] = MakePluck(Scale[i]);
            chirps = new AudioClip[4]; for (int i = 0; i < chirps.Length; i++) chirps[i] = MakeChirp(2400 + i * 450, 2 + i % 3);
            cricket = MakeCricket(); bell = MakeBell();
            AudioListener.volume = on ? 1 : 0;
        }

        public void Toggle() { on = !on; AudioListener.volume = on ? 1 : 0; PlayerPrefs.SetInt("conxanh_sound", on ? 1 : 0); }

        // ---------- clip synthesis ----------
        static AudioClip Clip(string name, float[] d) { var c = AudioClip.Create(name, d.Length, 1, SR, false); c.SetData(d, 0); return c; }

        static AudioClip MakeWaves()
        {
            int n = SR * 8; var d = new float[n]; float l = 0, lp = 0;
            for (int i = 0; i < n; i++)
            {
                float w = Random.value * 2 - 1; l = (l + 0.02f * w) / 1.02f; lp += (l * 3.5f - lp) * 0.08f;
                float env = 0.55f + 0.45f * Mathf.Sin(i / (float)n * Mathf.PI * 4);  // two swells per loop
                d[i] = lp * env;
            }
            for (int i = 0; i < SR / 2; i++) { float k = i / (SR / 2f); d[i] = d[i] * k + d[n - SR / 2 + i] * (1 - k); } // smooth loop seam
            return Clip("waves", d);
        }
        static AudioClip MakePluck(float f)
        {
            int len = (int)(SR * 1.6f), p = Mathf.RoundToInt(SR / f); var d = new float[len]; var ring = new float[p];
            for (int i = 0; i < p; i++) ring[i] = Random.value * 2 - 1;
            for (int i = 0; i < len; i++) { int k = i % p; d[i] = ring[k] * 0.6f; ring[k] = 0.5f * (ring[k] + ring[(k + 1) % p]) * 0.994f; }
            return Clip("kim" + f, d);
        }
        static AudioClip MakeChirp(float f0, int count)
        {
            int one = (int)(SR * 0.07f), gap = (int)(SR * 0.04f); var d = new float[(one + gap) * count]; float ph = 0;
            for (int c = 0; c < count; c++) for (int i = 0; i < one; i++)
            {
                float t = i / (float)one; ph += 2 * Mathf.PI * (f0 * (1 + 0.35f * t)) / SR;
                d[c * (one + gap) + i] = Mathf.Sin(ph) * Mathf.Sin(Mathf.PI * t) * 0.35f;
            }
            return Clip("chirp" + f0, d);
        }
        static AudioClip MakeCricket()
        {
            int n = (int)(SR * 0.25f); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; float gate = (t % 0.05f) < 0.025f ? 1 : 0; d[i] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 4300 * t)) * 0.05f * gate * (1 - t / 0.25f); }
            return Clip("cricket", d);
        }
        static AudioClip MakeBell()
        {
            int n = (int)(SR * 1.4f); var d = new float[n]; float[] fs = { 2650, 3980, 5320 }, vs = { 0.5f, 0.3f, 0.15f };
            foreach (float start in new[] { 0f, 0.2f })
                for (int i = (int)(start * SR); i < n; i++)
                {
                    float t = i / (float)SR - start, s = 0;
                    for (int k = 0; k < 3; k++) s += Mathf.Sin(2 * Mathf.PI * fs[k] * t) * vs[k];
                    d[i] += s * Mathf.Exp(-t * 6f) * 0.5f;
                }
            return Clip("bell", d);
        }

        // ---------- playback ----------
        public void Pluck(int note, float vol = 0.25f) { if (notes != null) sfx.PlayOneShot(notes[Mathf.Clamp(note, 0, notes.Length - 1)], vol); }
        public void Place() { Pluck(0, 0.3f); Invoke(nameof(PlaceHi), 0.08f); }
        void PlaceHi() => Pluck(3, 0.22f);
        public void Bell() { sfx.PlayOneShot(bell, 0.5f); Invoke(nameof(Bell2), 0.6f); }
        void Bell2() => sfx.PlayOneShot(bell, 0.5f);

        void Update()
        {
            float dt = Time.deltaTime, night = DayNight.Night;
            waves.volume = 0.28f + 0.06f * Mathf.Sin(Time.time * 0.45f) + Weather.Rain * 0.45f;
            birdT -= dt; if (birdT <= 0) { birdT = Random.Range(2.5f, 7f); if (night < 0.3f) { sfx.panStereo = Random.Range(-0.8f, 0.8f); sfx.PlayOneShot(chirps[Random.Range(0, chirps.Length)], 0.35f); } }
            crickT -= dt; if (crickT <= 0) { crickT = Random.Range(0.3f, 1.2f); if (night > 0.5f) sfx.PlayOneShot(cricket, 0.5f * night); }
            musicT -= dt;
            if (musicT <= 0)
            {
                musicT = Random.Range(7f, 15f);
                StartCoroutine(Phrase());
            }
        }
        System.Collections.IEnumerator Phrase()
        {
            int n = Random.Range(2, 5), len = Random.Range(4, 8);
            for (int k = 0; k < len; k++)
            {
                n = Mathf.Clamp(n + new[] { -2, -1, 1, 2 }[Random.Range(0, 4)], 0, 7);
                Pluck(n, 0.16f);
                yield return new WaitForSeconds(new[] { 0.35f, 0.5f, 0.7f }[Random.Range(0, 3)]);
            }
        }
    }
}
