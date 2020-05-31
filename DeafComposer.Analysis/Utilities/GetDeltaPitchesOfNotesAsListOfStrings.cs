using DeafComposer.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        /// <summary>
        /// Given a group of notes, it extracts the pitches, converts them to deltas (each value is the
        /// difference of 2 consecutive pitches), and concatenates them in a comma separated string
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<string> GetDeltaPitchesOfNotesAsListOfStrings(List<Note> notes)
        {

            var pitches = notes.OrderBy(m => m.StartSinceBeginningOfSongInTicks).Select(n => n.Pitch).ToList();
            var deltaPitches = new List<int>();
            // The first delta pitch is 0
            deltaPitches.Add(0);
            // For the rest is the difference between consecutive pithces
            for (int i = 1; i < pitches.Count(); i++)
                deltaPitches.Add(pitches[i] - pitches[i - 1]);
            return deltaPitches.Select(x=>x.ToString()).ToList();
        }
    }
}
