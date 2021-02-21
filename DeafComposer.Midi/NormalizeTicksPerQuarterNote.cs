using Melanchall.DryWetMidi.Core;
using System;
using System.IO;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static string NormalizeTicksPerQuarterNote(string base64encodedMidiFile)
        {
            short standardTicksPerQuarterNote = 96;
            var midiFile = MidiFile.Read(base64encodedMidiFile);
            midiFile = ConvertDeltaTimeToAccumulatedTime(midiFile);
            var originalTicksPerQuarterNote = GetTicksPerQuarterNote(base64encodedMidiFile);

            double conversionRate = standardTicksPerQuarterNote / (double)originalTicksPerQuarterNote;
            foreach (TrackChunk chunk in midiFile.Chunks)
            {
                foreach (var eventito in chunk.Events)
                {
                    eventito.DeltaTime = (long)Math.Round(conversionRate * eventito.DeltaTime);
                }
            }
            midiFile = ConvertAccumulatedTimeToDeltaTime(midiFile);
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision(standardTicksPerQuarterNote);

            return Base64EncodeMidiFile(midiFile);
        }
    }
}
