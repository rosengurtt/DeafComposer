using Melanchall.DryWetMidi.Core;
using System;
using System.IO;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static string Base64EncodeMidiFile(MidiFile midiFile)
        {
            using (MemoryStream memStream = new MemoryStream(1000000))
            {
                midiFile.Write(memStream);
                var bytes = memStream.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }
    }
}