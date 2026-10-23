using System.Collections.Generic;
using UnityEngine;

namespace ParkIt
{
    public enum Sfx { Click, Move, Slip, Win, Lose, Undo, Landing, VictoryHop, ColorSparkle, GameOverPowerDown, HintTap, ShiftTap, UITap, PopupOpen, PopupClose }

    /// <summary>Tiny synthesized sound effects so the toolbar mute toggle has something to mute.</summary>
    public class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer instance;
        private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        private AudioSource source;
        [SerializeField, Range(0f, .5f)] private float musicVolume = .16f;
        [SerializeField, Range(0f, 1f)] private float musicUnderEffects = .60f;
        [SerializeField, Range(0f, 1f)] private float tapVolume = .20f;
        [SerializeField, Range(0f, 1f)] private float popupVolume = .18f;
        private AudioSource musicSource;
        private AudioClip musicClip;
        private bool pausedWithGame;
        private readonly Dictionary<Sfx, AudioSource> animationSources = new Dictionary<Sfx, AudioSource>();

        public static bool Muted
        {
            get => ProgressSave.Instance.SoundMuted;
            set
            {
                ProgressSave.Instance.SetSoundMuted(value);
                if (instance != null && instance.musicSource != null) instance.musicSource.mute = value;
                if (value && instance != null)
                {
                    instance.source?.Stop();
                    foreach (var channel in instance.animationSources.Values) channel.Stop();
                }
            }
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
            musicClip = CandyMusic.Build();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 160;
            musicSource.volume = 0f;
            musicSource.mute = Muted;
            musicSource.clip = musicClip;
            musicSource.Play();
        }

        public static void Stop(Sfx sfx)
        {
            if (instance != null && instance.animationSources.TryGetValue(sfx, out var channel)) channel.Stop();
        }

        public static void StopAll()
        {
            if (instance == null) return;
            instance.source?.Stop();
            foreach (var channel in instance.animationSources.Values) channel.Stop();
        }

        private void Update()
        {
            // Keep the melody behind short car cues, with a quick soft dip and
            // slower recovery rather than abrupt volume jumps between notes.
            if (Time.timeScale > 0f)
            {
                bool effectPlaying = source.isPlaying;
                // Quiet UI feedback must not pump the music on every tap.
                foreach (var entry in animationSources)
                    if (!IsTapCue(entry.Key) && !IsPopupCue(entry.Key)) effectPlaying |= entry.Value.isPlaying;
                float target = musicVolume * (effectPlaying ? musicUnderEffects : 1f);
                float response = target < musicSource.volume ? 18f : 3f;
                musicSource.volume = Mathf.Lerp(musicSource.volume, target, 1f - Mathf.Exp(-response * Time.unscaledDeltaTime));
            }
            bool paused = Time.timeScale == 0f;
            if (paused == pausedWithGame) return;
            pausedWithGame = paused;
            if (paused) source.Pause(); else source.UnPause();
            if (paused) musicSource.Pause(); else musicSource.UnPause();
            foreach (var entry in animationSources)
                if (entry.Key != Sfx.UITap && !IsPopupCue(entry.Key))
                {
                    if (IsTapCue(entry.Key)) { if (paused) entry.Value.Stop(); }
                    else if (paused) entry.Value.Pause(); else entry.Value.UnPause();
                }
        }

        private static bool IsTapCue(Sfx sfx) => sfx == Sfx.UITap || sfx == Sfx.HintTap || sfx == Sfx.ShiftTap;
        private static bool IsPopupCue(Sfx sfx) => sfx == Sfx.PopupOpen || sfx == Sfx.PopupClose;

        private void PlayClip(Sfx sfx)
        {
            if (IsPopupCue(sfx))
                foreach (var entry in animationSources)
                    if (IsPopupCue(entry.Key) || IsTapCue(entry.Key)) entry.Value.Stop();
            if (IsTapCue(sfx))
            {
                // Mixed rapid taps replace one another instead of layering chimes.
                foreach (var entry in animationSources)
                    if (IsTapCue(entry.Key) || IsPopupCue(entry.Key)) entry.Value.Stop();
                if (Time.timeScale == 0f && sfx != Sfx.UITap) return;
            }
            if (!clips.TryGetValue(sfx, out var clip)) clips[sfx] = clip = Build(sfx);
            if (sfx == Sfx.Landing || sfx == Sfx.VictoryHop || sfx == Sfx.ColorSparkle || sfx == Sfx.GameOverPowerDown || IsTapCue(sfx) || IsPopupCue(sfx))
            {
                if (!animationSources.TryGetValue(sfx, out var channel))
                {
                    channel = gameObject.AddComponent<AudioSource>();
                    channel.playOnAwake = false;
                    channel.spatialBlend = 0;
                    channel.priority = IsTapCue(sfx) || IsPopupCue(sfx) ? 144 : 96;
                    animationSources[sfx] = channel;
                }
                channel.volume = IsPopupCue(sfx) ? popupVolume : IsTapCue(sfx) ? tapVolume : .45f;
                channel.clip = clip;
                channel.Play(); // Replays this effect without piling up rapid taps.
                if (Time.timeScale == 0f && sfx != Sfx.UITap && !IsPopupCue(sfx)) channel.Pause();
                return;
            }
            source.PlayOneShot(clip);
            if (Time.timeScale == 0f) source.Pause();
        }

