using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Generates the list of bar entities of a midi file
        /// </summary>
        /// <param name="base64encodedMidiFile"></param>
        /// <returns></returns>
        public static List<Bar> GetBarsOfSong(string base64encodedMidiFile, SongSimplification songSimplification)
        {
            List<Bar> retObj = new List<Bar>();
            int barNumber = 1;

            var ticksPerQuarterNote = GetTicksPerQuarterNote(base64encodedMidiFile);
            var songDurationInTicks = GetSongDurationInTicks(base64encodedMidiFile);
            var timeSignatureEventsAsMidiEvents = GetEventsOfType(base64encodedMidiFile, MidiEventType.TimeSignature);
            var TempoEventsAsMidiEvents = GetEventsOfType(base64encodedMidiFile, MidiEventType.SetTempo);
            var timeSignatureEvents = RemoveRedundantTimeSignatureEvents(ConvertDeltaTimeToAccumulatedTime(timeSignatureEventsAsMidiEvents));
            var TempoEvents = RemoveRedundantTempoEvents(QuantizeTempos(ConvertDeltaTimeToAccumulatedTime(TempoEventsAsMidiEvents)));


            // create variable that holds the time signature at the place we are analyzing
            // initialize it with default
            var currentTimeSignature = new TimeSignatureEvent
            {
                Numerator = 4,
                Denominator = 4
            };
            // create variable to hold the tempo at the place we are analyzing, initialize with default
            int currentTempo = 500000;

            int timeSigIndex = 0; // this is an index in the timeSignatureEvents List
            int tempoIndex = 0;     // index in the TempoEvents list
            long currentTick = 0;
            long lastTickOfBarToBeAdded = 0;
            int currentTicksPerBeat = 0;

            // continue until reaching end of song
            while (currentTick < songDurationInTicks &&
                lastTickOfBarToBeAdded < songDurationInTicks + (currentTicksPerBeat * currentTimeSignature.Numerator ))
            {
                // if there are tempo event changes, get the current tempo
                if (TempoEvents.Count > 0 && TempoEvents[tempoIndex].DeltaTime <= currentTick)
                    currentTempo = (int)TempoEvents[tempoIndex].MicrosecondsPerQuarterNote;

                // if there are time signature changes, get the current one
                if (timeSignatureEvents.Count > 0 && timeSignatureEvents[timeSigIndex].DeltaTime <= currentTick)
                    currentTimeSignature = timeSignatureEvents[timeSigIndex];
                // get the ticks per beat at this moment
                currentTicksPerBeat = ticksPerQuarterNote * 4 / currentTimeSignature.Denominator;

                // get the time in ticks of the next time signature change
                long timeOfNextTimeSignatureEvent = songDurationInTicks;
                if (timeSignatureEvents.Count - 1 > timeSigIndex)
                    timeOfNextTimeSignatureEvent = timeSignatureEvents[timeSigIndex + 1].DeltaTime;

                // get the time in ticks of the next tempo event change
                long timeOfNextSetTempoEvent = songDurationInTicks;
                if (TempoEvents.Count - 1 > tempoIndex)
                    timeOfNextSetTempoEvent = TempoEvents[tempoIndex + 1].DeltaTime;

                // get the end of the current bar
                lastTickOfBarToBeAdded = currentTimeSignature.Numerator * currentTicksPerBeat + currentTick;

                // keep adding new bars to the return object until we reach a change of time signature or change of tempo or end of song
                while ((lastTickOfBarToBeAdded < timeOfNextTimeSignatureEvent &&
                       lastTickOfBarToBeAdded < timeOfNextSetTempoEvent)
                        ||
                       lastTickOfBarToBeAdded < songDurationInTicks + (currentTicksPerBeat * currentTimeSignature.Numerator))
                {
                    // Add bar
                    var bar = new Bar
                    {
                        BarNumber = barNumber++,
                        TicksFromBeginningOfSong = currentTick,
                        TimeSignature = new TimeSignature
                        {
                            Numerator = currentTimeSignature.Numerator,
                            Denominator = currentTimeSignature.Denominator
                        },
                        TempoInMicrosecondsPerQuarterNote = currentTempo
                    };
                    bar.HasTriplets = HasBarTriplets(songSimplification, bar);
                    retObj.Add(bar);
                    // update currentTick and lastTickOfBarToBeAdded for next iteration
                    currentTick = lastTickOfBarToBeAdded;
                    lastTickOfBarToBeAdded += currentTimeSignature.Numerator * currentTicksPerBeat;
                }
                // if we get here it's probably because we reached a change in tempo or time signature. Update indexes
                if (lastTickOfBarToBeAdded >= timeOfNextTimeSignatureEvent && timeSigIndex < timeSignatureEvents.Count - 1)
                    timeSigIndex++;
                if (lastTickOfBarToBeAdded >= timeOfNextSetTempoEvent && tempoIndex < TempoEvents.Count - 1)
                    tempoIndex++;
            }
            return retObj;
        }

        private static List<TimeSignatureEvent> RemoveRedundantTimeSignatureEvents(IEnumerable<MidiEvent> events)
        {
            var retObj = new List<TimeSignatureEvent>();
            var auxObj = new List<TimeSignatureEvent>();
            // Remove duplicates
            foreach (var e in events)
            {
                var tse = (TimeSignatureEvent)e;
                if (auxObj.Where(ev => ev.Numerator == tse.Numerator &&
                ev.DeltaTime == tse.DeltaTime &&
                ev.Denominator == tse.Denominator ).ToList().Count == 0)
                {
                    auxObj.Add(tse);
                }
            }
            // Remove consecutive events that are actually identical
            for (var i = 0; i < auxObj.Count; i++)
            {
                if (i== auxObj.Count-1 ||
                    auxObj[i].Numerator != auxObj[i + 1].Numerator || auxObj[i].Denominator != auxObj[i + 1].Denominator)
                    retObj.Add(auxObj[i]);
            }
            return retObj.ToList();
        }
        private static List<SetTempoEvent> RemoveRedundantTempoEvents(IEnumerable<MidiEvent> events)
        {
            var retObj = new List<SetTempoEvent>();
            var auxObj = new List<SetTempoEvent>();
            foreach (var e in events)
            {
                var ste = (SetTempoEvent)e;
                if (retObj.Where(ev => ev.MicrosecondsPerQuarterNote == ste.MicrosecondsPerQuarterNote &&
                ev.DeltaTime == ste.DeltaTime).ToList().Count == 0)
                {
                    retObj.Add(ste);
                }
            }

            // Remove consecutive events that are actually identical
            for (var i = 0; i < auxObj.Count; i++)
            {
                if (i == auxObj.Count - 1 ||
                    auxObj[i].MicrosecondsPerQuarterNote != auxObj[i + 1].MicrosecondsPerQuarterNote)
                    retObj.Add(auxObj[i]);
            }
            return retObj.ToList();
        }
    }
}
