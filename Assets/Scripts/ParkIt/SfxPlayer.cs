using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public enum Sfx { Click, Move, Slip, Win, Lose, Undo }

    /// <summary>Tiny synthesized sound effects so the toolbar mute toggle has something to mute.</summary>
    public class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer instance;
        private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        private AudioSource source;

        public static bool Muted
        {
            get => ProgressSave.Instance.SoundMuted;
            set => ProgressSave.Instance.SetSoundMuted(value);
        }

        public static void Play(Sfx sfx)
        {
            if (Muted) return;
            if (instance == null)
            {
                instance = FindFirstObjectByType<SfxPlayer>();
                if (instance == null) instance = new GameObject("ParkIt_Sfx").AddComponent<SfxPlayer>();
            }
            instance.PlayClip(sfx);
        }

        private void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = .45f;
        }

        private void PlayClip(Sfx sfx)
        {
            if (!clips.TryGetValue(sfx, out var clip)) clips[sfx] = clip = Build(sfx);
            source.PlayOneShot(clip);
        }

        private static AudioClip Build(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click: return Tone("click", new[] { 1200f }, .04f, .35f);
                case Sfx.Move: return Tone("move", new[] { 220f, 330f }, .12f, .5f, true);
                case Sfx.Slip: return Tone("slip", new[] { 660f, 520f, 380f }, .3f, .35f, true);
                case Sfx.Win: return Tone("win", new[] { 523f, 659f, 784f, 1046f }, .5f, .5f);
                case Sfx.Lose: return Tone("lose", new[] { 392f, 330f, 262f }, .45f, .5f);
                default: return Tone("undo", new[] { 500f, 380f }, .12f, .4f);
            }
        }

        // Plays the notes back to back with a soft envelope; 'buzz' adds a square-ish engine rasp.
        private static AudioClip Tone(string name, float[] notes, float duration, float gain, bool buzz = false)
        {
            const int rate = 22050;
            int samples = Mathf.CeilToInt(duration * rate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                int note = Mathf.Min(notes.Length - 1, (int)(t / duration * notes.Length));
                float phase = 2f * Mathf.PI * notes[note] * t;
                float wave = buzz ? Mathf.Sign(Mathf.Sin(phase)) * .35f + Mathf.Sin(phase * .5f) * .4f : Mathf.Sin(phase);
                float env = Mathf.Clamp01(t / .005f) * Mathf.Clamp01((duration - t) / (duration * .6f));
                data[i] = wave * env * gain;
            }
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
