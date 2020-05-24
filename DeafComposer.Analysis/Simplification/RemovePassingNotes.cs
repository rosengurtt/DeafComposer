using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Simplification
{

    public static partial class SimplificationUtilities
    {

        /// <summary>
        /// A passing note is a note with a short duration whose pitch
        /// is between the pitch of a previous note and a subsequent note
        /// There can be 1 or 2 consecutive passing notes. If there are more
        /// then we consider it to be a scale rather than a passing note
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<Note> RemovePassingNotes(List<Note> notes)
        {
            var retObj = new List<Note>();
            var voices = Utilities.GetVoices(notes);
            foreach (var voice in voices.Keys)
            {
                var voiceNotes = voices[voice].OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
                foreach (var n in voiceNotes)
                {
                    if (n.IsPercussion)
                    {
                        retObj.Add(n);
                        continue;
                    }
                    var neighboors = GetNeighboorsOfNote(n, notes)
                        .OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
                    var isPassingNote = false;
                    for (var i = 0; i < neighboors.Count; i++)
                    {
                        var prev2 = i > 1 ? neighboors[i - 2] : null;
                        var prev1 = i > 0 ? neighboors[i - 1] : null;
                        var next1 = i < neighboors.Count() - 1 ? neighboors[i + 1] : null;
                        var next2 = i < neighboors.Count() - 2 ? neighboors[i + 2] : null;

                        if (IsPassingNote(prev2, prev1, n, next1, next2))
                        {
                            isPassingNote = true;
                            break;
                        }
                    }
                    if (!isPassingNote) retObj.Add(n);
                }
            }
            return retObj;
        }
        private static bool IsPassingNote(Note prev2, Note prev1, Note n, Note next1, Note next2)
        {
            if (IsNoteBetweenNotes(prev2, prev1, n) &&
                IsNoteBetweenNotes(prev1, n, next1) &&
                IsNoteShorterThanSurroundingNotes(prev2, n, next1))
                return true;
            if (IsNoteBetweenNotes(prev1, n, next1) &&
                IsNoteBetweenNotes(n, next1, next2) &&
                IsNoteShorterThanSurroundingNotes(prev1, n, next2))
                return true;
            if (IsNoteBetweenNotes(prev1, n, next1) && IsNoteShorterThanSurroundingNotes(prev1, n, next1))
                return true;
            return false;
        }
        private static bool IsNoteBetweenNotes(Note prev, Note n, Note next)
        {
            if (prev == null || next == null) return false;
            if (prev.Pitch < n.Pitch && n.Pitch < next.Pitch) return true;
            if (prev.Pitch > n.Pitch && n.Pitch > next.Pitch) return true;
            return false;
        }
        private static bool IsNoteShorterThanSurroundingNotes(Note prev, Note n, Note next)
        {
            if (prev == null || next == null) return false;
            if (n.DurationInTicks * 2 < prev.DurationInTicks && n.DurationInTicks * 2 < next.DurationInTicks) return true;
            return false;
        }



    }
}
