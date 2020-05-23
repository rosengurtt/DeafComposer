using System;
using System.Collections.Generic;
using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Core;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static List<TempoChange> GetTempoChanges(string base64encodedMidiFile)
        {
            var retObj = new List<TempoChange>();
            var setTempoEvents = GetEventsOfType(base64encodedMidiFile, MidiEventType.SetTempo);
            var TempoEvents = QuantizeTempos(ConvertDeltaTimeToAccumulatedTime(setTempoEvents));
            foreach (var te in TempoEvents)
            {
                retObj.Add(new TempoChange()
                {
                    MicrosecondsPerQuarterNote = (int)te.MicrosecondsPerQuarterNote,
                    TicksSinceBeginningOfSong = te.DeltaTime
                }
                    );
            }
            return retObj;
        }
    }
}
