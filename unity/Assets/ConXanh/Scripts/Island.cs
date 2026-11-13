// Cồn Xanh – editable island terrain (port of the web version): land, pond, path brushes. VTA Games · Võ Thái Anh
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ConXanh
{
    /// <summary>Heightfield island that the player reshapes with drag brushes.</summary>
    public class Island : MonoBehaviour
    {
        public const float Size = 22f, Pond = 0.2f, Land = 0.5f;
        public const int Seg = 150, Cols = Seg + 1;
        public const float Step = Size / Seg;

        public float[] H, B;              // current height, base (pre-dig) height
        public byte[] carved, paint;      // pond mask, path mask
        Vector3[] verts; Color[] cols; Mesh mesh, pondMesh;
        bool dirty, pondDirty;

        static float Sm(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
        static float Noise(float x, float z) => Mathf.Sin(x * 0.9f + 1.3f) * Mathf.Cos(z * 0.7f - 0.4f) * 0.5f + Mathf.Sin(x * 2.1f - z * 1.7f) * 0.25f + Mathf.Sin(z * 3.3f + x * 0.4f) * 0.12f;
        static float Hash(int i) { uint h = (uint)i * 2654435761u; h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; return (h & 0xFFFFFF) / 16777216f; }

        static float BaseHeight(float x, float z)
        {
            float a = Mathf.Atan2(z, x);
            float R = 7.0f + Mathf.Sin(a * 3 + 0.5f) * 0.7f + Mathf.Sin(a * 5) * 0.35f;
            float d = Mathf.Sqrt(x * x + z * z) / R;
            float t = Sm(1.1f, 0.9f, d);
            float hill = Mathf.Exp(-((x + 2.5f) * (x + 2.5f) + (z + 2.2f) * (z + 2.2f)) / 6f) * 0.35f;
            return -0.7f + t * 1.2f + t * (Noise(x * 0.6f, z * 0.6f) * 0.07f + hill);
        }

        // ---------- queries ----------
        public float X(int i) => -Size / 2 + (i % Cols) * Step;
        public float Z(int i) => -Size / 2 + (i / Cols) * Step;
        public int Idx(float x, float z)
        {
            int c = Mathf.RoundToInt((x + Size / 2) / Step), r = Mathf.RoundToInt((z + Size / 2) / Step);
            return (c < 0 || c > Seg || r < 0 || r > Seg) ? -1 : r * Cols + c;
        }
        /// <summary>Smooth terrain height at world x,z.</summary>
        public float HeightAt(float x, float z)
        {
            float fx = (x + Size / 2) / Step, fz = (z + Size / 2) / Step;
            int c = Mathf.FloorToInt(fx), r = Mathf.FloorToInt(fz);
            if (c < 0 || c >= Seg || r < 0 || r >= Seg) return -0.7f;
            float tx = fx - c, tz = fz - r; int i = r * Cols + c;
            return Mathf.Lerp(Mathf.Lerp(H[i], H[i + 1], tx), Mathf.Lerp(H[i + Cols], H[i + Cols + 1], tx), tz);
        }
        public bool InPond(float x, float z) { int i = Idx(x, z); return i >= 0 && carved[i] == 1 && H[i] < B[i] - 0.03f; }
        public bool IsWater(float x, float z) { int i = Idx(x, z); return i < 0 || H[i] < 0.03f || InPond(x, z); }
        public bool IsLand(float x, float z) { int i = Idx(x, z); return i >= 0 && H[i] > 0.32f && !InPond(x, z); }
        /// <summary>Water close to the island (ponds or the shore) – where bridges make sense.</summary>
        public bool IsLandNear(float x, float z) { int i = Idx(x, z); return i >= 0 && (carved[i] == 1 || B[i] > -0.45f); }

        void ForEachNear(float x, float z, float rad, Action<int, float> fn)
        {
            int c0 = Mathf.RoundToInt((x + Size / 2) / Step), r0 = Mathf.RoundToInt((z + Size / 2) / Step), n = Mathf.CeilToInt(rad / Step) + 1;
            for (int r = r0 - n; r <= r0 + n; r++) for (int c = c0 - n; c <= c0 + n; c++)
            {
                if (r < 0 || r > Seg || c < 0 || c > Seg) continue;
                int i = r * Cols + c; float d = Mathf.Sqrt((X(i) - x) * (X(i) - x) + (Z(i) - z) * (Z(i) - z));
                if (d <= rad) fn(i, d);
            }
        }

        /// <summary>Ray vs terrain (or sea level beyond it). Returns false if nothing hit.</summary>
        public bool Raycast(Ray ray, out Vector3 hit)
        {
            hit = default; float t = 0, prev = 0;
            for (int k = 0; k < 900; k++)
            {
                var p = ray.origin + ray.direction * t;
                float g = Mathf.Max(HeightAt(p.x, p.z), -0.7f);
                if (p.y <= g)
                {
                    float lo = prev, hi = t;
                    for (int j = 0; j < 12; j++) { float m = (lo + hi) / 2; var q = ray.origin + ray.direction * m; if (q.y <= HeightAt(q.x, q.z)) hi = m; else lo = m; }
                    hit = ray.origin + ray.direction * hi; return true;
                }
                prev = t; t += Mathf.Max(0.04f, (p.y - g) * 0.5f);
                if (t > 200) break;
            }
            return false;
        }

        // ---------- colors ----------
        static readonly Color grass = new Color(0.40f, 0.74f, 0.24f), grass2 = new Color(0.28f, 0.60f, 0.18f), grass3 = new Color(0.55f, 0.80f, 0.28f);
        static readonly Color sand = new Color(0.93f, 0.84f, 0.60f), wetSand = new Color(0.82f, 0.71f, 0.48f), shallow = new Color(0.30f, 0.74f, 0.73f), deep = new Color(0.14f, 0.48f, 0.66f);
        static readonly Color stone = new Color(0.64f, 0.62f, 0.56f), mud = new Color(0.45f, 0.48f, 0.33f), path = new Color(0.86f, 0.74f, 0.54f), pathEdge = new Color(0.76f, 0.63f, 0.44f);

        Color ColorAt(int i)
        {
            float h = H[i], b = B[i], x = X(i), z = Z(i); Color c;
            if (b < 0.32f && carved[i] == 0)
            {
                if (h < -0.3f) c = Color.Lerp(deep, shallow, Sm(-0.7f, -0.3f, h));
                else if (h < 0.03f) c = Color.Lerp(shallow, wetSand, Sm(-0.3f, 0.03f, h));
                else c = Color.Lerp(wetSand, sand, Sm(0.03f, 0.12f, h));
                if (h > 0.26f) c = Color.Lerp(c, grass, Sm(0.26f, 0.4f, h));
            }
            else if (carved[i] == 1 && b - h > 0.035f) c = Color.Lerp(mud, stone, Sm(Pond - 0.05f, Pond + 0.05f, h));
            else
            {
                c = Color.Lerp(grass, grass2, Mathf.Clamp01(Noise(x * 1.6f, z * 1.6f) * 0.7f + 0.35f));
                c = Color.Lerp(c, grass3, Mathf.Clamp01(Noise(x * 0.5f + 3, z * 0.5f) * 0.6f) * 0.5f);
            }
            if (paint[i] == 1 && h > Pond)
            {
                int cc = i % Cols; bool edge = (cc > 0 && paint[i - 1] == 0) || (cc < Seg && paint[i + 1] == 0) || (i >= Cols && paint[i - Cols] == 0) || (i + Cols < H.Length && paint[i + Cols] == 0);
                c = Color.Lerp(edge ? pathEdge : path, Color.white, (Hash(i) - 0.5f) * 0.06f);
            }
            return c;
        }

        // ---------- brushes ----------
        public bool landAdded;
        public void BrushLand(float x, float z)
        {
            const float R = 1.1f;
            ForEachNear(x, z, R, (i, d) =>
            {
                if (X(i) * X(i) + Z(i) * Z(i) > 10.4f * 10.4f) return;
                float f = Sm(R, R * 0.35f, d), nh;
                if (carved[i] == 1 && H[i] < B[i] - 0.01f) { nh = Mathf.Min(B[i], H[i] + 0.06f * f); if (nh >= B[i] - 0.015f) carved[i] = 0; }
                else if (H[i] < Land - 0.02f) { nh = H[i] + (Land + Noise(X(i), Z(i)) * 0.03f - H[i]) * f; if (f > 0.02f) landAdded = true; }
                else nh = Mathf.Min(H[i] + 0.025f * f, 1.5f);
                if (nh > H[i]) { H[i] = nh; if (carved[i] == 0) B[i] = Mathf.Max(B[i], nh); }
            });
            Recolor(x, z, R + Step * 2); pondDirty = true;
        }
        public void BrushPond(float x, float z)
        {
            const float R = 1.0f; float target = Pond - 0.17f;
            ForEachNear(x, z, R * 1.2f, (i, d) =>
            {
                if (B[i] < 0.34f) return;
                if (d <= R) { float f = Sm(R, R * 0.45f, d), nh = B[i] + (target - B[i]) * f; if (nh < H[i]) H[i] = nh; paint[i] = 0; }
                carved[i] = 1;
            });
            Recolor(x, z, R * 1.5f); pondDirty = true;
        }
        public void BrushPath(float x, float z)
        {
            const float R = 0.36f;
            ForEachNear(x, z, R, (i, d) => { if (B[i] > 0.3f && H[i] > Pond + 0.02f) { paint[i] = 1; H[i] = Mathf.Min(H[i], B[i] - 0.02f * Sm(R, 0, d)); } });
            Recolor(x, z, R + Step * 2);
        }
        /// <summary>Levels the ground under a building.</summary>
        public void Flatten(float x, float z, float r, float y)
        {
            ForEachNear(x, z, r, (i, d) => { if (carved[i] == 1) return; float f = Sm(r, r * 0.55f, d); H[i] = Mathf.Lerp(H[i], y, f); B[i] = H[i]; if (d < r * 0.6f) paint[i] = 0; });
            Recolor(x, z, r + Step * 2);
        }
        void Recolor(float x, float z, float r) { ForEachNear(x, z, r, (i, d) => cols[i] = ColorAt(i)); dirty = true; }

        // ---------- mesh ----------
        public void Build()
        {
            int n = Cols * Cols;
            H = new float[n]; B = new float[n]; carved = new byte[n]; paint = new byte[n]; verts = new Vector3[n]; cols = new Color[n];
            for (int i = 0; i < n; i++) { H[i] = B[i] = BaseHeight(X(i), Z(i)); }
            for (int i = 0; i < n; i++) { verts[i] = new Vector3(X(i), H[i], Z(i)); cols[i] = ColorAt(i); }
            var tris = new int[Seg * Seg * 6];
            for (int r = 0, t = 0; r < Seg; r++) for (int c = 0; c < Seg; c++)
            {
                int a = r * Cols + c, b = a + Cols;
                tris[t++] = a; tris[t++] = b; tris[t++] = a + 1; tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
            }
            mesh = new Mesh { indexFormat = IndexFormat.UInt32, name = "Island" };
            mesh.MarkDynamic(); mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("ConXanh/VertexColorLit"));
            mr.shadowCastingMode = ShadowCastingMode.On;

            // pond water surface (rebuilt from the carved mask)
            var pg = new GameObject("PondWater");
            pondMesh = new Mesh { indexFormat = IndexFormat.UInt32, name = "Pond" }; pondMesh.MarkDynamic();
            pg.AddComponent<MeshFilter>().sharedMesh = pondMesh;
            var pr = pg.AddComponent<MeshRenderer>(); pr.shadowCastingMode = ShadowCastingMode.Off;
            var pm = new Material(Shader.Find("ConXanh/Water"));
            pm.SetColor("_Shallow", new Color(0.45f, 0.86f, 0.9f, 0.5f)); pm.SetColor("_Deep", new Color(0.18f, 0.6f, 0.72f, 0.85f));
            pm.SetFloat("_DepthRange", 0.3f); pm.SetFloat("_FoamWidth", 0.06f); pm.SetFloat("_WaveHeight", 0.004f);
            pr.sharedMaterial = pm;

            var bed = GameObject.CreatePrimitive(PrimitiveType.Plane);
            bed.name = "SeaBed"; Destroy(bed.GetComponent<Collider>());
            bed.transform.position = new Vector3(0, -0.72f, 0); bed.transform.localScale = Vector3.one * 12f;
            var bm = new Material(Shader.Find("Universal Render Pipeline/Lit")); bm.color = new Color(0.1f, 0.42f, 0.6f);
            bed.GetComponent<Renderer>().sharedMaterial = bm;
        }

        void LateUpdate()
        {
            if (dirty)
            {
                for (int i = 0; i < verts.Length; i++) verts[i].y = H[i];
                mesh.vertices = verts; mesh.colors = cols; mesh.RecalculateNormals(); mesh.RecalculateBounds(); dirty = false;
            }
            if (pondDirty) { RebuildPond(); pondDirty = false; }
        }

        void RebuildPond()
        {
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            for (int r = 0; r < Seg; r++) for (int c = 0; c < Seg; c++)
            {
                int a = r * Cols + c;
                if (carved[a] == 0 && carved[a + 1] == 0 && carved[a + Cols] == 0 && carved[a + Cols + 1] == 0) continue;
                int b = v.Count;
                v.Add(new Vector3(X(a), Pond, Z(a))); v.Add(new Vector3(X(a + 1), Pond, Z(a + 1)));
                v.Add(new Vector3(X(a + Cols), Pond, Z(a + Cols))); v.Add(new Vector3(X(a + Cols + 1), Pond, Z(a + Cols + 1)));
                t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b + 1); t.Add(b + 2); t.Add(b + 3);
            }
            pondMesh.Clear(); pondMesh.SetVertices(v); pondMesh.SetTriangles(t, 0); pondMesh.RecalculateNormals(); pondMesh.RecalculateBounds();
        }
    }
}
