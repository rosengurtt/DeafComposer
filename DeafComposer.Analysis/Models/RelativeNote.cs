using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Analysis.Models
{
    /// <summary>
    /// Represents a note in a slice. Instedad of having absolutes vales like a real pitch and the ticks from the start of the song, it has values related to
    /// the start of the slice and the pitch of first note of the slice
    /// </summary>
    public class RelativeNote
    {
        public RelativeNote(Note n, Note previousNote, long startTick, KeySignature key)
        {
            if (previousNote == null)
            {
                DeltaPitchInSemitones = 0;
                DeltaPitch = 0;
                DeltaTick = 0;
            }
            else
            {
                DeltaPitchInSemitones = n.Pitch - previousNote.Pitch;
                DeltaPitch = GetNotePositioninKey(n, key) - GetNotePositioninKey(previousNote, key);
                DeltaTick = n.StartSinceBeginningOfSongInTicks - previousNote.StartSinceBeginningOfSongInTicks;
            }
            Tick = n.StartSinceBeginningOfSongInTicks - startTick;
        }
        public long Tick { get; set; }
        public long DeltaTick { get; set; }

        /// <summary>
        /// Represents the difference between the previous pitch and this one
        /// </summary>
        public int DeltaPitchInSemitones { get; set; }

        /// <summary>
        /// This is the difference in terms of a major scale, so C and D in C major have a DeltaPitch of 1 while the DeltaPitchInSemitones is 2
        /// </summary>
        public int DeltaPitch { get; set; }

        /// <summary>
        /// The pitch in relation to the first pitch of a slice
        /// </summary>
        public int Pitch { get; set; }

        /// <summary>
        /// If we are in C major scale and note is E, it returns 2, so a third has a value of 2, a fourth a value of 3, etc.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static int GetNotePositioninKey(Note n, KeySignature key)
        {
            var majorScalePitches = new List<int>();
            majorScalePitches.Add((60 + 7 * key.key) % 12);
            majorScalePitches.Add((62 + 7 * key.key) % 12);
            majorScalePitches.Add((64 + 7 * key.key) % 12);
            majorScalePitches.Add((65 + 7 * key.key) % 12);
            majorScalePitches.Add((67 + 7 * key.key) % 12);
            majorScalePitches.Add((69 + 7 * key.key) % 12);
            majorScalePitches.Add((71 + 7 * key.key) % 12);

            return 7 * (n.Pitch / 12) + majorScalePitches.IndexOf(n.Pitch % 12);
        }
    }

}
