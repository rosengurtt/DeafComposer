using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static IEnumerable<Note> QuantizeNotes(SongSimplification songSimplification, List<Bar> bars)
        {
            int standardTicksPerQuarterNote = 96;

            foreach (var n in songSimplification.Notes)
            {
                int i = 0;
                while (i < bars.Count &&
                    bars[i].TicksFromBeginningOfSong <= n.EndSinceBeginningOfSongInTicks)
                    i++;
                yield return QuantizeNote(n.Clone(), standardTicksPerQuarterNote, bars[i - 1].HasTriplets);

            }
        }
        private static Note QuantizeNote(Note n, int ticksPerQuarterNote, bool hasTriplets)
        {
            if ((n.StartSinceBeginningOfSongInTicks % ticksPerQuarterNote == 0) &&
                (n.EndSinceBeginningOfSongInTicks % ticksPerQuarterNote == 0))
                return n;
            var retObj = n.Clone();
            retObj.StartSinceBeginningOfSongInTicks = QuantizePointInTime(n.StartSinceBeginningOfSongInTicks,
                n.DurationInTicks, ticksPerQuarterNote, hasTriplets, true);
            retObj.EndSinceBeginningOfSongInTicks = QuantizePointInTime(n.EndSinceBeginningOfSongInTicks,
             n.DurationInTicks, ticksPerQuarterNote, hasTriplets, false);
            // We don't want to quantize in such a way that the note ends before a pitch bending on the note
            // If we find a pitch bending that would be lost, we quantize making the note longer rather than shorter
            var i = 1;
            while (n.PitchBending.Where(x => x.TicksSinceBeginningOfSong > retObj.EndSinceBeginningOfSongInTicks).Any())
            {
                retObj.EndSinceBeginningOfSongInTicks = QuantizePointInTime(n.EndSinceBeginningOfSongInTicks + i,
                          n.DurationInTicks, ticksPerQuarterNote, hasTriplets, false);
                i++;
            }
            return retObj;
        }
        private static long QuantizePointInTime(long point, int durationInTicks,
        int ticksPerQuarterNote, bool hasTriplets, bool pointIsStart)
        {
            // quantum represents the number of ticks that separate 2
            // consecutive quantized points  
            int quantum = (int)(hasTriplets ? Math.Round((double)durationInTicks / 6) :
                Math.Floor((double)durationInTicks / 4));

            if (quantum >= ticksPerQuarterNote)
                quantum = ticksPerQuarterNote;
            if (quantum == 0)
                quantum = 1;
            // that was a first aproximation. We want a quantum that divides the
            // tickPerQuarterNote value. So we find the first larger and the firstclosest
            // smaller values that divide ticksPerNote and we select the one 
            // to our first aproximation
            int maxQuantum = quantum;
            while (ticksPerQuarterNote % maxQuantum != 0)
                maxQuantum++;
            int minQuantum = quantum;
            while ((ticksPerQuarterNote % minQuantum != 0) && minQuantum > 1)
                minQuantum--;
            if (maxQuantum - quantum < quantum - minQuantum)
                quantum = maxQuantum;
            else
                quantum = minQuantum;

            // Now that we have the quantum, we find which of the left or right
            // points located on quantized values is closer 
            var shift = point % quantum;
            var leftQuantizedPoint = point - shift;
            var newDurationUsingLeft = pointIsStart ? durationInTicks + shift : durationInTicks - shift;
            var rightQuantizedPoint = leftQuantizedPoint + quantum;
            var newDurationUsingRight = pointIsStart ? durationInTicks - quantum + shift : durationInTicks + quantum - shift;

            if (point - leftQuantizedPoint < rightQuantizedPoint - point)
                return leftQuantizedPoint;
            else if (point - leftQuantizedPoint > rightQuantizedPoint - point)
                return rightQuantizedPoint;
            if (NumberOfWholeDivisors(newDurationUsingLeft) < NumberOfWholeDivisors(newDurationUsingRight))
                return rightQuantizedPoint;
            else return leftQuantizedPoint;
        }

        private static int NumberOfWholeDivisors(long number)
        {
            if (number == 0) return 0;
            int numberOfDivisors = 0;
            int i = 2;
            while (i < Math.Sqrt(number))
            {
                if (number % i == 0) numberOfDivisors++;
                i++;
            }
            return numberOfDivisors;
        }
    }
}
