using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
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
                    currentKeySignature = new KeySignature
                    {
                        key = ((KeySignatureEvent)keySignatureEvents[keySigIndex + i - 1]).Key,
                        scale = (ScaleType)((KeySignatureEvent)keySignatureEvents[keySigIndex + i - 1]).Scale
                    };
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
        ///         /// 
        /// The reasoning to determine the key singature is this:
        /// We have to determine first if the song is in a minor scale or a major scale. To determine this, we try to find
        /// the tonic and see if the third used most frequently is a minor or a major 3rd
        /// 
        /// The best key signature is the one that will need less alterations. So we consider all possible keys 
        /// and we select the one that needs less alterations
        /// 
        /// In the case of minor scales we don't include the 6th and 7th of the scale to do the calculations, because 
        /// in minor scales the 6th and 7th have 2 possiblities equally likely to be used
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static KeySignature GetKeySignatureOfSong(List<Note> notes)
        {
            var tonic = GetTonicOfNotes(notes);
            var scale = GetScaleOfNotes(notes, tonic);
            var retObj = new KeySignature { scale = scale };

            if (tonic == 0 && scale == ScaleType.major || tonic == 9 && scale == ScaleType.minor) retObj.key = 0;
            if (tonic == 7 && scale == ScaleType.major || tonic == 4 && scale == ScaleType.minor) retObj.key = 1;
            if (tonic == 2 && scale == ScaleType.major || tonic == 11 && scale == ScaleType.minor) retObj.key =2;
            if (tonic == 9 && scale == ScaleType.major || tonic == 6 && scale == ScaleType.minor) retObj.key = 3;
            if (tonic == 4 && scale == ScaleType.major || tonic == 1 && scale == ScaleType.minor) retObj.key = 4;
            if (tonic == 11 && scale == ScaleType.major || tonic == 8 && scale == ScaleType.minor) retObj.key = 5;
            if (tonic == 6 && scale == ScaleType.major || tonic == 3 && scale == ScaleType.minor) retObj.key = 6;
            if (tonic == 5 && scale == ScaleType.major || tonic == 2 && scale == ScaleType.minor) retObj.key = -1;
            if (tonic == 10 && scale == ScaleType.major || tonic == 7 && scale == ScaleType.minor) retObj.key = -2;
            if (tonic == 3 && scale == ScaleType.major || tonic == 0 && scale == ScaleType.minor) retObj.key = -3;
            if (tonic == 8 && scale == ScaleType.major || tonic == 5 && scale == ScaleType.minor) retObj.key = -4;
            if (tonic == 1 && scale == ScaleType.major || tonic == 10 && scale == ScaleType.minor) retObj.key = -5;

            return retObj;
        }
        private static ScaleType GetScaleOfNotes(List<Note> notes, int tonic)
        {
            var thirdMajor = (tonic + 4) % 12;
            var thirdMinor = (tonic + 3) % 12;
            var thirdMajorFrequency = notes.Where(x => x.Pitch % 12 == thirdMajor).Count();
            var thirdMinorFrequency = notes.Where(x => x.Pitch % 12 == thirdMinor).Count();

            if (thirdMajorFrequency > thirdMinorFrequency) return ScaleType.major;
            return ScaleType.minor;
        }
        private static int GetTonicOfNotes(List<Note> notes)
        {
            // totalUseOfPitch is a value that takes in consideration the times a pitch is used, the volume and the duration of
            // time it is played. We calculate it for each pitch
            var totalUseOfPitch = new long[12];
            var firstRoot = GetTonicOfFirstAndLastChords(notes, "first");
            var lastRoot = GetTonicOfFirstAndLastChords(notes, "last");

            // when several notes are played at the same timethe higher notes and the lower notes are heard more than middle notes
            // the percentage of time a pitch is the highest (or the lowest) pitch heard gives information about its importance
            var timePitchIsHighestNote = new long[12];
            var timePitchIsLowestNote = new long[12];
            var probability = new double[12];
            for (var i = 0; i < 12; i++)
            {
                totalUseOfPitch[i] = notes.Where(x => x.Pitch % 12 == i).Select(y => y.Volume * y.DurationInTicks).Sum();
                timePitchIsHighestNote[i] = GetHighestOrLowestPitchesOfNotes(notes, "high")
                    .Where(x => x.Pitch % 12 == i).Select(y => y.Volume * y.DurationInTicks).Sum();
                timePitchIsLowestNote[i] = GetHighestOrLowestPitchesOfNotes(notes, "low")
                    .Where(x => x.Pitch % 12 == i).Select(y => y.Volume * y.DurationInTicks).Sum();
            }
            long totalUseForAllNotes = totalUseOfPitch.Sum();
            long totalPitchIsHighesttNote = timePitchIsHighestNote.Sum();
            long totalPitchIsLowestNote = timePitchIsLowestNote.Sum();

            for (var i = 0; i < 12; i++)
            {
                probability[i] = totalUseOfPitch[i] / (double)totalUseForAllNotes +
                    // we give less importance to timePitchIsHighestNote than to totalUseOfPitch, so we divide by 2
                    timePitchIsHighestNote[i] / ((double)totalPitchIsHighesttNote * 2) +
                    // same with timePitchIsLowestNote
                    timePitchIsLowestNote[i] / ((double)totalPitchIsLowestNote * 2);
                if (i == firstRoot) probability[i] *= 1.2;
                if (i == lastRoot) probability[i] *= 1.2;
            }

            var winner = probability.ToList().IndexOf(probability.Max());
            return winner;
        }
        /// <summary>
        /// Used to get the notes in a group of notes that are heard as the highest notes or as the lowest notes
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> GetHighestOrLowestPitchesOfNotes(List<Note> notes, string which)
        {
            var retObj = new List<Note>();
            foreach(var n in notes)
            {
                if (which.ToLower().Contains("high"))
                {
                    if (notes.Where(x => x.Pitch > n.Pitch && x.StartSinceBeginningOfSongInTicks < n.EndSinceBeginningOfSongInTicks &&
                    x.EndSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks).Count() == 0)
                        retObj.Add(n);
                }
                else
                {
                    if (notes.Where(x => x.Pitch < n.Pitch && x.StartSinceBeginningOfSongInTicks < n.EndSinceBeginningOfSongInTicks &&
                                   x.EndSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks).Count() == 0)
                        retObj.Add(n);
                }
            }
            return retObj;
        }

        private static int GetTonicOfFirstAndLastChords(List<Note> notes, string which)
        {
            var firstNote = notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks).FirstOrDefault();
            var firstChord= notes.Where(x => x.StartSinceBeginningOfSongInTicks < firstNote.EndSinceBeginningOfSongInTicks).ToList();

            var lastNote = notes.OrderByDescending(x => x.EndSinceBeginningOfSongInTicks).FirstOrDefault();
            var lastChord = notes.Where(x => x.EndSinceBeginningOfSongInTicks >= lastNote.StartSinceBeginningOfSongInTicks).ToList();

            if (which.ToLower().Contains("first")) return GetRootOfChord(firstChord);
            else return GetRootOfChord(lastChord);
        }

        private static int GetRootOfChord(List<Note> notes)
        {
            var probability = new int[notes.Count];
            for (var i =0; i< notes.Count;i++)
            {
                if (notes.Where(x => x.Pitch % 12 == (notes[i].Pitch + 7) % 12).Any()) probability[i]+=5;
                if (notes.Where(x => (x.Pitch % 12 == (notes[i].Pitch + 3) % 12) ||
                                    (x.Pitch % 12 == (notes[i].Pitch + 4) % 12)).Any()) probability[i]+=2;
            }
            var winner = probability.ToList().IndexOf(probability.Max());
            return notes[winner].Pitch % 12;
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
