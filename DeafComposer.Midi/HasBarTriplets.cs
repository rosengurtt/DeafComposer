using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {

        /// <summary>
        /// We want to know if there are durations that are multiple of 3
        /// in the bar. If the bar has triplets, then when we quantize the
        /// notes in the bar we must aproximate points to values that
        /// are multiple of 3 and not only powers of 2
        /// </summary>
        /// <param name="song"></param>
        /// <param name="bar"></param>
        /// <returns></returns>
        public static bool HasBarTriplets(SongSimplification songSimplification, Bar bar)
        {
            int standardTicksPerQuarterNote = 96;
            var notes = GetNotesOfBar(bar, songSimplification);
            var lengthsOfTriplets = GetPossibleLengthsOfTriplets(standardTicksPerQuarterNote);
            int numberOfTriplets = 0;
            foreach (var n in notes)
            {
                foreach (var q in lengthsOfTriplets)
                {
                    if (IsDurationEssentiallyTheSame(n, q))
                    {
                        numberOfTriplets++;
                        break;
                    }
                }
            }
            return (numberOfTriplets * 2 > notes.Count);
        }
        private static IEnumerable<int> GetPossibleLengthsOfTriplets(int? ticksPerBeat)
        {
            var min = (int)ticksPerBeat / 3;
            for (int i = 0; i < 16; i++)
                yield return min * i;
        }

        /// <summary>
        /// If the duration of a note in ticks is greater or smaller than
        /// an int by less than 10% we consider that the duration of the
        /// note can be the int, so it can be quantized to it
        /// </summary>
        /// <param name="n"></param>
        /// <param name="dur"></param>
        /// <param name="ticksPerBeat"></param>
        /// <returns></returns>
        private static bool IsDurationEssentiallyTheSame(Note n, int dur)
        {
            var dif = Math.Abs(n.DurationInTicks - dur);
            var avg = (n.DurationInTicks + dur) / 2;
            if (avg == 0) return true;
            return (dif * 100 / avg < 10);
        }
    }
}
