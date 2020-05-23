using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static List<MidiEvent> ConvertAccumulatedTimeToDeltaTime(List<MidiEvent> events)
        {
            var returnObj = GetSortedEventsList(events);

            for (int i = returnObj.Count - 1; i > 0; i--)
                returnObj[i].DeltaTime -= returnObj[i - 1].DeltaTime;
            return returnObj;
        }
        public static List<MidiEvent> ConvertAccumulatedTimeToDeltaTime(TrackChunk chunky)
        {
            return ConvertAccumulatedTimeToDeltaTime(chunky.Events.ToList());
        }
        public static MidiFile ConvertAccumulatedTimeToDeltaTime(MidiFile midiFile)
        {
            foreach (TrackChunk chunk in midiFile.Chunks)
            {
                chunk.Events._events = ConvertAccumulatedTimeToDeltaTime(chunk.Events._events);
            }
            return midiFile;

        }
    }
}
