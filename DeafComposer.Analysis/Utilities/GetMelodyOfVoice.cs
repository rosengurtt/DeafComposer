using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        /// <summary>
        /// We remove the poliphony of a voice, to return a melody (notes that are playing sequentially)
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static IEnumerable<Note> GetMelodyOfVoice(List<Note> notes)
        {
            // The following var defines how much should be 2 notes sounding at the same time to remove the lower note
            var fractionOfSuperpositionThreshold = 0.7;
            var orderedNotes = notes.OrderBy(x => x.StartSinceBeginningOfSongInTicks)
                .ThenBy(y => y.Pitch).ToList();
            for (int i = 0; i < orderedNotes.Count() - 1; i++)
            {
                int j = 0;
                while (i + j < orderedNotes.Count() &&
                    orderedNotes[i + j].StartSinceBeginningOfSongInTicks < orderedNotes[i].EndSinceBeginningOfSongInTicks &&
                    IntersectionComparedToDuration(orderedNotes[i + j], orderedNotes[i]) > fractionOfSuperpositionThreshold)
                    j += 1;
                if (i + j < orderedNotes.Count())
                {
                    yield return orderedNotes[i + j - 1];
                }
                if (j > 1)
                    i += (j - 1);
            }
        }

        /// <summary>
        /// When we have 2 notes that sound at the same time, we want to keep the higher one and remove the other
        /// But it may be that 2 notes sound together for a short time due to imprecisions in the playing
        /// but they are distinct notes supposed to be played one after the other
        /// We discriminate between the 2 cases comparing the time they sound together with the average duration
        /// of the notes
        /// This function calculates that ratio
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        private static double IntersectionComparedToDuration(Note n1, Note n2)
        {
            var intersection = Math.Min(n1.EndSinceBeginningOfSongInTicks, n2.EndSinceBeginningOfSongInTicks) -
                Math.Max(n1.StartSinceBeginningOfSongInTicks, n2.StartSinceBeginningOfSongInTicks);
            var averageDuration = (n1.DurationInTicks + n2.DurationInTicks) / 2;
            return intersection / (double)averageDuration;
        }

    }
}
