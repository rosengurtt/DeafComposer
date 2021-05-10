using DeafComposer.Analysis.Models;
using DeafComposer.Models.Entities;
using DeafComposer.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Patterns
{

    public static class PatternUtilities
    {
        /// <summary>
        /// Finds the slices of notes separated by n beats that match (that is, they have equal relative notes in some interval of ticks)
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="bars"></param>
        /// <param name="noBeats"></param>
        /// <returns></returns>
        public static List<MelodyMatch> GetMelodyMatchesWithDurationOfUpToNbeats(List<Note> notes, List<Bar> bars, int noBeats)
        {
            var retObj = new List<MelodyMatch>();
            notes = notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ThenByDescending(y => y.Pitch).ToList();
            var lastTick = notes.OrderByDescending(x => x.StartSinceBeginningOfSongInTicks).FirstOrDefault().StartSinceBeginningOfSongInTicks;
            var voices = notes.NonPercussionVoices();
            foreach (var v1 in voices)
            {
                foreach (var v2 in voices)
                {
                    // When v1 and v2 are different, we want to evaluate them once, having v1=0 and v2=1 will find the same patterns as v1=1 and v2=0
                    if (v2 < v1) continue;
                    var totalBeats = GetTotalBeats(bars);
                    var count1 = 1;                
                    while (count1 * noBeats <= totalBeats)
                    {
                        var count2 = count1 + noBeats;
                        while (count2 * noBeats < totalBeats)
                        {
                            var slice1 = GetNsliceOfLengthMbeats(notes, bars, v1, count1, noBeats);
                            var slice2 = GetNsliceOfLengthMbeats(notes, bars, v2, count2, noBeats);
                            var match = GetLargerMatchBetween2Slices(slice1, slice2, bars);
                            if (IsGoodMatch(match, noBeats))
                                retObj.Add(match);
                            count2++;
                        }
                        count1++;
                    }
                }
            }
            return retObj;
        }




        /// <summary>
        /// Gets the nth slice with a duration of m beats
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="bars"></param>
        /// <param name="voice"></param>
        /// <param name="count">Slice 1 is the first slice</param>
        /// <param name="lolo"></param>
        /// <returns></returns>
        private static NotesSlice GetNsliceOfLengthMbeats(List<Note> notes, List<Bar> bars, byte voice, int count, int m)
        {
            // Find the bar number and the beat number inside the bar
            int currentBar;
            int lastBeatOfPreviousBar;
            var firstBarBeats = bars[0].TimeSignature.Numerator;
            if ((count - 1) * m < firstBarBeats)
            {
                currentBar = 1;
                lastBeatOfPreviousBar = 0;
            }
            else
            {
                currentBar = 2;
                lastBeatOfPreviousBar = firstBarBeats;
                while (lastBeatOfPreviousBar <= (count - 1) * m)
                {
                    var previousBar = currentBar - 1;
                    var previousBarBeats = bars[previousBar - 1].TimeSignature.Numerator;
                    if (lastBeatOfPreviousBar + previousBarBeats <= (count - 1) * m)
                    {
                        lastBeatOfPreviousBar += previousBarBeats;
                        currentBar++;
                    }
                    else
                        break;
                }
            }
            var beatLength = 4 * 96 / bars[currentBar - 1].TimeSignature.Denominator;
            var beat = (count - 1) * m + 1 - lastBeatOfPreviousBar;

            // currentBar and beat start in 1, so we have to substract 1
            var startTick = bars[currentBar - 1].TicksFromBeginningOfSong + (beat - 1) * beatLength;
            var endTick = startTick + beatLength * m;

            return new NotesSlice(notes, startTick, endTick, voice, bars[currentBar - 1], beat);
        }

        private static (long, long) GetBarAndBeatNumberOfTick(List<Bar> bars, long tick)
        {
            var bar = bars.Where(b => b.TicksFromBeginningOfSong <= tick).OrderByDescending(y => y.TicksFromBeginningOfSong).FirstOrDefault();
            var beatLength = 4 * 96 / bar.TimeSignature.Denominator;
            return (bar.BarNumber, ((tick - bar.TicksFromBeginningOfSong) / beatLength) + 1);
        }

        private static int GetTotalBeats(List<Bar> bars)
        {
            return bars.Aggregate(0, (sum, next) => sum + next.TimeSignature.Numerator);
        }
        /// <summary>
        /// Decides if the match between 2 slices is good enough to define a pattern
        /// The duration must be greater than half the number of beats
        /// The notes matching must be at least 3 or they could be 2 but only if they follow one another immediately and they are not
        /// just going up or down the scale
        /// The notes must have the same intervals between them, but not be exactly the same
        /// </summary>
        /// <param name="match"></param>
        /// <param name="minTicks">The minimum length the match must have</param>
        /// <returns></returns>
        private static bool IsGoodMatch(MelodyMatch match, int numberOfBeats)
        {
            if (match != null && match.DurationInBeats > numberOfBeats / (double)2 && match.Matches >= 3 &&
                match.Slice1.Notes[0].Pitch != match.Slice2.Notes[0].Pitch) return true;
            if (match != null && match.DurationInBeats > numberOfBeats / (double)2 && match.Matches == 2 &&
                match.Slice1.Notes[0].Pitch != match.Slice2.Notes[0].Pitch &&
                (match.Slice1.StartTick == match.Slice2.EndTick || match.Slice2.StartTick == match.Slice1.EndTick) &&
                Math.Abs(match.Slice1.RelativeNotes[1].DeltaPitch) != 1
                )
                return true;
            return false;
        }

        private static MelodyMatch GetLargerMatchBetween2Slices(NotesSlice slice1, NotesSlice slice2, List<Bar> bars)
        {
            if (slice1.Notes.Count == 0 || slice2.Notes.Count == 0) return null;

            // if the denominator of the time signatures are different we don't try to match
            if (slice1.Bar.TimeSignature.Denominator != slice2.Bar.TimeSignature.Denominator)
                return null;

            // DifferencePoints has the ticks since the beginning of the slice where there are notes that don't match
            var DifferencePoints = new HashSet<long>();
            foreach (var n in slice1.RelativeNotes)
            {
                if (slice2.RelativeNotes.Where(x => x.Tick == n.Tick && x.DeltaPitch == n.DeltaPitch).Count() == 0)
                    DifferencePoints.Add(n.Tick);
            }
            foreach (var n in slice2.RelativeNotes)
            {
                if (slice1.RelativeNotes.Where(x => x.Tick == n.Tick && x.DeltaPitch == n.DeltaPitch).Count() == 0)
                    DifferencePoints.Add(n.Tick);
            }
            // if slices are perfect match return the whole slices as a match
            if (DifferencePoints.Count == 0)
            {
                return new MelodyMatch
                {
                    Slice1 = slice1,
                    Slice2 = slice2,
                    Start = 0,
                    End = slice1.Duration
                };
            }
            // Find interval with the maximum number of matches


            // We have to add the start and end of the slice time, in order to have all the possible time slices to analyze
            DifferencePoints.Add(0);
            DifferencePoints.Add(slice1.Duration + 1);

            // difPoints is DifferencePoints as an ordered list
            var difPoints = DifferencePoints.ToList().OrderBy(x => x).ToList();

            // we use maxConsecutiveMatches to store the highest number of matching notes in the intervals
            var maxConsecutiveMatches = 0;
            long start = 0;
            long end = 0;
            // we iterate now on the intervals defined by the points in difPoints
            for (var i = 0; i < difPoints.Count - 1; i++)
            {
                var matchingNotes = slice1.RelativeNotes
                    .Where(x => x.Tick >= difPoints[i] && x.Tick < difPoints[i + 1]).ToList();
                if (matchingNotes.Count > maxConsecutiveMatches &&
                    // The first relative note of a slice has always pitch 0, so the matching of the first notes of 2 slices, if they are the only match, are not significant
                    !(i == 0 && matchingNotes.Count == 1))
                {
                    maxConsecutiveMatches = matchingNotes.Count();
                    start = slice1.RelativeNotes.Where(x => x.Tick >= difPoints[i]).OrderBy(y => y.Tick).FirstOrDefault().Tick;
                    end = difPoints[i + 1];
                }
            }
            // If no matches at all return null
            if (maxConsecutiveMatches == 0) return null;

            // At this point we have an interval (start, end) (start and end are relative values) where there are maxConsecutiveMatches matches
            var notes1 = slice1
                .Notes.Where(x => x.StartSinceBeginningOfSongInTicks - slice1.StartTick >= start && x.StartSinceBeginningOfSongInTicks - slice1.StartTick < end)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z => z.Pitch)
                .ToList();
            var notes2 = slice2.Notes.Where(x => x.StartSinceBeginningOfSongInTicks - slice2.StartTick >= start && x.StartSinceBeginningOfSongInTicks - slice2.StartTick < end)
                .OrderBy(y => y.StartSinceBeginningOfSongInTicks)
                .ThenByDescending(z => z.Pitch)
                .ToList();
            // The first note of a slice always has relative pitch 0, so it doesn't count for a match
            var countOfMatchingNotes = IsFirstNoteOfSlice(notes1[0], slice1) ? notes1.Count - 1 : notes1.Count;
            var (bar1, beat1) = GetBarAndBeatNumberOfTick(bars, slice1.StartTick + start);
            var (bar2, beat2) = GetBarAndBeatNumberOfTick(bars, slice2.StartTick + start);

            // We have to make this correction or otherwise the slices will extend 1 tick after the end
            if (end == slice1.Duration + 1) end--;

            return new MelodyMatch
            {
                Slice1 = new NotesSlice(notes1, slice1.StartTick + start, slice1.StartTick + end, slice1.Voice, bars[(int)(bar1 - 1)], beat1),
                Slice2 = new NotesSlice(notes2, slice2.StartTick + start, slice2.StartTick + end, slice2.Voice, bars[(int)(bar2 - 1)], beat2),
                Start = start,
                End = end > slice1.Duration ? slice1.Duration : end
            };
        }
        private static bool IsFirstNoteOfSlice(Note n, NotesSlice s)
        {
            var firstNote = s.Notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks).FirstOrDefault();
            return n.StartSinceBeginningOfSongInTicks == firstNote.StartSinceBeginningOfSongInTicks;
        }
    }
}
