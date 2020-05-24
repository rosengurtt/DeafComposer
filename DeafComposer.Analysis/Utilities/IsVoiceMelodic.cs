using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {

        /// <summary>
        /// Checks if the notes in the voice mainly do chords or not
        /// If most of the notes form part of chords, it means the
        /// voice is doing accompaniment
        /// 
        /// It calculates the total time (in ticks) where there is at least
        /// one not playing (totalNonSilentTime)
        /// 
        /// It then calculates the total of adding all durations of all notes
        /// (totalPlayingTime)
        /// 
        /// The division of totalPlayingTime/totarlNonSilentTime gives an
        /// estimation of the polyphony. If it is 3 or more, we consider
        /// this voice is playing chords
        /// It then divides 
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static bool IsVoiceMelodic(List<Note> notes)
        {
            if (notes.Select(n => n.IsPercussion == true).Any()) return false;
            long totalNonSilentTime = 0;
            long totalPlayingTime = 0;
            var sortedNotes = notes.OrderBy(n => n.StartSinceBeginningOfSongInTicks);
            long endPreviousNote = 0;
            foreach (var n in sortedNotes)
            {
                totalPlayingTime += n.DurationInTicks;
                if (n.EndSinceBeginningOfSongInTicks < endPreviousNote) continue;
                totalNonSilentTime += (n.EndSinceBeginningOfSongInTicks -
                    Math.Max(n.StartSinceBeginningOfSongInTicks, endPreviousNote));
                endPreviousNote = n.EndSinceBeginningOfSongInTicks;
            }
            if ((totalPlayingTime / (double)totalNonSilentTime) >= 3) return false;
            return true;
        }
    }
}
