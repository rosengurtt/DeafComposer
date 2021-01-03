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
            var keySignatureEvents = ConvertDeltaTimeToAccumulatedTime(GetEventsOfType(base64encodedMidiFile, MidiEventType.KeySignature));


            // create variable that holds the time signature at the place we are analyzing
            // initialize it with default
            var currentTimeSignature = new TimeSignatureEvent
            {
                Numerator = 4,
                Denominator = 4
            };
            var currentKeySignature = GetKeySignatureOfSong(songSimplification.Notes);
            // create variable to hold the tempo at the place we are analyzing, initialize with default
            int currentTempo = 500000;

            int timeSigIndex = 0; // this is an index in the timeSignatureEvents List
            int tempoIndex = 0;     // index in the TempoEvents list
            int keySigIndex = 0; // index in the keySignatureEvents list
            long currentTick = 0;
            long lastTickOfBarToBeAdded = 0;
            int currentTicksPerBeat = 0;

            // continue until reaching end of song
            while (currentTick < songDurationInTicks &&
                lastTickOfBarToBeAdded < songDurationInTicks + (currentTicksPerBeat * currentTimeSignature.Numerator))
            {
                // if there are tempo event changes, get the current tempo
                if (TempoEvents.Count > 0 && TempoEvents[tempoIndex].DeltaTime <= currentTick)
                    currentTempo = (int)TempoEvents[tempoIndex].MicrosecondsPerQuarterNote;

                // if there are time signature changes, get the current one
                // we use the while because there may be more than 1 time signature events in the same tick, we want the last one
                var j = 0;
                while (timeSignatureEvents.Count > j && timeSignatureEvents[timeSigIndex + j].DeltaTime <= currentTick) j++;
                if (j > 0)
                {
                    currentTimeSignature = timeSignatureEvents[timeSigIndex+j-1];
                }

                // if there are key signature changes, get the current one
                var i = 0;
                while (keySignatureEvents.Count > i && keySignatureEvents[keySigIndex + i].DeltaTime <= currentTick) i++;
                if (i >= 1)
                {
                    currentKeySignature = ((KeySignatureEvent)keySignatureEvents[keySigIndex+i-1]).Key;
                }
             

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

                long timeOfNextKeySignatureEvent = songDurationInTicks;
                if (keySignatureEvents.Count - 1 > keySigIndex)
                    timeOfNextKeySignatureEvent = keySignatureEvents[keySigIndex + 1].DeltaTime;

                // get the end of the current bar
                lastTickOfBarToBeAdded = currentTimeSignature.Numerator * currentTicksPerBeat + currentTick;

                // keep adding new bars to the return object until we reach a change of time signature or change of tempo or end of song
                while ((lastTickOfBarToBeAdded < timeOfNextTimeSignatureEvent &&
                       lastTickOfBarToBeAdded < timeOfNextSetTempoEvent &&
                       lastTickOfBarToBeAdded < timeOfNextKeySignatureEvent)
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
                        TempoInMicrosecondsPerQuarterNote = currentTempo,
                        KeySignature = currentKeySignature
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
                if (lastTickOfBarToBeAdded >= timeOfNextKeySignatureEvent && keySigIndex < keySignatureEvents.Count - 1)
                    keySigIndex++;
            }
            return retObj;
        }

        /// <summary>
        /// When a midi file has no key signature events, we want to deduce what is the best key signature to use
        /// and add it to all bars
        /// This function returns a number that if positive means the number of sharps and if negative the number of
        /// flats
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static int GetKeySignatureOfSong(List<Note> notes)
        {
            var neededAlterations = new int[12];
            for (var i = 0; i < 12; i++)
            {
                neededAlterations[i] = 0;
                var notAlteredPitches = new List<int>() { 0 + i, (2 + i) % 12, (4 + i) % 12, (5 + i) % 12, (7 + i) % 12, (9 + i) % 12, (11 + i) % 12 };

                foreach (var n in notes)
                {
                    if (!notAlteredPitches.Where(x => n.Pitch % 12 == x).Any()) neededAlterations[i]++;
                }
            }
            var key= neededAlterations.ToList().IndexOf(neededAlterations.Min());
            switch (key)
            {
                case 0:
                    return 0;
                case 1:
                    return -5;
                case 2:
                    return 2;
                case 3:
                    return -3;
                case 4:
                    return 4;
                case 5:
                    return -1;
                case 6:
                    return 6;
                case 7:
                    return 1;
                case 8:
                    return -4;
                case 9:
                    return 3;
                case 10:
                    return -2;
                case 11:
                    return 5;
                default:
                    return 0;
            }
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
