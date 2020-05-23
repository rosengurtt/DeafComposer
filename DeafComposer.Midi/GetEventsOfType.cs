using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static List<MidiEvent> GetEventsOfType(MidiFile midiFile, MidiEventType type, bool isDeltaAccumulated = false)
        {
            List<MidiEvent> retObj = new List<MidiEvent>();

            foreach (TrackChunk chunk in midiFile.Chunks.ToList())
            {
                long acumulatedTimeInTicks = 0;
                foreach (var eventito in chunk.Events)
                {
                    acumulatedTimeInTicks += eventito.DeltaTime;

                    if (eventito.EventType == type)
                    {
                        var e = eventito.Clone();
                        if (!isDeltaAccumulated)
                            e.DeltaTime = acumulatedTimeInTicks;
                        retObj.Add(e);
                    }
                }
            }
            if (isDeltaAccumulated)
                return retObj;
            else
                return ConvertAccumulatedTimeToDeltaTime(retObj);
        }
        public static List<MidiEvent> GetEventsOfType(string base64encodedMidiFile, MidiEventType type)
        {
            var midiFile = MidiFile.Read(base64encodedMidiFile);
            return GetEventsOfType(midiFile, type);
        }

        public static List<MidiEvent> GetEventsOfType(TrackChunk chunk, MidiEventType type)
        {
            List<MidiEvent> retObj = new List<MidiEvent>();
            long acumulatedTimeInTicks = 0;
            foreach (var eventito in chunk.Events)
            {
                acumulatedTimeInTicks += eventito.DeltaTime;

                if (eventito.EventType == type)
                {
                    var e = eventito.Clone();
                    e.DeltaTime = acumulatedTimeInTicks;
                    retObj.Add(e);
                }
            }
            return ConvertAccumulatedTimeToDeltaTime(retObj);
        }
    }
}
