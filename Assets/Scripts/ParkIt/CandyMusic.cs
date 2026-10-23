using UnityEngine;

namespace ParkIt
{
    /// <summary>Original, lightweight eight-bar music-box loop with soft chord plucks.</summary>
    public static class CandyMusic
    {
        public static AudioClip Build()
        {
            const int rate = 22050;
            const float beat = .625f; // 96 BPM, eight bars of 4/4.
            var samples = new float[Mathf.RoundToInt(32 * beat * rate)];
            int[,] chords = { { 48, 52, 55 }, { 53, 57, 60 }, { 45, 48, 52 }, { 43, 47, 50 },
                              { 48, 52, 55 }, { 53, 57, 60 }, { 45, 48, 52 }, { 43, 47, 50 } };
            int[,] melody = {
                { 72, 76, 79, 0, 76, 74, 72, 0 },
                { 77, 81, 79, 0, 77, 76, 74, 0 },
                { 76, 79, 81, 0, 79, 76, 72, 0 },
                { 74, 79, 83, 0, 81, 79, 74, 0 },
                { 76, 79, 84, 0, 79, 76, 74, 72 },
                { 77, 81, 84, 0, 81, 79, 77, 0 },
                { 76, 79, 81, 79, 76, 74, 72, 0 },
                { 74, 76, 79, 0, 74, 71, 72, 0 }
            };
            for (int bar = 0; bar < 8; bar++)
            {
                float start = bar * 4 * beat;
                for (int pulse = 0; pulse < 4; pulse++)
                {
                    AddNote(samples, rate, start + pulse * beat, chords[bar, pulse % 3] + 12, .8f, .085f, false);
                    if (pulse % 2 == 0) AddNote(samples, rate, start + pulse * beat, chords[bar, 0] - 12, .9f, .10f, false);
                }
                for (int step = 0; step < 8; step++)
                {
                    int note = melody[bar, step];
                    if (note == 0) continue;
                    float time = start + step * beat * .5f;
                    AddNote(samples, rate, time, note, .7f, .17f, true);
                    AddNote(samples, rate, time + beat * .75f, note, .55f, .025f, true);
                }
            }
            // Wrapped note tails make the transition back to bar one seamless.
            var clip = AudioClip.Create("Candy_Music_Box_Loop", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void AddNote(float[] samples, int rate, float start, int midi, float duration, float gain, bool bell)
        {
            float hz = 440f * Mathf.Pow(2f, (midi - 69) / 12f);
            int first = Mathf.RoundToInt(start * rate), count = Mathf.RoundToInt(duration * rate);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate, u = t / duration;
                float envelope = Mathf.Clamp01(t / .012f) * Mathf.Exp(-4f * u) * Mathf.Clamp01((1f - u) / .12f);
                float phase = 2f * Mathf.PI * hz * t;
                float wave = bell ? .8f * Mathf.Sin(phase) + .16f * Mathf.Sin(2f * phase) + .04f * Mathf.Sin(3f * phase)
                    : Mathf.Sin(phase);
                samples[(first + i) % samples.Length] += wave * envelope * gain;
            }
        }
    }
}
