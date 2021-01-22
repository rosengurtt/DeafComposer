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

        /// <summary>
        /// Makes little obvious corrections. 
        /// If a note starts 1 tick after the beat, it should really start in the beat
        /// For the ear the difference is negligible, but when processing the data for ex. to show the song in music notation
        /// this little difference is a nuisance
        /// 
        /// When the note is long in terms of tick (for ex. a quarter) we change only the start. When it is very short (a few
        /// ticks) we change the end as well, otherwise the duration would be affected
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private static Note QuantizeNote(Note n)
        {
            int standardTicksPerQuarterNote = 96;

            int displacement = 0;

            // Quantize starts
            if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == 1)
                displacement= -1;
            if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == standardTicksPerQuarterNote/2-1)
                displacement = 1;
            if (n.DurationInTicks > 6)
            {
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == 1)
                    displacement = -1;
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == standardTicksPerQuarterNote / 3 - 1)
                    displacement = 1;
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == 2)
                    displacement = -2;
                if (n.StartSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote/2) == standardTicksPerQuarterNote/2 - 2)
                    displacement = 2;
            }

            // Quantize endings
            if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == 1)
                displacement = -1;
            if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == standardTicksPerQuarterNote / 2 - 1)
                displacement = 1;
            if (n.DurationInTicks > 6)
            {
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == 1)
                    displacement = -1;
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 3) == standardTicksPerQuarterNote / 3 - 1)
                    displacement = 1;
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == 2)
                    displacement = -2;
                if (n.EndSinceBeginningOfSongInTicks % (standardTicksPerQuarterNote / 2) == standardTicksPerQuarterNote / 2 - 2)
                    displacement = 2;
            }
            // if the note is short we change the end time as well, so we don't change the duration of the note
            // otherwise we change only the beginning and the duration is affected but the change is negligible
            if (n.DurationInTicks>6)
                n.StartSinceBeginningOfSongInTicks += displacement;
            else
            {
                n.StartSinceBeginningOfSongInTicks += displacement;
                n.EndSinceBeginningOfSongInTicks += displacement;
            }
            
            var tolerance = standardTicksPerQuarterNote / 6;

            // We check now long notes (a quarter or longer) and we make corrections up to a 6th of a quarter note
            // Basically we don't expect a quarter or half note to start less than a sixteenth after the beat or before the beat
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
