// Izlog: tvoj lik u vozilu na postolju na početku staze. U meniju stoji u sredini ekrana (tap → garaža),
// a u garaži se okreće prevlačenjem i približava/udaljava (točkić, dva prsta). Kamera se računa odavde.
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class Showcase : MonoBehaviour
    {
        public const float Scale = 1.7f;          // lik je malo veći nego u trci, da se vide detalji
        public Transform Pivot { get; private set; }
        public AvatarRig Rig { get; private set; }
        public Vector3 Forward { get; private set; }   // smjer staze na startu
        public float Yaw;                           // okret lika (stepeni), 0 = gleda prema kameri menija
        public bool AutoSpin;
        public float Top => 0.16f;                  // visina gornje ploče postolja
        GameObject model;
        Look shown;
        float centerY = 0.32f, size = 0.85f;      // visina sredine i veličina lika (za kadriranje kamere)
        public float SizeFactor => Mathf.Clamp(size / 1.1f, 0.8f, 1.6f);

        public static Showcase Create(Transform parent, Vector3 pos, Vector3 forward)
        {
            var go = new GameObject("Izlog");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var s = go.AddComponent<Showcase>();
            s.Forward = forward.Flat().sqrMagnitude > 0.001f ? forward.Flat().normalized : Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(s.Forward);

            var k = new MeshKit();
            k.Cylinder(new Vector3(0, -0.06f, 0), 0.78f, 0.74f, 0.14f, 40, MeshKit.C("#2b7de9"));
            k.Cylinder(new Vector3(0, 0.08f, 0), 0.7f, 0.68f, 0.08f, 40, MeshKit.C("#f6bd2a"), true, MeshKit.C("#ffe08a"));
            // svjetlucavi rub (sijalice)
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24;
                k.Sphere(new Vector3(Mathf.Cos(a) * 0.76f, 0.02f, Mathf.Sin(a) * 0.76f), 0.022f, MeshKit.C(i % 2 == 0 ? "#fff6c8" : "#ffd23f", 0.9f), 4, 6);
            }
            k.ToObject("Postolje", go.transform);

            s.Pivot = new GameObject("Okret").transform;
            s.Pivot.SetParent(go.transform, false);
            s.Pivot.localPosition = new Vector3(0, s.Top, 0);

            var lightGo = new GameObject("Svjetlo izloga");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0.6f, 1.6f, -1.8f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point; l.range = 5; l.intensity = 1.6f; l.color = Col.Hex("#fff3dc");
            return s;
        }

        public void SetLook(Look look)
        {
            if (model != null && shown != null && Same(shown, look)) return;
            if (model != null) Destroy(model);
            shown = look.Clone();
            model = Avatars.Build(look, Pivot, "Model");
            model.transform.localScale = Vector3.one * Scale;
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);   // gleda unazad niz stazu, prema kameri
            Rig = model.GetComponent<AvatarRig>();
            if (Rig != null) Rig.Preview = true;
            // kadar prema stvarnoj veličini (kamion i balon su viši od bicikla)
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
                centerY = Mathf.Clamp(b.center.y - Pivot.position.y, 0.2f, 1.2f);
                size = Mathf.Max(0.5f, b.size.magnitude);
                if (Rig != null && Rig.Flies) centerY += 0.5f;
            }
        }

        static bool Same(Look a, Look b) =>
            a.Zivotinja == b.Zivotinja && a.Vozilo == b.Vozilo && a.Boja == b.Boja && a.Kapa == b.Kapa && a.Naocare == b.Naocare &&
            a.Sara == b.Sara && a.Boja2 == b.Boja2 && a.Nalijepnica == b.Nalijepnica && a.Trag == b.Trag;

        void Update()
        {
            if (AutoSpin) Yaw += Time.unscaledDeltaTime * 16;
            Pivot.localRotation = Quaternion.Euler(0, Yaw, 0);
        }

        // Kamera ispred lika (na stazi prije starta), pod uglom `orbit` stepeni oko lika, na udaljenosti `dist`;
        // `shift` pomjera lik lijevo/desno na ekranu (pozitivno = lik desnije), da ga ne prekrije meni.
        public void CameraAt(float orbit, float pitch, float dist, float shift, WorldView world, out Vector3 pos, out Vector3 look, float lift = 0)
        {
            var center = transform.position + Vector3.up * (Top + centerY);
            dist *= SizeFactor;
            var back = Quaternion.AngleAxis(orbit, Vector3.up) * (-Forward);
            var dir = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, back)) * back;
            pos = center + dir.normalized * dist;
            if (world != null)
            {
                float ground = world.GroundAt(pos) + 0.35f;
                if (pos.y < ground) pos.y = ground;
            }
            var right = Vector3.Cross(Vector3.up, (center - pos).normalized).normalized;
            var upv = Vector3.Cross((center - pos).normalized, right).normalized;
            look = center - right * shift * dist - upv * lift * dist;
        }
    }
}
