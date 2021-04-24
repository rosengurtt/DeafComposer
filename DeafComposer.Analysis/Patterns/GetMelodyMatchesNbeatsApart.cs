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
        /// <param name="n"></param>
        /// <returns></returns>
        public static List<MelodyMatch> GetMelodyMatchesNbeatsApart(List<Note> notes, List<Bar> bars, int n)
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
                    if (v2 < v1) break;
                    var count = 1;
                    var totalBeats = GetTotalBeats(bars);
                    while ((count + 1) * n <= totalBeats)
                    {
                        var slice1 = GetNsliceOfLengthMbeats(notes, bars, v1, count, n);
                        var slice2 = GetNsliceOfLengthMbeats(notes, bars, v2, count + 1, n);
                        var match = GetLargerMatchBetween2Slices(slice1, slice2, bars);
                        if (match != null && IsGoodMatch(match, n * 48, 1 + n))
                            retObj.Add(match);
                        count++;
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
            int currentBar = 0;
            int lastBeatOfPreviousBar = 0;
            while (lastBeatOfPreviousBar < count * m)
            {
                currentBar++;
                lastBeatOfPreviousBar += bars[currentBar - 1].TimeSignature.Numerator;
            }
            lastBeatOfPreviousBar = lastBeatOfPreviousBar - bars[currentBar - 1].TimeSignature.Numerator;
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
        /// </summary>
        /// <param name="match"></param>
        /// <param name="minTicks">The minimum length the match must have</param>
        /// <returns></returns>
        private static bool IsGoodMatch(MelodyMatch match, int minTicks = 48, int minMatchingNotes = 2)
        {
            if (match.Duration < minTicks) return false;
            if (match.Matches < minMatchingNotes) return false;
            return true;
        }

        private static MelodyMatch GetLargerMatchBetween2Slices(NotesSlice slice1, NotesSlice slice2, List<Bar> bars)
        {
            if (slice1.Notes.Count == 0 || slice2.Notes.Count == 0) return null;

            // DifferencePoints has the ticks since the beginning of the slice where there are not matching notes
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
                    Duration = slice1.Duration
                };
            }
            // Find interval with the maximum number of matches


            // We have to add the start and end of the slice time, in order to have all the possible time slices to analyze
            DifferencePoints.Add(0);
            DifferencePoints.Add(slice1.Duration + 1);

            // difPoints is DifferencePoints as an ordered list
            var difPoints = DifferencePoints.ToList().OrderBy(x => x).ToList();

            var maxConsecutiveMatches = 0;
            long start = 0;
            long end = 0;
            for (var i = 0; i < difPoints.Count - 1; i++)
            {
                var matchingNotes = slice1.RelativeNotes
                    .Where(x => x.Tick >= difPoints[i] && x.Tick < difPoints[i + 1]).ToList();
                if (matchingNotes.Count > maxConsecutiveMatches &&
                    // The first relative note of a slice has always pitch 0, so the matching of the first notes of 2 slices, if they are the only match, are not significative
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
            return new MelodyMatch
            {
                Slice1 = new NotesSlice(notes1, slice1.StartTick + start, slice1.StartTick + end, slice1.Voice, bars[(int)(bar1 - 1)], beat1),
                Slice2 = new NotesSlice(notes2, slice2.StartTick + start, slice2.StartTick + end, slice2.Voice, bars[(int)(bar2 - 1)], beat2),
                Duration = end > slice1.Duration ? slice1.Duration - start : end - start
            };
        }
        private static bool IsFirstNoteOfSlice(Note n, NotesSlice s)
        {
            var firstNote = s.Notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks).FirstOrDefault();
            return n.StartSinceBeginningOfSongInTicks == firstNote.StartSinceBeginningOfSongInTicks;
        }
    }
}
