using DeafComposer.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Makes minor adjustments to notes that don't start in the beat for a very small amount
        /// It has some intelligence in the sense that a thirtysecond, that is a very short note, may make sense that starts
        /// in an odd place, because in a passage of the song where the player is playing a lot of notes in a short time, we
        /// can expect to have notes with unusual start locations. Also when we have an embelishment, we can expect notes a
        /// short time before or after the beat. But if a note has a duration of a quarter, we don't expect it to be played 
        /// 3 ticks after the beat
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
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
