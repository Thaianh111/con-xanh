// Cồn Xanh – orbit camera. Xem: 1 ngón/chuột trái xoay. Mọi lúc: 2 ngón zoom + xoay, chuột phải xoay, cuộn zoom. VTA Games · Võ Thái Anh
using UnityEngine;

namespace ConXanh
{
    public class OrbitCam : MonoBehaviour
    {
        public static bool oneFingerRotate = true;
        public Vector3 target;
        public float distance = 15f, yaw = 0f, pitch = 37f, minDist = 4f, maxDist = 30f;
        readonly InputCompat.T[] touches = new InputCompat.T[2];
        float lastPinch;

        void LateUpdate()
        {
            int n = InputCompat.Touches(touches);
            if (n == 1 && oneFingerRotate && !Builder.I.GhostActive) { yaw += touches[0].delta.x * 0.25f; pitch -= touches[0].delta.y * 0.2f; }
            else if (n == 2)
            {
                float d = Vector2.Distance(touches[0].pos, touches[1].pos);
                if (touches[1].began || lastPinch <= 0) lastPinch = d;
                distance *= lastPinch / Mathf.Max(d, 1f); lastPinch = d;
                var avg = (touches[0].delta + touches[1].delta) * 0.5f;
                yaw += avg.x * 0.2f; pitch -= avg.y * 0.15f;
            }
            else if (n == 0)
            {
                lastPinch = 0;
                InputCompat.MouseButtons(out bool left, out bool right);
                if (right || (left && oneFingerRotate)) { var dm = InputCompat.MouseDelta(); yaw += dm.x * 0.25f; pitch -= dm.y * 0.2f; }
                distance *= 1 - InputCompat.Scroll() * 0.1f;
            }
            pitch = Mathf.Clamp(pitch, 12f, 80f);
            distance = Mathf.Clamp(distance, minDist, maxDist);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target + rot * new Vector3(0, 0, -distance);
            transform.rotation = rot;
        }
    }
}