        private static AudioClip Build(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click: return Tone("click", new[] { 1200f }, .04f, .35f);
                case Sfx.PopupOpen: return PopupTone(true);
                case Sfx.PopupClose: return PopupTone(false);
                case Sfx.UITap: return Tone("ui_candy_tap", new[] { 780f, 1040f }, .065f, .25f);
                case Sfx.HintTap: return Tone("hint_candy_chime", new[] { 880f, 1320f }, .10f, .28f);
                case Sfx.ShiftTap: return Tone("shift_candy_pop", new[] { 660f, 990f }, .07f, .25f);
                case Sfx.Move: return Tone("move", new[] { 220f, 330f }, .12f, .5f, true);
                case Sfx.Slip: return Tone("slip", new[] { 660f, 520f, 380f }, .3f, .35f, true);
                case Sfx.Win: return Tone("win", new[] { 523f, 659f, 784f, 1046f }, .5f, .5f);
                case Sfx.Lose: return Tone("lose", new[] { 392f, 330f, 262f }, .45f, .5f);
                case Sfx.Landing: return AnimationTone("landing_soft_pop", .17f, 210f, 85f, .48f, false);
                case Sfx.VictoryHop: return AnimationTone("victory_hop_chime", .65f, 520f, 1040f, .42f, true);
                case Sfx.ColorSparkle: return AnimationTone("color_sparkle", .38f, 1320f, 2640f, .26f, true);
                case Sfx.GameOverPowerDown: return PowerDown();
                default: return Tone("undo", new[] { 500f, 380f }, .12f, .4f);
            }
        }

        private static AudioClip PopupTone(bool opening)
        {
            const int rate = 22050;
            float duration = opening ? .20f : .14f;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            var random = new System.Random(opening ? 42 : 43);
            float phase = 0f, air = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, u = t / duration;
                float hz = opening ? Mathf.Lerp(520f, 960f, u) : Mathf.Lerp(620f, 300f, u);
                phase += 2f * Mathf.PI * hz / rate;
                air = Mathf.Lerp(air, (float)random.NextDouble() * 2f - 1f, .18f);
                float envelope = Mathf.Sin(Mathf.PI * u) * Mathf.Pow(1f - u, opening ? 1.4f : .7f);
                float candy = Mathf.Sin(phase) + .15f * Mathf.Sin(phase * 2f);
                samples[i] = .26f * envelope * ((opening ? .8f : .3f) * candy + (opening ? .2f : .7f) * air);
            }
            var clip = AudioClip.Create(opening ? "popup_candy_pop" : "popup_soft_whoosh", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip PowerDown()
        {
            const int rate = 22050;
            float duration = CarVisuals.GameOverAnimationDuration;
            var data = new float[Mathf.CeilToInt(duration * rate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate, u = t / duration;
                phase += 2f * Mathf.PI * Mathf.Lerp(240f, 65f, u) / rate;
                // Four soft engine pulses track the shake, then fade with the lamps.
                float sputter = .55f + .45f * Mathf.Cos(u * Mathf.PI * 8f);
                float envelope = Mathf.Clamp01(t / .008f) * Mathf.Pow(1f - u, 2f);
                float engine = .72f * Mathf.Sin(phase) + .18f * Mathf.Sin(phase * 2f) + .10f * Mathf.Sin(phase * 3f);
                data[i] = engine * sputter * envelope * .48f;
            }
            var clip = AudioClip.Create("game_over_power_down", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip AnimationTone(string name, float duration, float startHz, float endHz, float gain, bool chime)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, u = t / duration;
                phase += 2f * Mathf.PI * Mathf.Lerp(startHz, endHz, u) / rate;
                float envelope = Mathf.Clamp01(t / .008f) * Mathf.Pow(1f - u, chime ? 1.4f : 2.5f);
                float wave = Mathf.Sin(phase);
                if (chime) wave = .7f * wave + .2f * Mathf.Sin(phase * 1.5f) + .1f * Mathf.Sin(phase * 2f);
                samples[i] = wave * envelope * gain;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (musicClip != null) Destroy(musicClip);
            foreach (var clip in clips.Values) if (clip != null) Destroy(clip);
            if (instance == this) instance = null;
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
