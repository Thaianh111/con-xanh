// Cồn Xanh – simple procedural animation for people and animals. VTA Games · Võ Thái Anh
using UnityEngine;

namespace ConXanh
{
    /// <summary>Swings named limb pivots (LegL/LegR/ArmL/ArmR, LegFL…) like the web prototype.</summary>
    public class LimbRig
    {
        readonly Transform legL, legR, armL, armR, fl, fr, bl, br, head, body, tail;
        public LimbRig(Transform root)
        {
            legL = ConXanhGame.FindDeep(root, "LegL"); legR = ConXanhGame.FindDeep(root, "LegR");
            armL = ConXanhGame.FindDeep(root, "ArmL"); armR = ConXanhGame.FindDeep(root, "ArmR");
            fl = ConXanhGame.FindDeep(root, "LegFL"); fr = ConXanhGame.FindDeep(root, "LegFR");
            bl = ConXanhGame.FindDeep(root, "LegBL"); br = ConXanhGame.FindDeep(root, "LegBR");
            head = ConXanhGame.FindDeep(root, "Head"); body = ConXanhGame.FindDeep(root, "Body"); tail = ConXanhGame.FindDeep(root, "Tail");
        }
        static void X(Transform t, float deg) { if (t != null) t.localRotation = Quaternion.Euler(deg, 0, 0); }
        public void Walk(float phase, float amount)
        {
            float s = Mathf.Sin(phase) * amount;
            X(legL, s * 38); X(legR, -s * 38); X(armL, -s * 32); X(armR, s * 32);
            X(fl, s * 40); X(br, s * 40); X(fr, -s * 40); X(bl, -s * 40);
            if (body != null) body.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(phase)) * 0.012f * amount, 0);
            if (tail != null) tail.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 9) * 25);
        }
        public void Head(float pitchDeg) { X(head, pitchDeg); }
    }

    /// <summary>Wanders around the island, or follows another walker (husky).</summary>
    public class Walker : MonoBehaviour
    {
        public float speed = 0.5f, wanderRadius = 3f;
        public bool quad, peck;
        public Walker follow;
        LimbRig rig; Vector3 home, target; float wait, phase, curSpeed, walkT;

        void Start() { rig = new LimbRig(transform); home = transform.position; wait = Random.Range(0, 3f); target = transform.position; }

        void Update()
        {
            var g = ConXanhGame.I; float dt = Time.deltaTime;
            Vector3 to;
            if (follow != null)
            {
                to = follow.transform.position - transform.position; to.y = 0;
                if (to.magnitude < 0.6f) to = Vector3.zero;
            }
            else
            {
                if (wait > 0) { wait -= dt; to = Vector3.zero; if (wait <= 0) { var r = Random.insideUnitCircle * wanderRadius; target = home + new Vector3(r.x, 0, r.y); walkT = 0; } }
                else { to = target - transform.position; to.y = 0; walkT += dt; if (to.magnitude < 0.12f || walkT > 10 || !g.Walkable(target)) { wait = Random.Range(1.5f, 5f); to = Vector3.zero; } }
            }
            float want = to.sqrMagnitude > 0.0001f ? speed * (follow != null && to.magnitude > 1.6f ? 1.5f : 1f) : 0;
            curSpeed = Mathf.Lerp(curSpeed, want, 1 - Mathf.Exp(-dt * 8));
            if (curSpeed > 0.01f && to.sqrMagnitude > 0.0001f)
            {
                var dir = to.normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1 - Mathf.Exp(-dt * 10));
                var next = transform.position + transform.forward * curSpeed * dt;
                if (g.Walkable(next)) transform.position = next; else { wait = 0.5f; target = transform.position; }
            }
            var p = transform.position; p.y = Mathf.Lerp(p.y, g.island.HeightAt(p.x, p.z), 1 - Mathf.Exp(-dt * 15)); transform.position = p;
            phase += curSpeed * dt * (quad ? 22 : 16);
            rig.Walk(phase, Mathf.Clamp01(curSpeed / 0.5f));
            if (peck) rig.Head(curSpeed < 0.05f ? Mathf.Max(0, Mathf.Sin(Time.time * 7 + home.x)) * 60 : 0);
        }
    }

    /// <summary>Buffalo pulling the plow back and forth across the rice field; the farmer follows behind.</summary>
    public class Plow : MonoBehaviour
    {
        public Transform field, leader;
        LimbRig rig; float dir = 1, lane, phase; Vector3 last;
        void Start() { rig = new LimbRig(transform); last = transform.position; }
        void Update()
        {
            float dt = Time.deltaTime;
            if (leader != null)
            {
                var back = leader.position - leader.forward * 0.45f;
                transform.position = Vector3.Lerp(transform.position, back, 1 - Mathf.Exp(-dt * 6));
                transform.rotation = leader.rotation;
            }
            else
            {
                var lp = field.InverseTransformPoint(transform.position);
                lp.x += dir * 0.28f * dt;
                if (Mathf.Abs(lp.x) > 0.85f) { lp.x = Mathf.Sign(lp.x) * 0.85f; dir = -dir; lane = (lane + 1) % 5; }
                lp.z = Mathf.Lerp(lp.z, -0.55f + lane * 0.27f, 1 - Mathf.Exp(-dt * 2));
                transform.position = field.TransformPoint(lp);
                transform.rotation = field.rotation * Quaternion.Euler(0, dir > 0 ? 90 : -90, 0);
            }
            float sp = (transform.position - last).magnitude / Mathf.Max(dt, 0.0001f); last = transform.position;
            phase += sp * dt * 20;
            rig.Walk(phase, Mathf.Clamp01(sp / 0.25f));
        }
    }

    /// <summary>Boats and ducks bob on the water.</summary>
    public class Bob : MonoBehaviour
    {
        public float amp = 0.02f; Vector3 p0; float seed;
        void Start() { p0 = transform.position; seed = Random.value * 10; }
        void Update()
        {
            float t = Time.time + seed;
            transform.position = p0 + new Vector3(0, Mathf.Sin(t * 1.5f) * amp - 0.02f, 0);
            var e = transform.eulerAngles; transform.rotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 2f, e.y, Mathf.Sin(t * 1.1f) * 2.5f);
        }
    }

    /// <summary>Gentle wind sway for trees and palms.</summary>
    public class Sway : MonoBehaviour
    {
        Quaternion r0; float seed;
        void Start() { r0 = transform.localRotation; seed = transform.position.x * 0.7f; }
        void Update() { transform.localRotation = r0 * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.2f + seed) * 1.2f); }
    }
}

