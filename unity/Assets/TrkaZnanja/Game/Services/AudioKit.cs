// Zvuk: muzika sa domaćim motivima, zvučni efekti, zvuk vozila po pređenom polju i melodije za pitanja.
// Ako u Resources/Zvuk staviš pravi CC0 zvuk sa istim imenom (npr. "klik", "tacno", "muzika"), koristi se on
// umjesto sintetizovanog. Vidi PROCITAJ.md (Kenney paketi).
using System.Collections;
using System.Collections.Generic;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class AudioKit : MonoBehaviour
    {
        public static AudioKit I { get; private set; }
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource music, sfx, melody;
        float musicVolume = 0.55f, duckK = 1;
        public bool MusicOn { get; private set; } = true;
        public bool SfxOn { get; private set; } = true;

        public static AudioKit Create(Transform parent)
        {
            var go = new GameObject("Zvuk");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioKit>();
            I = a;
            a.music = go.AddComponent<AudioSource>();
            a.music.loop = true;
            a.music.playOnAwake = false;
            a.sfx = go.AddComponent<AudioSource>();
            a.sfx.playOnAwake = false;
            a.melody = go.AddComponent<AudioSource>();
            a.melody.loop = true;
            a.melody.playOnAwake = false;
            return a;
        }

        AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, Mathf.Max(1, data.Length), 1, Synth.Rate, false);
            c.SetData(data, 0);
            return c;
        }

        public AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Zvuk/" + name);
            if (c == null)
            {
                float[] data;
                switch (name)
                {
                    case "klik": data = Synth.Click(); break;
                    case "tacno": data = Synth.Correct(); break;
                    case "netacno": data = Synth.Wrong(); break;
                    case "novcic": data = Synth.Coin(); break;
                    case "fanfara": data = Synth.Fanfare(); break;
                    case "vatromet": data = Synth.Firework(); break;
                    case "vjetar": data = Synth.Whoosh(); break;
                    case "muzika": data = Synth.Music(); break;
                    default:
                        if (name.StartsWith("vozilo-")) { data = Synth.VehicleHop(name.Substring(7)); break; }
                        return null;
                }
                c = Make(name, data);
            }
            clips[name] = c;
            return c;
        }

        public void SetMusic(bool on)
        {
            MusicOn = on;
            if (on && !music.isPlaying)
            {
                music.clip = Clip("muzika");
                music.volume = musicVolume * duckK;
                music.Play();
            }
            else if (!on) music.Stop();
        }

        public void SetSfx(bool on) => SfxOn = on;

        // Tiša muzika dok svira melodija iz pitanja.
        public void Duck(bool on)
        {
            duckK = on ? 0.15f : 1f;
            music.volume = musicVolume * duckK;
        }

        public void Play(string name, float volume = 1f)
        {
            if (!SfxOn) return;
            var c = Clip(name);
            if (c != null) sfx.PlayOneShot(c, volume);
        }

        public void Click() => Play("klik", 0.6f);
        public void Correct() => Play("tacno", 0.8f);
        public void Wrong() => Play("netacno", 0.8f);

        public sealed class VehicleSound
        {
            readonly AudioKit kit; readonly string id;
            public VehicleSound(AudioKit k, string id) { kit = k; this.id = id; }
            public void Hop() => kit.Play("vozilo-" + id, 0.45f);
        }
        public VehicleSound Vehicle(string id) => SfxOn ? new VehicleSound(this, string.IsNullOrEmpty(id) ? "auto" : id) : null;

        // ---- melodije (pitanje „prepoznaj melodiju”) ----
        public void PlayMelody(string notes, double tempo)
        {
            StopMelody();
            var key = "mel:" + notes + "@" + tempo;
            if (!clips.TryGetValue(key, out var c)) clips[key] = c = Make("Melodija", Synth.RenderMelody(notes, tempo));
            melody.clip = c;
            melody.volume = 0.9f;
            melody.Play();
            Duck(true);
        }

        public void StopMelody()
        {
            if (melody.isPlaying) melody.Stop();
            Duck(false);
        }

        public bool MelodyPlaying => melody.isPlaying;

        // ---- zvuk instrumenta (mini igra): nekoliko nota jedna za drugom, npr. Instrumenti/violin/C4 ----
        Coroutine notes;
        readonly List<AudioSource> noteSources = new List<AudioSource>();

        public void PlayNotes(IList<string> clipNames, float gap = 0.48f, float volume = 0.9f)
        {
            StopNotes();
            notes = StartCoroutine(NotesRoutine(clipNames, gap, volume));
        }

        IEnumerator NotesRoutine(IList<string> clipNames, float gap, float volume)
        {
            Duck(true);
            for (int i = 0; i < clipNames.Count; i++)
            {
                var c = Clip(clipNames[i]);
                if (c != null)
                {
                    if (noteSources.Count <= i) { var src = gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; noteSources.Add(src); }
                    noteSources[i].clip = c;
                    noteSources[i].volume = volume;
                    noteSources[i].Play();
                }
                float t = 0;
                while (t < gap) { t += Time.unscaledDeltaTime; yield return null; }
            }
            float rest = 0;
            while (rest < 1.6f) { rest += Time.unscaledDeltaTime; yield return null; }
            Duck(false);
            notes = null;
        }

        public void StopNotes()
        {
            if (notes != null) { StopCoroutine(notes); notes = null; Duck(false); }
            foreach (var s in noteSources) if (s.isPlaying) s.Stop();
        }
    }

    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Tap(long ms = 25)
        {
            if (!Enabled || !Application.isMobilePlatform) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vib = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    vib?.Call("vibrate", ms);
                return;
            }
            catch (System.Exception) { /* nema vibracije na ovom uređaju */ }
#endif
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
