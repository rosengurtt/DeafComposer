using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        private static List<MidiEvent> GetSortedEventsList(List<MidiEvent> events)
        {
            var returnObj = new List<MidiEvent>();
            foreach (var e in events) returnObj.Add(e.Clone());
            for (int i = 0; i < returnObj.Count - 1; i++)
            {
                for (int j = i + 1; j < returnObj.Count; j++)
                {
                    if (returnObj[i].DeltaTime > returnObj[j].DeltaTime)
                    {
                        var aux = returnObj[i].Clone();
                        returnObj[i] = returnObj[j];
                        returnObj[j] = aux;
                    }
                }
            }
            return returnObj;
        }
    }
}
