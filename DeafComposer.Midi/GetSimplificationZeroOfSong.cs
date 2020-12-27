using DeafComposer.Models.Entities;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using DeafComposer.Models.Helpers;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static SongSimplification GetSimplificationZeroOfSong(string base64encodedMidiFile)
        {

            var notesObj0 = GetNotesOfMidiFile(base64encodedMidiFile);
            var midiEncoded1 = GetMidiBytesFromNotes(notesObj0);
            var dif0 = CompareMidiFiles(base64encodedMidiFile, midiEncoded1);
            var notesEvolution = GetNotesEvolution(notesObj0);

            var notesObj1 = QuantizeNotes(notesObj0);
            var dif1 = CompareListOfNotes(notesObj0, notesObj1);
            notesEvolution = GetNotesEvolution(notesObj1, notesEvolution);


            var notesObj2 = CorrectNotesTimings(notesObj1);
            var dif2 = CompareListOfNotes(notesObj1, notesObj2);
            notesEvolution = GetNotesEvolution(notesObj2, notesEvolution);


            var notesObj3 = RemoveDuplicationOfNotes(notesObj2);
            var dif3 = CompareListOfNotes(notesObj2, notesObj3);
            notesEvolution = GetNotesEvolution(notesObj3, notesEvolution);


            // Split voices that have more than one melody playing at the same time
            var notesObj4 = SplitPolyphonicVoiceInMonophonicVoices(notesObj3);
            var dif4 = CompareListOfNotes(notesObj3, notesObj4);
            notesEvolution = GetNotesEvolution(notesObj4, notesEvolution);

            // Reorder voices so when we have for ex the left and right hand of a piano in 2 voices, the right hand comes first
            var notesObj5 = ReorderVoices(notesObj4);
            var dif5 = CompareListOfNotes(notesObj4, notesObj5);
            notesEvolution = GetNotesEvolution(notesObj5, notesEvolution);


            if (!AreVoicesMonophonic(notesObj5))
            {
                var kiki = GetProblematicNotes(notesObj5);
            }
            var retObj = new SongSimplification()
            {
                Notes = notesObj5,
                SimplificationVersion = 0,
                NumberOfVoices = GetVoicesOfNotes(notesObj5).Count()
            };
            return retObj;
        }

        
        // Used to check that we have splitted the notes in different voices correctly
        // After the split, we shouldn't have simultaneous notes that are not exactly simultaneous
        // When 2 or more notes start and end together, we consider them a chord and it is OK to have
        // them in a monophonic voice. But otherwise, if they play together for a while but not all
        // the time, then they are problematic and they shouldn't be there, something went wrong
        // when we did the split
        private static List<List<Note>> GetProblematicNotes(List<Note> notes, int tolerance = 0, bool notesOfSamePitch = false)
        {
            var retObj = new List<List<Note>>();
            var voices = GetVoicesOfNotes(notes);
            foreach (var v in voices)
            {
                foreach (var n in notes.Where(x => x.Voice == v))
                {
                    foreach (var m in notes.Where(y => y.Voice == v && n.TempId != y.TempId))
                    {
                        if (GetIntersectionOfNotesInTicks(m, n) == 0) continue;
             
                        var soret = new List<Note>();
                        if (!AreNotesExactlySimultaneous(m, n))
                        {
                            soret.Add(m);
                            soret.Add(n);
                        }
                        if (soret.Count > 0) retObj.Add(soret);
                    }
                }
            }
            return retObj;
        }
      
        private static List<NoteEvolution> GetNotesOfDurationZero(List<NoteEvolution> notesEv)
        {
            var retObj = new List<NoteEvolution>();
            foreach (var ne in notesEv)
            {
                for(var i = 0; i < ne.start.Count; i++)
                {
                    if (ne.start[i] == ne.end[i] && !retObj.Contains(ne)) retObj.Add(ne);
                }
            }
            return retObj;
        }


        /// <summary>
        /// If we have 2 notes with the same pitch and the same instrument playing at the same time, this
        /// is problematic for the analysis and displaying. So we do one of 2 things:
        /// - if the notes start at the same time (or almost at the same time) we remove one
        /// - if the notes start in different times, we shorten the first, so they don't play simultaneously
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> RemoveDuplicationOfNotes(List<Note> notes)
        {
            // We first copy all the notes to retObj, we will then remove and alter notes in rettObj, but the original notes are left unchanged
            var retObj = notes.Clone();
            // If 2 notes with the same pitch and the same instrument start at the same time, we remove the 
            // one with the lower volume, or if the volume is more or less the same, the shortest one
            var notesToRemove = new List<Note>();
            var volumeTolerance = 5;    // The amount the volumes have to differ to consider them different
            var voices = GetVoicesOfNotes(retObj);
            foreach (var v in voices)
            {
                var notesOfVoice = retObj.Where(w => w.Voice == v)
                                    .OrderBy(z => z.StartSinceBeginningOfSongInTicks).ToList();
                for (var i = 0; i < notesOfVoice.Count - 1; i++)
                {
                    for (var j = i + 1; j < notesOfVoice.Count; j++)
                    {
                        var ni = notesOfVoice[i];
                        var nj = notesOfVoice[j];
                        // This deduplication applies only when notes are played by the same instrument
                        if (ni.Instrument != nj.Instrument) continue;
                        // Check if they play simultaneously and if they have the same pitch
                        if (GetIntersectionOfNotesInTicks(ni, nj) == 0 || ni.Pitch != nj.Pitch)
                            continue;
                        // Found duplicates. Check if they start together or not
                        var timeTolerance = GetTolerance(ni, nj);
                        if (Math.Abs(ni.StartSinceBeginningOfSongInTicks - nj.StartSinceBeginningOfSongInTicks) < timeTolerance)
                        {
                            // They start together, Select the one to remove and store it in notesToRemove
                            if (Math.Abs(ni.Volume - nj.Volume) > volumeTolerance)
                            {
                                var quieterNote = ni.Volume < nj.Volume ? ni : nj;
                                notesToRemove.Add(quieterNote);
                                continue;
                            }
                            var shorterNote = ni.DurationInTicks < nj.DurationInTicks ? ni : nj;
                            notesToRemove.Add(shorterNote);
                        }
                        else
                        {
                            // They don't start together. Shorten the first one
                            var firstNote = ni.StartSinceBeginningOfSongInTicks < nj.StartSinceBeginningOfSongInTicks ? ni : nj;
                            var secondNote = ni.StartSinceBeginningOfSongInTicks < nj.StartSinceBeginningOfSongInTicks ? nj : ni;
                            firstNote.EndSinceBeginningOfSongInTicks = secondNote.StartSinceBeginningOfSongInTicks;
                        }
                    }
                }

            }
            // Now remove the duplicate notes
            foreach (var n in notesToRemove) retObj.Remove(n);
            return retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
        }

        /// <summary>
        /// When we have 2 notes starts or ends happening almost at the same time but not exactly
        /// in most cases they are meant to happen at the same time. If one of the notes is very short, then the fact
        /// that they start at short different times may be on purpose like when we have an embelishment
        /// We try to find the notes that are meant to start together but are not, and make them start exactly at the
        /// same time. We select the time so it matches a suitable subdivision of the beat
        /// 
        /// When we have consecutive notes in a voice, where each one ends approximately when the next starts, but
        /// one of the notes is quite larger or shorter we can assume it is a mistake. If it is much, much larger, like
        /// a half, when the other notes are sixteens we assume it is on purpose. But if is an eight or a quarter and
        /// the others are sixteens, we assume is a mistake and we fix it
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> CorrectNotesTimings(List<Note> notes)
        {
            // We first copy all the notes to retObj, we will then remove and alter notes in rettObj, but the original notes are left unchanged
            var retObj = notes.Clone();
            // In this loop we do the first type of correction when 2 notes start and stop aprox at the same time
            for (var i = 0; i < retObj.Count - 1; i++)
            {
                for (var j = i + 1; j < retObj.Count; j++)
                {
                    var ni = retObj[i];
                    var nj = retObj[j];
                    var tolerance = GetTolerance(ni, nj);
                    var dif = Math.Abs(ni.StartSinceBeginningOfSongInTicks - nj.StartSinceBeginningOfSongInTicks);
                    // If any of the notes is very short, then don't change the timings
                    if (ni.DurationInTicks < dif * 4 || nj.DurationInTicks < dif * 4) continue;
                    if (EventsAreAlmostSimultaneousButNotExactly(ni.StartSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks, tolerance))
                    {
                        var bestTime = GetMostAppropriateTime(ni.StartSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks);
                        ni.StartSinceBeginningOfSongInTicks = bestTime;
                        nj.StartSinceBeginningOfSongInTicks = bestTime;
                    }
                    if (EventsAreAlmostSimultaneousButNotExactly(ni.EndSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks, tolerance))
                    {
                        var bestTime = GetMostAppropriateTime(ni.EndSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks);
                        ni.EndSinceBeginningOfSongInTicks = bestTime;
                        nj.StartSinceBeginningOfSongInTicks = bestTime;
                    }
                }
            }

            // In this loop we make the second type of correction, when a note has a wrong duration
            var voices = GetVoicesOfNotes(retObj);
            foreach (var v in voices)
            {
                var notesOfVoice = notes.Where(x => x.Voice == v).OrderBy(y => y.StartSinceBeginningOfSongInTicks).ToList();
                List<Note> alreadyEvaluated = new List<Note>();
                while (true)
                {
                    var nextGroupOf4ConsecutiveNotes = GetNextGroupOf4ConsecutiveWithNoOverlap(notesOfVoice, alreadyEvaluated);
                    if (nextGroupOf4ConsecutiveNotes.Count == 0) break;
                    var averageDuration = nextGroupOf4ConsecutiveNotes.Average(n => n.DurationInTicks);
                    var averagePitch = nextGroupOf4ConsecutiveNotes.Average(n => n.Pitch);
                    for (var s = 0; s < 4; s++)
                    {
                        var startPoint = nextGroupOf4ConsecutiveNotes[s].StartSinceBeginningOfSongInTicks;
                        var tempIdsOfSequence = nextGroupOf4ConsecutiveNotes.Select(x => x.TempId).ToList();
                        var candidatesToFix = notesOfVoice
                            .Where(x => x.StartSinceBeginningOfSongInTicks > startPoint - 2 * averageDuration &&
                                        x.StartSinceBeginningOfSongInTicks < startPoint &&
                                        Math.Abs(x.Pitch - averagePitch) < 12 &&
                                        !tempIdsOfSequence.Contains(x.TempId)).ToList();
                        foreach (var candidate in candidatesToFix)
                        {
                            // we check that it starts aprox in the right place and with a duration that is not too far from the expected duration
                            if ((Math.Abs(candidate.StartSinceBeginningOfSongInTicks + averageDuration - startPoint) < Math.Min(10, averageDuration / 4))
                                && candidate.DurationInTicks > averageDuration / 2 && candidate.DurationInTicks < averageDuration * 3)
                            {
                                //fix duration of corresponding note in retObj                           
                                retObj.Where(n => n.TempId == candidate.TempId).FirstOrDefault().EndSinceBeginningOfSongInTicks = startPoint;
                            }
                        }
                    }
                    alreadyEvaluated.Add( nextGroupOf4ConsecutiveNotes[0]);
                }
            }

            return retObj;
        }
        // Checks if a group of consecutive notes are played in succession, where each starts where the previous ended
        // It allows for small imperfections
        private static bool NotesAreMeantConsecutiveWithNoInterlap(List<Note> notes)
        {
            var notesAvgDuration = notes.Average(x => x.DurationInTicks);
            long totalInterlap = 0;
            for (var i = 0; i < notes.Count - 1; i++)
            {
                totalInterlap += Math.Abs(notes[i].EndSinceBeginningOfSongInTicks - notes[i + 1].StartSinceBeginningOfSongInTicks);
            }
            var averageInterlap = totalInterlap / (double)notes.Count;
            if (notesAvgDuration / averageInterlap > 3) return true;
            return false;
        }

        private static List<Note> GetNextGroupOf4ConsecutiveWithNoOverlap(List<Note> notes, List<Note> alreadyEvaluated)
        {
            var retObj = new List<Note>();
            for (var i = 0; i < notes.Count - 4; i++)
            {
                // Find the first note we have to evaluate
                if (alreadyEvaluated.Count > 0 &&
                    notes[i].StartSinceBeginningOfSongInTicks < alreadyEvaluated.Max(x => x.StartSinceBeginningOfSongInTicks))
                    continue;
                if (alreadyEvaluated.Select(x => x.TempId).Contains(notes[i].TempId)) 
                    continue;

                var firstNote = notes[i];
                var averageDuration = firstNote.DurationInTicks;
                var iteration1Limit = Math.Min(firstNote.StartSinceBeginningOfSongInTicks + 4 * averageDuration, notes.Count - 3);
                for (var j = i + 1; notes[j].StartSinceBeginningOfSongInTicks < iteration1Limit; j++)
                {
                    averageDuration = (firstNote.DurationInTicks + notes[j].DurationInTicks) / 2;
                    var startDifferenceBetween1and2 = notes[j].StartSinceBeginningOfSongInTicks - firstNote.StartSinceBeginningOfSongInTicks;
                    var note2StartsWhenNote1Ends = (Math.Abs(startDifferenceBetween1and2 - notes[j].DurationInTicks) * 3 < averageDuration) ? true : false;
                    var overlappingBetween2FirstNotes = Math.Abs(notes[j].StartSinceBeginningOfSongInTicks - firstNote.EndSinceBeginningOfSongInTicks);
                    var durationDifference = Math.Abs(firstNote.DurationInTicks - notes[j].DurationInTicks);
                    var pitchDifferenceBetween1and2 = Math.Abs(firstNote.Pitch - notes[j].Pitch);
                    if (note2StartsWhenNote1Ends &&
                        overlappingBetween2FirstNotes * 4 < averageDuration &&
                        durationDifference * 3 < averageDuration &&
                        pitchDifferenceBetween1and2 < 12)
                    {
                        var secondNote = notes[j];
                        var iteration2Limit = Math.Min(secondNote.StartSinceBeginningOfSongInTicks + 4 * averageDuration, notes.Count - 2);
                        for (var k = j + 1; notes[k].StartSinceBeginningOfSongInTicks < iteration2Limit; k++)
                        {
                            var durationDifferenceWith2previous = Math.Abs(averageDuration - notes[k].DurationInTicks);
                            averageDuration = (firstNote.DurationInTicks + secondNote.DurationInTicks + notes[k].DurationInTicks) / 3;
                            var startDifferenceBetween2and3 = notes[k].StartSinceBeginningOfSongInTicks - secondNote.StartSinceBeginningOfSongInTicks;
                            var note3StartsWhenNote2Ends = (Math.Abs(startDifferenceBetween2and3 - notes[k].DurationInTicks) * 3 < averageDuration) ? true : false;
                            var overlappingBetweenNotes2And3 = Math.Abs(notes[k].StartSinceBeginningOfSongInTicks - secondNote.EndSinceBeginningOfSongInTicks);
                            var pitchDifferenceBetween3andPrevious = Math.Abs((secondNote.Pitch + firstNote.Pitch) / 2 - notes[k].Pitch);
                            if (note3StartsWhenNote2Ends &&
                                overlappingBetweenNotes2And3 < 4 * averageDuration &&
                                durationDifferenceWith2previous * 3 < averageDuration &&
                                pitchDifferenceBetween3andPrevious < 12)
                            {
                                var thirdNote = notes[k];
                                var iteration3Limit = Math.Min(thirdNote.StartSinceBeginningOfSongInTicks + 4 * averageDuration, notes.Count);
                                for (var m = k + 1; notes[m].StartSinceBeginningOfSongInTicks < iteration3Limit; m++)
                                {
                                    var durationDifferenceWith3previous = Math.Abs(averageDuration - notes[m].DurationInTicks);
                                    averageDuration = (firstNote.DurationInTicks + secondNote.DurationInTicks + thirdNote.DurationInTicks + notes[m].DurationInTicks) / 3;
                                    var startDifferenceBetween3and4 = notes[m].StartSinceBeginningOfSongInTicks - thirdNote.StartSinceBeginningOfSongInTicks;
                                    var note4StartsWhenNote3Ends = (Math.Abs(startDifferenceBetween3and4 - notes[m].DurationInTicks) * 3 < averageDuration) ? true : false;
                                    var overlappingBetweenNotes3And4 = Math.Abs(notes[m].StartSinceBeginningOfSongInTicks - thirdNote.EndSinceBeginningOfSongInTicks);
                                    var pitchDifferenceBetween4andPrevious = Math.Abs((firstNote.Pitch + secondNote.Pitch + thirdNote.Pitch) / 3 - notes[m].Pitch);
                                    if (note4StartsWhenNote3Ends &&
                                        overlappingBetweenNotes3And4 < 4 * averageDuration &&
                                        durationDifferenceWith3previous * 3 < averageDuration &&
                                        pitchDifferenceBetween4andPrevious < 12)
                                    {
                                        retObj.Add(firstNote);
                                        retObj.Add(secondNote);
                                        retObj.Add(thirdNote);
                                        retObj.Add(notes[m]);
                                        return retObj;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return retObj;
        }

        
        /// <summary>
        /// When we want to clean the timing of notes, we have to consider the duration of the notes
        /// If 2 quarter notes start with a difference of 2 ticks, they are probably meant to start at the same time
        /// But if 2 thirtysecond notes or one thirtysecond and a quarter start with a difference of 2 ticks, it may
        /// be on purpose, it could be an embelishment for ex.
        /// So the amount of time tolerance to be used to decide if 2 notes are meant to be played together or not
        /// depends on the duration of the notes.
        /// This method returns the appropriate tolerance to be used when checking 2 notes
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static int GetTolerance(Note n, Note m)
        {
            var shorterNote = n.DurationInTicks < m.DurationInTicks ? n : m;
            return Math.Min(shorterNote.DurationInTicks / 4, 6);
        }

        /// <summary>
        /// When we clean the notes timing, we are interested in the events (note starts or note ends) that
        /// happen almost at the same time but not exactly at the same time
        /// This method tells if that is the case for 2 event times expressed in ticks
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private static bool EventsAreAlmostSimultaneousButNotExactly(long e1, long e2, int tolerance)
        {
            if (e1 == e2 || Math.Abs(e1 - e2) > tolerance) return false;
            return true;
        }
      
        /// <summary>
        /// When we are doing cleaning of notes timings, and we have for ex 2 notes that start almost together
        /// but not exactly at the same time, we change them so they start exactly in the same tick
        /// We have to decide wich tick to use. We select the most appropriate one as any between the 2 times
        /// that matches the bigger subdivision of the beat.
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        private static long GetMostAppropriateTime(long e1, long e2)
        {
            var divisor = 96;
            while (divisor > 1)
            {
                for (var i = Math.Min(e1, e2); i <= Math.Max(e1, e2); i++)
                {
                    if (i % divisor == 0) return i;
                    if (i % (divisor/3) == 0) return i;
                }
                divisor = divisor / 2;
            }
            return e1;
        }
        /// <summary>
        /// If 2 notes are not played at the same time, it returns the number of ticks between the end of the
        /// first and the start of the second
        /// Used to fix the starts and ends of notes to avoid having short rests between notes, that would
        /// make the display of the song confusing
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static long GetNumberOfTicksBetweenNotes(Note n, Note m)
        {
            if (GetIntersectionOfNotesInTicks(n, m) > 0) return 0;
            var firstNote = n.StartSinceBeginningOfSongInTicks < m.StartSinceBeginningOfSongInTicks ? n : m;
            var secondNote= n.StartSinceBeginningOfSongInTicks < m.StartSinceBeginningOfSongInTicks ? m : n;
            return secondNote.StartSinceBeginningOfSongInTicks - firstNote.EndSinceBeginningOfSongInTicks;
        }
        
        private static bool AreVoicesMonophonic(List<Note> notes, int tolerance = 0)
        {
            var voices = GetVoicesOfNotes(notes);
            foreach (var v in voices)
            {
                foreach (var n in notes.Where(x => x.Voice == v))
                {
                    if (n.StartSinceBeginningOfSongInTicks >= 1440)
                    {

                    }
                    foreach (var m in notes.Where(y => y.Voice == v && n.TempId != y.TempId))
                    {
                        if (m.StartSinceBeginningOfSongInTicks > 1440)
                        {

                        }
                        if (GetIntersectionOfNotesInTicks(m, n) == 0)  continue;
                        if (!AreNotesExactlySimultaneous(m,n)) return false;
                    }
                }
            }
            return true;
        }
        // Given a group of notes that may be played by several voices, returns all the different
        // voices found in the notes
        private static List<int> GetVoicesOfNotes(List<Note> notes)
        {
            var retObj = new List<int>();
            foreach (var v in notes.Select(n => n.Voice).Distinct())
                retObj.Add(v);
            return retObj;
        }

     
        // Returns true if the 2 voices start and finish at the same time (optionally with some tolerance)
        private static bool AreNotesExactlySimultaneous(Note m, Note n, int tolerance = 0)
        {
            return Math.Abs(m.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) <= tolerance &&
                            Math.Abs(m.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) <= tolerance; ;
        }
        // Returns the number of ticks that the song play both at the same time
        private static long GetIntersectionOfNotesInTicks(Note n, Note m)
        {
            var dif = Math.Min(m.EndSinceBeginningOfSongInTicks, n.EndSinceBeginningOfSongInTicks) -
                Math.Max(m.StartSinceBeginningOfSongInTicks, n.StartSinceBeginningOfSongInTicks);
            return dif > 0 ? dif : 0;
        }
        /// <summary>
        /// We want the drum voices to be at the end, and when there are several voices with the same instrument
        /// we want the higher pitch voices first (in this way if we have the left and the right hand of a piano
        /// in 2 different voices, we want the right hand first)
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> ReorderVoices(List<Note> notes)
        {
            var retObj = new List<Note>();
            var notesCopy= notes.Clone();
            
            // The variable newVoice is used to generate the numbers of the reordered voices
            byte newVoice = 0;
            // first split the notes by instrument
            var noteInstrument = notesCopy.Where(m => m.IsPercussion == false).GroupBy(n => n.Instrument).OrderBy(x => x);
            // now loop by instrument
            foreach (var noteInstGroup in noteInstrument)
            {
                // get notes of instrument
                var instrNotes = notesCopy.Where(n => n.Instrument == noteInstGroup.Key);
                // get the order of this instrument voices by pitch
                var orderedInstrVoices = OrderInstrumentVoicesByPitch(instrNotes);
                // loop on the instrument voices
                foreach (var voice in orderedInstrVoices)
                {
                    // add to the return object the notes of this voice, assigning a new voice number
                    foreach (var n in notesCopy.Where(m => m.Voice == voice))
                    {
                        var m = (Note)n.Clone();
                        m.Voice = newVoice;
                        retObj.Add(m);
                    }
                    // increment the voice number so the next group of notes will get the next available integer
                    newVoice++;
                }
            }
            // now add the percusion notes
            foreach(var n in notesCopy.Where(n => n.IsPercussion == true))
            {
                var m = (Note)n.Clone();
                m.Voice = newVoice;
                retObj.Add(m);
            }
            return retObj.OrderBy(n=>n.StartSinceBeginningOfSongInTicks).ToList();
        }
        // Returns the voice numbers ordered by the average pitch of their notes, higher averages first
        private static IEnumerable<byte> OrderInstrumentVoicesByPitch(IEnumerable<Note> notes)
        {
            var voicesAveragePitches = new Dictionary<byte, double>();
            foreach (var v in GetInstrumentVoicesOfNotes(notes))
            {
                voicesAveragePitches[v] = getAveragePitchOfNOtes(notes.Where(n => n.Voice == v));
            }
            var reorderedDic = voicesAveragePitches.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            return reorderedDic.Keys;
        }
        // Biven a group of notes (possibly from different voices) returns an average of their pitches
        // Used to order voices, with higher pitched voices first
        private static double getAveragePitchOfNOtes(IEnumerable<Note> notes)
        {
            return notes.Average(n => n.Pitch);
        }
        // Given a group of notes that may be played by different voices, returns the different
        // instruments played by the voices. There may be many voices but all play the same instrument
        // in which case it would return a list with only one element
        private static List<byte> GetInstrumentVoicesOfNotes(IEnumerable<Note> notes)
        {
            var instrumentsNotes = notes.Where(n => n.IsPercussion == false);
            return instrumentsNotes.Select(n => n.Voice).Distinct().ToList();
        }
        // Returns the numbers of the voices which consist of percusion notes
        // Voices that have percusion notes, have only percusion notes
        // Percusion notes and melodic notes are never mixed together in the same voice
        private static List<byte> getPercusionVoicesOfNotes(List<Note> notes)
        {
            var instrumentsNotes = notes.Where(n => n.IsPercussion == true);
            return instrumentsNotes.Select(n => n.Voice).Distinct().ToList();
        }

        /// <summary>
        /// A single track may be polyphonic in the sense that 2 or more notes that start and stop 
        /// independently are played simultaneously. When the 2 notes start and stop at the
        /// same time, then we can consider them to be 1 voice.
        /// We split these tracks so each voice of the song simplification is really 1 voice
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> SplitPolyphonicVoiceInMonophonicVoices(List<Note> notes)
        {
            var retObj = new List<Note>();
            // in voicesNotes we have the original notes separated by voice
            var voicesNotes = GetNotesAsDictionary(notes.Clone());
            // we will build this new dictionary that possibly will have more voices, but the same total of notes
            var newVoicesNotes = new Dictionary<byte, List<Note>>();
            byte voice = 0;
            foreach (byte v in voicesNotes.Keys)
            {
                // We keep in this variable the notes of this voice that we still haven't added to newVoicesNotes
                // We initialize it with all the notes of the voice
                var remainingNotes = new List<Note>(voicesNotes[v]);
                List<Note> upperVoice;
                while (remainingNotes.Count > 0)
                {
                    // If there are not many notes left, add them to the last voice, don't create a new voice
                    // notes.Count / voice is the average of notes per voice for this song (we add 1 to avoid division by 0)
                    if (remainingNotes.Count < notes.Count / (6 * (voice + 1)))
                    {
                        var lastVoice = newVoicesNotes[(byte)(voice - 1)];
                        var problematicNotes = GetProblematicNotes(lastVoice);
                        remainingNotes.ForEach(n => lastVoice.Add(n));
                        RemoveOverlappingsInNotes(lastVoice);
                        problematicNotes = GetProblematicNotes(lastVoice);
                        if (problematicNotes.Count > 0)
                        {
                        }
                        break;
                    }
                    upperVoice = GetUpperVoice(remainingNotes);
                    newVoicesNotes[voice++] = upperVoice;
                    upperVoice.ForEach(x => remainingNotes.Remove(x));
                }
            }
            // We have now to reassing each note to the right voice
            foreach(var v in newVoicesNotes.Keys)
            {
                foreach (var n in newVoicesNotes[v])
                {
                    n.Voice = v;
                    retObj.Add(n);
                }
            }
            return retObj.OrderBy(n=>n.StartSinceBeginningOfSongInTicks).ToList();
        }
        /// <summary>
        /// When we split a group of notes in voices so each one of them is monophonic, we have to make a compromise
        /// We don't want in the same voice notes that play together but don't start and end exactly at the same time
        /// but we also don't want to end up with voices with very few notes. So, after splitting in a suitable number
        /// of voices, any remaining notes are added to one of the voices and the durations of the notes are altered
        /// so they are exactly simultaneous with other notes or they don't overlap at all
        /// </summary>
        /// <param name="voiceNotes"></param>
        /// <param name="notesToAdd"></param>
        //private static void AddNotesToVoice(List<Note> voiceNotes, List<Note> notesToAdd)
        //{
        //    RemoveOverlappingsInNotes(notesToAdd);
        //    foreach (var n in notesToAdd)
        //    {
        //        var simulNotes = voiceNotes.Where(m =>
        //           m.TempId != n.TempId &&
        //           GetIntersectionOfNotesInTicks(m, n) > 0 &&
        //           !AreNotesExactlySimultaneous(m, n))
        //            .OrderBy(x => x.StartSinceBeginningOfSongInTicks)
        //           .ToList();
        //        if (simulNotes.Count == 0) continue;
        //        // We first make all notes starting at the same time exactly simultaneous
        //        foreach (var y in simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks))
        //            MakeEndingsSimultaneous(n, y);

        //        if (simulNote != null)
        //        {
        //            // We have a problem: there is a note that is aproximately simultaneous. We have to find the best way to fix it

        //            // First find all notes that are exactly simultaneous with the one we found. If we modify one of them
        //            // we have to modify all of them
        //            var notesToModify = voiceNotes.Where(x => x.StartSinceBeginningOfSongInTicks == simulNote.StartSinceBeginningOfSongInTicks &&
        //               x.EndSinceBeginningOfSongInTicks == simulNote.EndSinceBeginningOfSongInTicks).ToList();

        //            // if they start at the same time, change the end time
        //            if (simulNote.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks)
        //            {
        //                var bestEnd = GetBestPoint(simulNote.EndSinceBeginningOfSongInTicks, n.EndSinceBeginningOfSongInTicks);
        //                n.EndSinceBeginningOfSongInTicks = bestEnd;                   
        //                foreach (var m in notesToModify) m.EndSinceBeginningOfSongInTicks = bestEnd;                            
        //            }
        //            else
        //            {
        //                // if the start time difference is small change start times and end times
        //                var difStart = Math.Abs(n.StartSinceBeginningOfSongInTicks - simulNote.StartSinceBeginningOfSongInTicks);
        //                var averageNoteDuration = (n.DurationInTicks + simulNote.DurationInTicks) / 2;
        //                if (difStart < Math.Min(24, averageNoteDuration / 4))
        //                {
        //                    var bestStart = GetBestPoint(n.StartSinceBeginningOfSongInTicks, simulNote.StartSinceBeginningOfSongInTicks);
        //                    var bestEnd = GetBestPoint(n.EndSinceBeginningOfSongInTicks, simulNote.EndSinceBeginningOfSongInTicks);
        //                    n.StartSinceBeginningOfSongInTicks = bestStart;
        //                    n.EndSinceBeginningOfSongInTicks = bestEnd;
        //                    // There may be other notes that are exactly simultaneous with simulNote, we have to change them all
        //                    foreach (var m in notesToModify)
        //                    {
        //                        m.StartSinceBeginningOfSongInTicks = bestStart;
        //                        m.EndSinceBeginningOfSongInTicks = bestEnd;
        //                    }
        //                }
        //                else
        //                {
        //                    // if the start time difference is large, shorten the first note
        //                    if (simulNote.StartSinceBeginningOfSongInTicks < n.StartSinceBeginningOfSongInTicks)
        //                    {
        //                        foreach (var m in notesToModify) m.EndSinceBeginningOfSongInTicks = n.StartSinceBeginningOfSongInTicks;
        //                    }
        //                    else
        //                        n.EndSinceBeginningOfSongInTicks = simulNote.StartSinceBeginningOfSongInTicks;
        //                }
        //            }
        //        }
        //        voiceNotes.Add(n);
        //    }
        //}
      
        
        /// <summary>
        /// This is a bit drastic, it is used when we have a few notes to assign to a voice and we don't want to create
        /// a new voice to hold them. We remove all overlappings by making the notes exactly simultaneous or shortening
        /// some of them. We don't change start times
        /// </summary>
        /// <param name="notes"></param>
        private static void RemoveOverlappingsInNotes(List<Note> notes)
        {
            foreach (var n in notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks))
            {
                var simulNotes = notes.Where(m =>
                   m.TempId != n.TempId &&
                   GetIntersectionOfNotesInTicks(m, n) > 0 &&
                   !AreNotesExactlySimultaneous(m, n))
                    .OrderBy(x => x.StartSinceBeginningOfSongInTicks)
                   .ToList();
                if (simulNotes.Count == 0) continue;
                // We first make all notes starting at the same time exactly simultaneous
                foreach (var y in simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks))
                    MakeEndingsSimultaneous(n, y);
                // we now look for the first note that is not exactly simultaneous that starts later
                var nextSimul = simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks)
                    .OrderBy(y=>y.StartSinceBeginningOfSongInTicks)
                    .FirstOrDefault();
                // If we find such a note, we make all the notes that started together with n to end when that note starts
                if (nextSimul != null)
                {
                    // if the start time difference is small change start times and end times
                    var difStart = nextSimul.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks;
                    var averageNoteDuration = (n.DurationInTicks + nextSimul.DurationInTicks) / 2;
                    if (difStart < Math.Min(24, averageNoteDuration / 4))
                    {
                        var bestStart = GetBestPoint(n.StartSinceBeginningOfSongInTicks, nextSimul.StartSinceBeginningOfSongInTicks);
                        var bestEnd = GetBestPoint(n.EndSinceBeginningOfSongInTicks, nextSimul.EndSinceBeginningOfSongInTicks);

                        // There may be other notes that are exactly simultaneous with simulNote or with n, we have to change them all
                        foreach (var m in simulNotes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks
                        || x.StartSinceBeginningOfSongInTicks == nextSimul.StartSinceBeginningOfSongInTicks))
                        {
                            m.StartSinceBeginningOfSongInTicks = bestStart;
                            m.EndSinceBeginningOfSongInTicks = bestEnd;
                        }
                    }
                    else
                    {
                        foreach (var m in notes.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks))
                            m.EndSinceBeginningOfSongInTicks = nextSimul.StartSinceBeginningOfSongInTicks;
                    }
                }
            }
        }
        private static void MakeEndingsSimultaneous(Note n, Note m)
        {
            var bestEnd = GetBestPoint(n.EndSinceBeginningOfSongInTicks, m.EndSinceBeginningOfSongInTicks);
            n.EndSinceBeginningOfSongInTicks = bestEnd;
            m.EndSinceBeginningOfSongInTicks = bestEnd;
        }

        /// <summary>
        /// Separates a list of notes in diferent lists, one for each voice (that is the key of the dictionary)
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static Dictionary<byte, List<Note>> GetNotesAsDictionary(List<Note> notes)
        {
            var retObj = new Dictionary<byte, List<Note>>();
            foreach (var n in notes)
            {
                if (!retObj.ContainsKey(n.Voice)) retObj[n.Voice] = new List<Note>();
                retObj[n.Voice].Add(n);
            }
            return retObj;
        }
        // Given a set of notes where we may have polyphony, it returns the upper voice
        // When there are several notes playing at the same time but they start and end together, then all of
        // them are returned. The notes not returned are notes played simultaneously with upper notes,
        // that start and/or stop on different times of the upper notes  by a margin greater than 'tolerance'
        private static List<Note> GetUpperVoice(List<Note> notes)
        {
            var retObj = new List<Note>();
            foreach (var n in notes)
            {
                // Check if we have already added this note
                if (retObj.Where(x => x.TempId == n.TempId).Count() > 0) continue;

                var simulNotes = notes.Where(m =>
                m.TempId != n.TempId &&
                GetIntersectionOfNotesInTicks(m, n) > 0 &&
                !AreNotesExactlySimultaneous(m, n))
                .ToList();
                if (simulNotes.Where(m => m.Pitch > n.Pitch).ToList().Count == 0)
                {
                    retObj.Add(n);

                    // add also notes that start and end both at the same time
                    var chordNotes = notes.Where(m => m.TempId != n.TempId && AreNotesExactlySimultaneous(m, n)).ToList();
                    foreach (var chordNote in chordNotes)
                    {
                        // If it was not already added to retObj, add it
                        if (retObj.Where(x => x.TempId == chordNote.TempId).Count() == 0)
                            retObj.Add(chordNote);
                    }
                }
            }
            // Now correct the timings, so there are no small overlapping of consecutive notes or
            // very short rests between consecutive notes
            retObj = retObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
            for (var i = 0; i < retObj.Count - 1; i++)
            {
                var n1 = retObj[i];
                var n2 = retObj[i + 1];
                if (n2.StartSinceBeginningOfSongInTicks == n1.StartSinceBeginningOfSongInTicks) continue;
                var intersection = GetIntersectionOfNotesInTicks(n1, n2);
                // check overlappings
                if (intersection > 0 && intersection * 8 < (n1.DurationInTicks + n2.DurationInTicks))
                {
                    n1.EndSinceBeginningOfSongInTicks = n2.StartSinceBeginningOfSongInTicks;
                    continue;
                }
                // check short rests between notes
                var separation = n2.StartSinceBeginningOfSongInTicks - n1.EndSinceBeginningOfSongInTicks;
                var shorterNote = n1.DurationInTicks < n2.DurationInTicks ? n1 : n2;
                if (separation > 0 && separation * 3 < shorterNote.DurationInTicks)
                {
                    // There may be notes that are exactly simultaneous with the first one, we must change them all
                    // so they stay exactly simultaneous
                    var notesToChange = retObj.Where(x => 
                        x.StartSinceBeginningOfSongInTicks == n1.StartSinceBeginningOfSongInTicks &&
                        x.EndSinceBeginningOfSongInTicks == n1.EndSinceBeginningOfSongInTicks)
                        .ToList();
                    notesToChange.ForEach(x => x.EndSinceBeginningOfSongInTicks = n2.StartSinceBeginningOfSongInTicks);
                }
            }
            RemoveNotesThatAreTooLowForThisVoice(retObj, notes);

            var sacamela = GetProblematicNotes(retObj);
            if (sacamela.Count > 0)
            {

            }
            return retObj;
        }
            
        // When we extract the upper voice, we select at each tick the highest note of all the ones that are playing
        // at that time. But it could be that the upper voice is silent and the highest note playing belongs to another voice
        // In that case we "return" that note to the pool of notes 
        // We have to do this at the end and not while we are extracting the upper voice, because we need the average pitch
        // of the upper voice and the average pitch of the rest of the notes to decide if a note belongs by its pitch to the
        // upper voice or not
        // The function modifies the parameters sent to it and doesn't return a value. It basically removes some notes from
        // the upper voice and puts them in the pool of notes
        private  static void RemoveNotesThatAreTooLowForThisVoice(List<Note> upperVoice, List<Note> poolOfNotes)
        {
            var restOfNotes = new List<Note>();
            foreach (var n in poolOfNotes)
                if (upperVoice.Where(x => x.TempId == n.TempId).Count() == 0) restOfNotes.Add(n);

            if (restOfNotes.Count == 0) return;

            var notesToRemove = new List<Note>();
            var tolerance = 7;
            foreach (var n in upperVoice)
            {
                var upperVoiceNeighbors = upperVoice
                    .Where(y => y.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks - 300 &&
                    y.StartSinceBeginningOfSongInTicks < n.StartSinceBeginningOfSongInTicks + 300).ToList();
               


                var restOfNotesNeighboors = restOfNotes
                    .Where(y => y.StartSinceBeginningOfSongInTicks > n.StartSinceBeginningOfSongInTicks - 300 &&
                    y.StartSinceBeginningOfSongInTicks < n.StartSinceBeginningOfSongInTicks + 300).ToList();

                if (upperVoiceNeighbors.Count>0 && restOfNotesNeighboors.Count > 0)
                {
                    var upperVoiceAveragePitch = upperVoiceNeighbors.Average(x => x.Pitch);
                    var restOfNotesAveragePitch = restOfNotesNeighboors.Average(x => x.Pitch);
                    var difWithUpperVoiceAverage = Math.Abs(n.Pitch - upperVoiceAveragePitch);
                    var difWithRestOfNotes = Math.Abs(n.Pitch - restOfNotesAveragePitch);
                    if (difWithUpperVoiceAverage - tolerance > difWithRestOfNotes)
                        notesToRemove.Add(n);
                }
            }
            notesToRemove.ForEach(n => upperVoice.Remove(n));
        }

        /// <summary>
        /// When we have to change the end of 2 notes, so they are the same, we want some value between the 2
        /// ends that falls in a "hard tick" For ex. if end1 is 93 and end2 is 97, we select 96
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        private static long GetBestPoint(long point1, long point2)
        {
            int[] subdivisions = { 1, 2, 3, 4, 6, 8, 12, 16, 18, 24, 32, 48 };

            var first = Math.Min(point1, point2);
            var second =Math.Max(point1, point2);
            foreach (int i in subdivisions)
            {
                var distance = 96 / i;
                if (first / distance < second / distance)
                    return second - second % distance;
            }
            return first;
        }
    }
}
