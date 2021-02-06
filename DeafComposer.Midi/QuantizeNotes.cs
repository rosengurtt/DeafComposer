using DeafComposer.Models.Entities;
using DeafComposer.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Tries to "correct" the start and end of notes so they fit as much as possible with
        /// the tick times that correspond exactly with the start of a beat or a subdivision of it
        /// 
        /// We have to be carefully with embelishments and quick sequences, because if we quantize
        /// those notes we may affect badly the song
        /// 
        /// We recognize those special cases looking at the neighboring notes. If there are short notes
        /// in the same voice played close to a note, and they don't start at the same time as the note
        /// under investigation, we don't make changes
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<Note> QuantizeNotes(List<Note> notes)
        {
            var retObj = notes.Clone();
            foreach (var n in retObj)
            {

                var closeShortNotes = retObj
                    .Where(x => Math.Abs(x.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) < 24 &&
                    n.DurationInTicks < 24 &&
                    x.Voice == n.Voice);

                // Quantize starts
                if (closeShortNotes.Where(x => x.StartSinceBeginningOfSongInTicks != n.StartSinceBeginningOfSongInTicks).Count() == 0)
                {
                    var maxStartChange = Math.Min(4, n.DurationInTicks / 10);
                    n.StartSinceBeginningOfSongInTicks = GetTickOfHighestImportanceInSegment(n.StartSinceBeginningOfSongInTicks - maxStartChange,
                        n.StartSinceBeginningOfSongInTicks + maxStartChange);
                }

                // Quantize endings
                var maxEndChange = Math.Min(12, n.DurationInTicks / 8);
                n.EndSinceBeginningOfSongInTicks = GetTickOfHighestImportanceInSegment(n.EndSinceBeginningOfSongInTicks - maxEndChange,
                    n.EndSinceBeginningOfSongInTicks + maxEndChange);
            }
            return retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
        }

        /// <summary>
        /// When we have a chord, all notes are supposed to start together and end together, but in practice
        /// they may start or end with minor differences. Eliminating these differences is essential for showing
        /// the chord in music notation properly
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> FixLengthsOfChordNotes(List<Note> notes)
        {
            // We work on a copy so we don't modify the supplied notes
            var retObj = notes.Clone();
            var voices = retObj.NonPercussionVoices();

            foreach (var v in voices)
            {
                var voiceNotes = retObj.Where(n => n.Voice == v)
                    .OrderBy(x => x.StartSinceBeginningOfSongInTicks)
                    .ThenBy(y => y.DurationInTicks)
                    .ToList();
                // We use this variable to avoid processing again notes we have already processed
                var notesProcessed = new List<Note>();

                foreach (var n in voiceNotes)
                {
                    if (notesProcessed.Contains(n)) continue;

                    var toleranceStart = Math.Min(3, n.DurationInTicks / 12);

                    // we get first all the notes not processed yet that start at aprox the same time as n
                    var candidateChordNotes = voiceNotes
                        .Where(x => StartDifference(x, n) <= toleranceStart && !notesProcessed.Contains(x))
                        .ToList();

                    // if candidateChordNotes contains only note n, the note is not played as part of a chord
                    if (candidateChordNotes.Count == 1)
                    {
                        notesProcessed.Add(n);
                        continue;
                    }

                    // we filter out the ones whose duration differs from the average duration for more than 60%
                    var averageDuration = (int)Math.Round(candidateChordNotes.Average(x => x.DurationInTicks));
                    var chordNotes = candidateChordNotes.Where(x => Difference(x.DurationInTicks, averageDuration) < averageDuration * 0.6).ToList();

                    // if we found a chord that contains n, fix the starts and endings
                    if (chordNotes.Count > 2 && chordNotes.Contains(n))
                    {
                        var (bestStart, bestEnd) = GetBestStarAndEndForChordNotes(chordNotes);

                        chordNotes.ForEach(m =>
                        {
                            // There are strange cases of notes that are just 1 or 2 ticks long that can end up with duration 0 if we
                            // are not careful, so we check that we are not going to make the duration null before making the change
                            if (bestStart < bestEnd)
                            {
                                m.StartSinceBeginningOfSongInTicks = bestStart;
                                m.EndSinceBeginningOfSongInTicks = bestEnd;
                                // we add the notes to notesProcessed, so we don't change these notes again
                                notesProcessed.Add(m);
                            }
                        });
                    }
                }
            }
            return retObj.OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
        }

        /// <summary>
        /// Calculates the difference in duration between 2 notes
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        private static int Difference(int number1, int number2)
        {
            return Math.Abs(number1 - number2);
        }

        /// <summary>
        /// Calculates the difference in ticks between the starts of 2 notes
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        private static long StartDifference(Note n1, Note n2)
        {
            return Math.Abs(n1.StartSinceBeginningOfSongInTicks - n2.StartSinceBeginningOfSongInTicks);
        }
        private static long EndDifference(Note n1, Note n2)
        {
            return Math.Abs(n1.EndSinceBeginningOfSongInTicks - n2.EndSinceBeginningOfSongInTicks);
        }
        /// <summary>
        /// Given a group of notes that are aprox simultaneous but not exactly, it finds a start tick that falls 
        /// between the start ticks of the notes and that falls on a beat or subdivision of the beat. 
        /// The same for the end
        /// </summary>
        /// <param name="chordNotes"></param>
        /// <returns></returns>
        private static (long, long) GetBestStarAndEndForChordNotes(List<Note> chordNotes)
        {
            long shortestStart = chordNotes.OrderBy(n => n.StartSinceBeginningOfSongInTicks).First().StartSinceBeginningOfSongInTicks;
            long longestStart = chordNotes.OrderByDescending(n => n.StartSinceBeginningOfSongInTicks).First().StartSinceBeginningOfSongInTicks;
            long shortestEnd = chordNotes.OrderBy(n => n.EndSinceBeginningOfSongInTicks).First().EndSinceBeginningOfSongInTicks;
            long longestEnd = chordNotes.OrderByDescending(n => n.EndSinceBeginningOfSongInTicks).First().EndSinceBeginningOfSongInTicks;
            return (GetTickOfHighestImportanceInSegment(shortestStart, shortestEnd), GetTickOfHighestImportanceInSegment(shortestEnd, longestEnd));
        }
        /// <summary>
        /// We often want to find the point inside a segment where the beat starts, and if there isn't a beat starting
        /// we want the half of a beat, and if there isn't one the half of a half of a beat, etc.
        /// So for example if we are going to quantize a chord, so all the notes of the chord start and end together, rather
        /// than quantizing to the average start time of the notes, it makes more sense to start in the tick when a beat starts
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static long GetTickOfHighestImportanceInSegment(long start, long end)
        {
            var divisionsOfBeat = new int[] { 1, 2, 3, 4, 6, 8, 12, 16, 18, 24, 32, 64 };
            foreach (var d in divisionsOfBeat)
            {
                var segSize = 96 / d;
                if (start % segSize == 0) return start;
                if (end % segSize == 0) return end;
                if (end - (end % segSize) > start) return end - (end % segSize);
            }
            // we don't expect to leave from here, but just in case
            return (start + end) / 2;
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
        //private static long GetMostAppropriateTime(long e1, long e2)
        //{
        //    var divisor = 96;
        //    while (divisor > 1)
        //    {
        //        for (var i = Math.Min(e1, e2); i <= Math.Max(e1, e2); i++)
        //        {
        //            if (i % divisor == 0) return i;
        //            if (i % (divisor / 3) == 0) return i;
        //        }
        //        divisor = divisor / 2;
        //    }
        //    return e1;
        //}
        /// <summary>
        /// When a song is finishing it is common that the musician slows gradually the pace and plays the final
        /// note or chord for a length of time that exceeds the bar length. We limit the length of the final
        /// notes to the length of the bar, so the routines that create the musical notation of the song will have
        /// an easier job
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="bars"></param>
        /// <returns></returns>
        private static List<Note> FixDurationOfLastNotes(List<Note> notes, List<Bar> bars)
        {
            var ticksPerQuarter = 96;
            var lastBar = bars[bars.Count - 1];
            var startOfLastBar = lastBar.TicksFromBeginningOfSong;
            var endOfLastBar = startOfLastBar + lastBar.TimeSignature.Numerator * ticksPerQuarter * 4 / lastBar.TimeSignature.Denominator;
            var retObj = notes.Clone();
            if (!retObj.Where(x => x.StartSinceBeginningOfSongInTicks >= startOfLastBar).Any())
                endOfLastBar = startOfLastBar;
            retObj.ForEach(x =>
            {
                if (x.EndSinceBeginningOfSongInTicks > endOfLastBar)
                    x.EndSinceBeginningOfSongInTicks = endOfLastBar;
            });
            return retObj;
        }

        /// <summary>
        /// When we have 2 notes starts or ends happening almost at the same time but not exactly
        /// in most cases they are meant to happen at the same time. If one of the notes is very short, then the fact
        /// that they start at short different times may be on purpose like when we have an embelishment
        /// We try to find the notes that are meant to start together but are not, and make them start exactly at the
        /// same time. We select the time so it matches a suitable subdivision of the beat
        /// 
        /// When we have consecutive notes in a voice, where each one ends approximately when the next starts, but
        /// one of the notes is larger or shorter we can assume it is a mistake. If it is much, much larger, like
        /// a half, when the other notes are sixteens we assume it is on purpose. But if is an eight or a quarter and
        /// the others are sixteens, we assume is a mistake and we fix it
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>      
        private static List<Note> CorrectNotesTimings(List<Note> notes)
        {
            // We first copy all the notes to retObj, we will then remove and alter notes in rettObj, but the original notes are left unchanged
            var retObj = notes.Clone().OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
            // In this loop we do the first type of correction when 2 notes start and stop aprox at the same time
            for (var i = 0; i < retObj.Count - 1; i++)
            {
                for (var j = i + 1; j < retObj.Count; j++)
                {
                    var ni = retObj[i];
                    var nj = retObj[j];
                    var toleranceStart = GetToleranceForComparingNotes(ni, nj);
                    var toleranceEnd = GetToleranceForComparingNotes(ni, nj, false);
                    var dif = Math.Abs(ni.StartSinceBeginningOfSongInTicks - nj.StartSinceBeginningOfSongInTicks);
                    // If any of the notes is very short, then don't change the timings
                    if (ni.DurationInTicks < dif * 4 || nj.DurationInTicks < dif * 4) continue;
                    if (EventsAreAlmostSimultaneousButNotExactly(ni.StartSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks, toleranceStart))
                    {
                        var bestTime = GetTickOfHighestImportanceInSegment(ni.StartSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks);
                        ni.StartSinceBeginningOfSongInTicks = bestTime;
                        nj.StartSinceBeginningOfSongInTicks = bestTime;
                    }
                    if (EventsAreAlmostSimultaneousButNotExactly(ni.EndSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks, toleranceEnd))
                    {
                        var bestTime = GetTickOfHighestImportanceInSegment(ni.EndSinceBeginningOfSongInTicks, nj.StartSinceBeginningOfSongInTicks);
                        ni.EndSinceBeginningOfSongInTicks = bestTime;
                        nj.StartSinceBeginningOfSongInTicks = bestTime;
                    }
                }
            }

            // In this loop we make the second type of correction, when a note has a wrong duration
            var voices = retObj.NonPercussionVoices();
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
                    alreadyEvaluated.Add(nextGroupOf4ConsecutiveNotes[0]);
                }
            }
            return retObj;
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
        private static int GetToleranceForComparingNotes(Note n, Note m, bool isNoteStart = true)
        {
            var shortestNote = n.DurationInTicks < m.DurationInTicks ? n : m;
            if (isNoteStart)
                return Math.Min(shortestNote.DurationInTicks / 6, 3);
            else
                return shortestNote.DurationInTicks / 3;
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

        // Returns true if the 2 voices start and finish at the same time (optionally with some tolerance)
        private static bool DoNotesStartAndEndTogether(Note m, Note n, int tolerance = 0)
        {
            return Math.Abs(m.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) <= tolerance &&
                            Math.Abs(m.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) <= tolerance; ;
        }
    }
}
