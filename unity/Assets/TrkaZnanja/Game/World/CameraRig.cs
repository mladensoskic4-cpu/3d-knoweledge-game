// Kamera: let iznad mape u meniju, praćenje iza tvog vozila u vožnji, lagano kruženje oko tebe dok čitaš pitanje,
// pregled odozgo i finale na cilju (usporen snimak pa kruženje oko pobjednika).
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class CameraRig : MonoBehaviour
    {
        public enum Mode { MapOrbit, Follow, QuestionOrbit, Overview, Finale, Showroom, Fixed }

        public Camera Cam { get; private set; }
        public Mode Current { get; private set; } = Mode.MapOrbit;
        Transform target;
        Vector3 center, fixedPos, fixedLook;
        float extent = 40, t, orbitAngle;
        Vector3 look;
        public float FixedRate = 2f;     // koliko brzo kamera stiže u zadatu tačku (Fixed/Showroom); garaža traži brže

        public static CameraRig Create(Transform parent)
        {
            var go = new GameObject("Kamera");
            go.transform.SetParent(parent, false);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 46;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600;
            cam.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            rig.Cam = cam;
            return rig;
        }

        public void SetWorld(Bounds b)
        {
            center = b.center;
            extent = Mathf.Max(b.size.x, b.size.z) * 0.5f;
        }

        public void MapOrbit() => Current = Mode.MapOrbit;
        public void Follow(Transform tr) { target = tr; Current = Mode.Follow; }
        public void QuestionOrbit(Transform tr) { target = tr; Current = Mode.QuestionOrbit; orbitAngle = tr != null ? Mathf.Atan2(-tr.forward.x, -tr.forward.z) : 0; }
        public void Overview(Transform tr) { target = tr; Current = Mode.Overview; }
        public void Finale(Transform tr) { target = tr; Current = Mode.Finale; orbitAngle = tr != null ? Mathf.Atan2(tr.forward.x, tr.forward.z) : 0; }
        public void Showroom(Vector3 pos, Vector3 lookAt) { fixedPos = pos; fixedLook = lookAt; Current = Mode.Showroom; }
        public void Fixed(Vector3 pos, Vector3 lookAt) { fixedPos = pos; fixedLook = lookAt; Current = Mode.Fixed; }

        // Odmah na mjesto (bez klizanja), npr. na početku trke.
        public void Snap()
        {
            ComputeDesired(out var p, out var l);
            transform.position = p;
            look = l;
            transform.LookAt(look);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            ComputeDesired(out var desired, out var desiredLook);
            float rate = Current == Mode.Follow ? 3.2f : Current == Mode.Fixed || Current == Mode.Showroom ? FixedRate : 2.0f;
            float k = 1 - Mathf.Exp(-dt * rate);
            transform.position = Vector3.Lerp(transform.position, desired, k);
            look = Vector3.Lerp(look, desiredLook, k);
            transform.LookAt(look);
        }

        void ComputeDesired(out Vector3 pos, out Vector3 lookAt)
        {
            bool portrait = Screen.height > Screen.width;
            switch (Current)
            {
                case Mode.Follow when target != null:
                    {
                        var fwd = target.forward.Flat().normalized;
                        pos = target.position - fwd * 3.2f + Vector3.up * 2.4f + Vector3.Cross(Vector3.up, fwd) * 0.4f;
                        lookAt = target.position + fwd * 1.6f + Vector3.up * 0.2f;
                        return;
                    }
                case Mode.QuestionOrbit when target != null:
                    {
                        orbitAngle += Time.unscaledDeltaTime * 0.12f;
                        var off = new Vector3(Mathf.Sin(orbitAngle), 0, Mathf.Cos(orbitAngle)) * 3.0f;
                        pos = target.position + off + Vector3.up * 1.7f;
                        lookAt = target.position + Vector3.up * 0.35f;
                        return;
                    }
                case Mode.Overview when target != null:
                    pos = target.position + new Vector3(-2, 11, -8);
                    lookAt = target.position + new Vector3(0, 0, 2);
                    return;
                case Mode.Finale when target != null:
                    {
                        orbitAngle += Time.unscaledDeltaTime * 0.35f;
                        var off = new Vector3(Mathf.Sin(orbitAngle), 0, Mathf.Cos(orbitAngle)) * 2.2f;
                        pos = target.position + off + Vector3.up * 0.9f;
                        lookAt = target.position + Vector3.up * 0.4f;
                        return;
                    }
                case Mode.Showroom:
                case Mode.Fixed:
                    pos = fixedPos; lookAt = fixedLook;
                    return;
                default:
                    {
                        float r = Mathf.Max(18, extent * 0.75f) * (portrait ? 1.3f : 1f);
                        float a = t * 0.04f;
                        pos = center + new Vector3(Mathf.Cos(a) * r, r * 0.62f, Mathf.Sin(a) * r * 0.75f);
                        lookAt = center;
                        return;
                    }
            }
        }
    }
}
