using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace pac_man
{
    public static class SoundSynth
    {
        public static SoundEffect StartTheme;
        public static SoundEffect Waka;
        public static SoundEffect Death;
        public static SoundEffect GhostEat;
        public static SoundEffect FruitEat;
        public static SoundEffect Siren;
        public static SoundEffect FrightenedSiren;

        public static void Initialize()
        {
            try
            {
                int sampleRate = 22050;

                // 1. Generate Start Theme
                StartTheme = GenerateStartTheme(sampleRate);

                // 2. Generate Waka-Waka
                Waka = GenerateWaka(sampleRate);

                // 3. Generate Death Sound
                Death = GenerateDeath(sampleRate);

                // 4. Generate Ghost Eat
                GhostEat = GenerateGhostEat(sampleRate);

                // 5. Generate Fruit Eat
                FruitEat = GenerateFruitEat(sampleRate);

                // 6. Generate Siren
                Siren = GenerateSiren(sampleRate);

                // 7. Generate Frightened Siren
                FrightenedSiren = GenerateFrightenedSiren(sampleRate);
            }
            catch (Exception ex)
            {
                // In case audio synthesis fails or audio device is missing, print warning but don't crash
                System.Diagnostics.Debug.WriteLine("Audio synthesis initialization failed: " + ex.Message);
            }
        }

        private static SoundEffect CreateWav(int sampleRate, short[] samples)
        {
            byte[] wavBytes = new byte[44 + samples.Length * 2];
            
            // RIFF header
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavBytes, 0);
            BitConverter.GetBytes(36 + samples.Length * 2).CopyTo(wavBytes, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavBytes, 8);
            
            // fmt chunk
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavBytes, 12);
            BitConverter.GetBytes(16).CopyTo(wavBytes, 16); // Chunk size
            BitConverter.GetBytes((short)1).CopyTo(wavBytes, 20); // PCM format
            BitConverter.GetBytes((short)1).CopyTo(wavBytes, 22); // Mono channel
            BitConverter.GetBytes(sampleRate).CopyTo(wavBytes, 24); // Sample rate
            BitConverter.GetBytes(sampleRate * 2).CopyTo(wavBytes, 28); // Byte rate (SampleRate * Channels * BitsPerSample / 8)
            BitConverter.GetBytes((short)2).CopyTo(wavBytes, 32); // Block align (2 bytes)
            BitConverter.GetBytes((short)16).CopyTo(wavBytes, 34); // 16-bit
            
            // data chunk
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavBytes, 36);
            BitConverter.GetBytes(samples.Length * 2).CopyTo(wavBytes, 40);

            // Copy samples to bytes
            for (int i = 0; i < samples.Length; i++)
            {
                BitConverter.GetBytes(samples[i]).CopyTo(wavBytes, 44 + i * 2);
            }

            using (var ms = new MemoryStream(wavBytes))
            {
                return SoundEffect.FromStream(ms);
            }
        }

        private static SoundEffect GenerateStartTheme(int sampleRate)
        {
            // Pac-man iconic intro sequence
            // We define notes as: (Frequency, Duration in seconds)
            // Speeds up at the end!
            float bpm = 125f;
            float beat = 60f / bpm;
            float q = beat / 2f; // Quarter note duration
            float e = q / 2f;    // Eighth note duration
            float s = e / 2f;    // Sixteenth note duration

            // Notes and frequencies
            float B4 = 493.88f;
            float B5 = 987.77f;
            float Fs5 = 739.99f;
            float Ds5 = 622.25f;
            float C5 = 523.25f;
            float C6 = 1046.50f;
            float G5 = 783.99f;
            float E5 = 659.25f;
            
            float F5 = 698.46f;
            float Gs5 = 830.61f;
            float A5 = 880.00f;
            float As5 = 932.33f;

            var sequence = new[]
            {
                // Measure 1
                (B4, e), (B5, e), (Fs5, e), (Ds5, e), (B5, s), (Fs5, s), (Ds5, e + s), (0f, s),
                // Measure 2
                (C5, e), (C6, e), (G5, e), (E5, e), (C6, s), (G5, s), (E5, e + s), (0f, s),
                // Measure 3
                (B4, e), (B5, e), (Fs5, e), (Ds5, e), (B5, s), (Fs5, s), (Ds5, e + s), (0f, s),
                // Measure 4
                (Ds5, s), (E5, s), (F5, e), (F5, s), (Fs5, s), (G5, e), (G5, s), (Gs5, s), (A5, e), (As5, e), (B5, q)
            };

            // Calculate total samples needed
            float totalSeconds = 0f;
            foreach (var note in sequence) totalSeconds += note.Item2;

            int totalSamples = (int)(sampleRate * totalSeconds);
            short[] samples = new short[totalSamples];

            int sampleIndex = 0;
            double phase = 0;

            foreach (var note in sequence)
            {
                float freq = note.Item1;
                float duration = note.Item2;
                int noteSamples = (int)(sampleRate * duration);

                for (int i = 0; i < noteSamples; i++)
                {
                    if (sampleIndex >= totalSamples) break;

                    if (freq == 0)
                    {
                        samples[sampleIndex++] = 0;
                    }
                    else
                    {
                        // Retro Arcade Square Wave Synthesis
                        // To make it sound warmer, we can use a slight decay envelope or a low-pass effect.
                        // Standard retro sound is simple square wave: amplitude toggles between positive and negative
                        double t = (double)i / sampleRate;
                        phase += 2.0 * Math.PI * freq / sampleRate;
                        
                        double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0;

                        // Apply standard ADSR volume envelope (very simple: rapid fade out at the very end of note to avoid clicks)
                        double volume = 0.5;
                        int fadeThreshold = noteSamples - 300;
                        if (i > fadeThreshold)
                        {
                            volume *= (double)(noteSamples - i) / 300.0;
                        }

                        samples[sampleIndex++] = (short)(val * volume * 12000);
                    }
                }
            }

            return CreateWav(sampleRate, samples);
        }

        private static SoundEffect GenerateWaka(int sampleRate)
        {
            // Waka-Waka is a short frequency sweep (from low to high and back down, mimicking eating)
            float duration = 0.08f;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            double phase = 0;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / numSamples;
                
                // Sweep frequency from 300Hz to 650Hz, then back down
                double freq = 300.0 + 350.0 * Math.Sin(t * Math.PI);

                phase += 2.0 * Math.PI * freq / sampleRate;
                double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0; // Square wave

                // Envelope to fade out at the end
                double env = 1.0;
                if (i > numSamples * 0.8)
                {
                    env = (1.0 - t) / 0.2;
                }

                samples[i] = (short)(val * env * 8000);
            }

            return CreateWav(sampleRate, samples);
        }

        private static SoundEffect GenerateDeath(int sampleRate)
        {
            // Cascading pitch sweeping down 10-12 times, then a final noise burst!
            float duration = 1.5f;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            double phase = 0;
            
            // Total sweeps: 12
            int numSweeps = 12;
            int samplesPerSweep = numSamples / (numSweeps + 1);

            for (int s = 0; s < numSweeps; s++)
            {
                int startIdx = s * samplesPerSweep;
                for (int i = 0; i < samplesPerSweep; i++)
                {
                    int globalIdx = startIdx + i;
                    if (globalIdx >= numSamples) break;

                    double t = (double)i / samplesPerSweep;
                    // Frequency sweeps from 800Hz down to 200Hz in each sweep
                    double freq = 850.0 - 650.0 * t;
                    
                    phase += 2.0 * Math.PI * freq / sampleRate;
                    double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0;

                    // Envelope: quiet down as sweeps progress
                    double globalVolume = 1.0 - (double)s / numSweeps;

                    samples[globalIdx] = (short)(val * globalVolume * 10000);
                }
            }

            // Final quiet noise sweep at the very end
            int noiseStart = numSweeps * samplesPerSweep;
            Random rand = new Random();
            for (int i = noiseStart; i < numSamples; i++)
            {
                double t = (double)(i - noiseStart) / (numSamples - noiseStart);
                double val = rand.NextDouble() * 2.0 - 1.0; // White noise
                double volume = 0.3 * (1.0 - t);
                samples[i] = (short)(val * volume * 10000);
            }

            return CreateWav(sampleRate, samples);
        }

        private static SoundEffect GenerateGhostEat(int sampleRate)
        {
            // Quick arcade slide up (from 200Hz to 1200Hz)
            float duration = 0.3f;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            double phase = 0;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / numSamples;
                double freq = 200.0 + 1000.0 * (t * t); // parabolic rise

                phase += 2.0 * Math.PI * freq / sampleRate;
                double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0;

                double env = 1.0 - t;

                samples[i] = (short)(val * env * 10000);
            }

            return CreateWav(sampleRate, samples);
        }

        private static SoundEffect GenerateFruitEat(int sampleRate)
        {
            // Retro double ding sound
            float duration = 0.25f;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            double phase = 0;

            int mid = numSamples / 2;

            for (int i = 0; i < numSamples; i++)
            {
                double freq = (i < mid) ? 880.0 : 1320.0; // note A5 then E6
                int offset = (i < mid) ? i : (i - mid);
                int len = (i < mid) ? mid : (numSamples - mid);

                phase += 2.0 * Math.PI * freq / sampleRate;
                double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0;

                double env = 1.0 - (double)offset / len;

                samples[i] = (short)(val * env * 8000);
            }

            return CreateWav(sampleRate, samples);
        }

        private static SoundEffect GenerateSiren(int sampleRate)
        {
            // Pac-man looping ambient siren (1.0 second duration)
            float duration = 0.8f;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            double phase = 0;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / numSamples;
                // Oscillating pitch frequency: 300Hz to 400Hz and back down
                double freq = 340.0 + 60.0 * Math.Sin(t * 2.0 * Math.PI);

                phase += 2.0 * Math.PI * freq / sampleRate;
                double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0;

                samples[i] = (short)(val * 3500); // lower volume since it loops constantly
            }

            return CreateWav(sampleRate, samples);
        }

        private static SoundEffect GenerateFrightenedSiren(int sampleRate)
        {
            // Low alternate tone alarm (0.6 seconds loop)
            float duration = 0.6f;
            int numSamples = (int)(sampleRate * duration);
            short[] samples = new short[numSamples];
            double phase = 0;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / numSamples;
                // Alternate between 220Hz and 180Hz
                double freq = (t < 0.5) ? 220.0 : 170.0;

                phase += 2.0 * Math.PI * freq / sampleRate;
                double val = Math.Sin(phase) >= 0 ? 1.0 : -1.0;

                samples[i] = (short)(val * 4000);
            }

            return CreateWav(sampleRate, samples);
        }
    }
}