namespace ConXanh
{
    /// <summary>Ghe thương lái: sails in from the open sea ringing its bell, waits, sails away, comes back later.</summary>
    public class TraderBoat : MonoBehaviour
    {
        public Vector3 dock; Vector3 far; float t; int state; float wait;
        void Start() { var o = dock; o.y = 0; far = dock + o.normalized * 14f; transform.position = far; state = 0; t = 0; Sound.I?.Bell(); GameUI.I?.Toast("Reng reng! Ghe thương lái đang tới cồn"); }
        void Update()
        {
            float dt = Time.deltaTime;
            if (state == 0) { t = Mathf.Min(1, t + dt / 12f); Move(far, dock, 1 - (1 - t) * (1 - t)); if (t >= 1) { state = 1; wait = 45f; Sound.I?.Bell(); GameUI.I?.Toast("Ghe thương lái đã cập bến"); } }
            else if (state == 1) { wait -= dt; Bob(); if (wait <= 0) { state = 2; t = 0; } }
            else if (state == 2) { t = Mathf.Min(1, t + dt / 12f); Move(dock, far, t * t); if (t >= 1) { state = 3; wait = 60f; } }
            else { wait -= dt; if (wait <= 0) Start(); }
        }
        void Move(Vector3 a, Vector3 b, float k)
        {
            var p = Vector3.Lerp(a, b, k); transform.position = p; Bob();
            var d = b - a; d.y = 0; if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0, -90, 0) * d) ;
        }
        void Bob() { var p = transform.position; p.y = Mathf.Sin(Time.time * 1.4f) * 0.02f - 0.04f; transform.position = p; }
    }
}
