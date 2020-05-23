using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Removes tempo changes that change the tempo by less than 15%
        /// </summary>
        /// <param name="evs"></param>
        /// <returns></returns>
        private static List<SetTempoEvent> QuantizeTempos(List<MidiEvent> evs)
        {
            var retObj = new List<SetTempoEvent>();
            int threshold = 15; // If the tempo change is less than 15%, ignore it
            var tempoEvs = new List<SetTempoEvent>();
            foreach (var ev in evs)
            {
                if (!(ev is SetTempoEvent)) continue;
                var evito = (SetTempoEvent)ev;
                tempoEvs.Add(evito);
            }
            if (tempoEvs.Count == 0) return retObj;
            retObj.Add(tempoEvs[0]);
            for (int i = 0; i < tempoEvs.Count - 1; i++)
            {
                var change = Math.Abs(tempoEvs[i].MicrosecondsPerQuarterNote -
                    tempoEvs[i + 1].MicrosecondsPerQuarterNote);
                var average = (tempoEvs[i].MicrosecondsPerQuarterNote +
                    tempoEvs[i + 1].MicrosecondsPerQuarterNote) / 2;
                var percentChange = (change / average) * 100;
                if (percentChange > threshold)
                    retObj.Add(tempoEvs[i + 1]);
            }
            return retObj;
        }
    }
}
