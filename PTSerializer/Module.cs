﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PTSerializer
{
    public class Module
    {
        public string Name { get; set; }
        public Sample[] Samples { get; set; }
        public const int MaxSamples = 31;
        public int SongLength { get; set; }
        public int[] SongPositions { get; set; }
        public List<Pattern> Patterns { get; set; }
        public byte Unknown { get; set; }
        public Module()
        {
            Samples = new Sample[MaxSamples];
            SongPositions = new int[128];
            for (var i = 0; i < MaxSamples; i++)
            {
                Samples[i] = new Sample();
            }
            Patterns = new List<Pattern>();
        }

        public void OptimseLoopedSamples()
        {
            foreach (var sample in this.Samples)
            {
                if (sample.RepeatLength > 2)
                {
                    var newLength = sample.RepeatStart + sample.RepeatLength;
                    if (newLength < sample.Length)
                    {
                        sample.Data = sample.Data.Take(newLength).ToArray();
                        sample.Length = newLength;
                    }
                }
            }
        }

        public void RemoteUnusedSamples()
        {
            // find all used samples
            var used = new bool[32];
            foreach (var pattern in Patterns)
            {
                foreach (var chan in pattern.Channels)
                {
                    foreach (var item in chan.PatternItems)
                    {
                        used[item.SampleNumber] = true;
                    }
                }
            }

            // clear any unsed samples
            for (var i = 1; i < used.Length; i++)
            {
                if (!used[i])
                {
                    Samples[i - 1].Data = new byte[2];
                    Samples[i - 1].Length = 2;
                    Samples[i - 1].RepeatStart = 0;
                    Samples[i - 1].RepeatLength = 2;
                    Samples[i - 1].FineTune = 0;
                }
            }
        }

        public void RemoveUnusedPatterns()
        {
            // zero out anything after song end
            for (var i = 0; i < 128; i++)
            {
                if (i >= SongLength)
                {
                    SongPositions[i] = 0;
                }
            }

            // based on song length, mark which patterns are actually used
            var used = new bool[100];
            for (var i = 0; i < SongLength; i++)
            {
                used[SongPositions[i]] = true;
            }

            // copy highest used pattern to lowest unused pattern
            for (var current = 0; current < used.Length; current++)
            {
                if (!used[current])
                {
                    var lastUsed = FindLast(true, used);
                    if (lastUsed > current)
                    {
                        Patterns[current] = Patterns[lastUsed];
                        RemapPattern(lastUsed, current);
                        used[current] = true;
                        used[lastUsed] = false;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            SanitizePatterns();
        }

        private void SanitizePatterns()
        {
            var newPatterns = new List<Pattern>();
            var maxUsedPattern = SongPositions.Max();
            for (var p = 0; p <= maxUsedPattern; p++)
            {
                newPatterns.Add(Patterns[p]);
            }
            Patterns = newPatterns;
        }

        public void RemoveDuplicatePatterns()
        {
            for (var a = 0; a < Patterns.Count; a++)
            {
                for (var b = a + 1; b < Patterns.Count; b++)
                {
                    var pata = Patterns[a];
                    var patb = Patterns[b];
                    if (PatternCompare(pata, patb))
                    {
                        RemapPattern(b, a);
                    }
                }
            }

            SanitizePatterns();
        }

        private bool PatternCompare(Pattern a, Pattern b)
        {
            for (var line = 0; line < 64; line++)
            {
                for (var chan = 0; chan < 4; chan++)
                {
                    var itema = a.Channels[chan].PatternItems[line];
                    var itemb = b.Channels[chan].PatternItems[line];

                    if (itema.SampleNumber != itemb.SampleNumber) return false;
                    if (itema.Period != itemb.Period) return false;
                    if (itema.Command != itemb.Command) return false;
                    if (itema.CommandValue != itemb.CommandValue) return false;
                }
            }
            return true;
        }

        private void RemapPattern(int source, int dest)
        {
            for (var p = 0; p < 128; p++)
            {
                if (SongPositions[p] == source)
                    SongPositions[p] = dest;
            }
        }

        private int FindLast(bool state, bool[] blist)
        {
            for (var i = blist.Length-1; i >= 0; i--)
            {
                if (blist[i] == state) return i;
            }
            return -1;
        }
    }

    public class Sample
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public int FineTune { get; set; }
        public int Volume { get; set; }
        public int RepeatStart { get; set; }
        public int RepeatLength { get; set; }
        public byte[] Data { get; set; }
    }

    public class Pattern
    {
        public Channel[] Channels { get; set; }

        public Pattern()
        {
            Channels = new Channel[4];
            Channels[0] = new Channel();
            Channels[1] = new Channel();
            Channels[2] = new Channel();
            Channels[3] = new Channel();
        }
    }
    
    public class Channel
    {
        public PatternItem[] PatternItems { get; set; }

        public Channel()
        {
            PatternItems = new PatternItem[64];
        }
    }

    public class PatternItem
    {
        public int SampleNumber { get; set; }
        public int Period { get; set; }
        public CommandType Command { get; set; }
        public byte CommandValue { get; set; }
    }

    public enum CommandType
    {
        None = 0x00,
        SlideUp = 0x01,
        SlideDown = 0X02,
        TonePortamento = 0x03,
        Vibrato = 0x04,
        TonePortamentoVolumeSlide = 0x05,
        VibratoVolumeSlide = 0x06,
        Tremolo = 0x07,
        UnusedSync = 0x08,
        SampleOffset = 0x09,
        VolumeSlide = 0x0A,
        PositionJump = 0x0B,
        SetVolume = 0x0C,
        PatternBreak = 0x0D,
        Extended = 0x0E,
        SetSpeed = 0X0F
    }
}