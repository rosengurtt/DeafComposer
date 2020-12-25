using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static List<Note> QuantizeNotes(List<Note> notes)
        {
            var retObj = new List<Note>();
            foreach (var n in notes) retObj.Add(QuantizeNote((Note)n.Clone()));
            return retObj.OrderBy(x=>x.StartSinceBeginningOfSongInTicks).ToList();
        }

        private static Note QuantizeNote(Note n)
        {
            int standardTicksPerQuarterNote = 96;

            // Quantize starts
            if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == 1)
                n.StartSinceBeginningOfSongInTicks -= 1;
            if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == standardTicksPerQuarterNote/2-1)
                n.StartSinceBeginningOfSongInTicks += 1;
            if (n.DurationInTicks > 6)
            {
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == 1)
                    n.StartSinceBeginningOfSongInTicks -= 1;
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == standardTicksPerQuarterNote / 3 - 1)
                    n.StartSinceBeginningOfSongInTicks += 1;
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == 2)
                    n.StartSinceBeginningOfSongInTicks -= 2;
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == standardTicksPerQuarterNote/2 - 2)
                    n.StartSinceBeginningOfSongInTicks += 2;
            }

            // Quantize endings
            if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == 1)
                n.EndSinceBeginningOfSongInTicks -= 1;
            if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == standardTicksPerQuarterNote / 2 - 1)
                n.EndSinceBeginningOfSongInTicks += 1;
            if (n.DurationInTicks > 6)
            {
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == 1)
                    n.EndSinceBeginningOfSongInTicks -= 1;
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == standardTicksPerQuarterNote / 3 - 1)
                    n.EndSinceBeginningOfSongInTicks += 1;
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == 2)
                    n.EndSinceBeginningOfSongInTicks -= 2;
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == standardTicksPerQuarterNote / 2 - 2)
                    n.EndSinceBeginningOfSongInTicks += 2;
            }
            var tolerance = standardTicksPerQuarterNote / 6;

            int[] numbers = { 6, 5, 4, 3, 2, 1 };
            foreach (var m in numbers)
            {
                if (n.DurationInTicks > standardTicksPerQuarterNote * m - tolerance && n.DurationInTicks < standardTicksPerQuarterNote * m + tolerance)
                {
                    n.EndSinceBeginningOfSongInTicks = n.StartSinceBeginningOfSongInTicks + standardTicksPerQuarterNote * m;
                    return n;
                }
            }
            if (n.DurationInTicks > (standardTicksPerQuarterNote - tolerance)/(double)2 &&
                n.DurationInTicks < (standardTicksPerQuarterNote + tolerance) / (double)2)
                n.EndSinceBeginningOfSongInTicks = n.StartSinceBeginningOfSongInTicks + standardTicksPerQuarterNote/2;
            return n;
        }
      
    }
}
