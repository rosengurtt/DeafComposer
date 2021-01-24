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
        /// Applies all the functions that remove different types of embelishments
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<Note> RemoveEmbelishments(List<Note> notes)
        {
            var retObj = RemoveMordents(notes);
            retObj = RemoveTrills(retObj);
            return RemoveTurns(retObj);
        }
      
        public static List<Note> RemoveMordents(List<Note> notes)
        {
            var retObj = new List<Note>();
            var voices = Utilities.GetVoices(notes);
            // We put in this list the notes that have been found to be part of a mordent
            // and therefore have been absorved to a previous note, so we don't compute them twice
            var membersOfMordents = new List<Note>();
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
                    if (membersOfMordents.Contains(n)) continue;
                    var neighboors = GetNeighboorsOfNote(n, notes)
                    .OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
                    for (var i = 0; i < neighboors.Count; i++)
                    {
                        var next1 = i < neighboors.Count() - 1 ? neighboors[i + 1] : null;
                        var next2 = i < neighboors.Count() - 2 ? neighboors[i + 2] : null;

                        if (next1 != null && next2 != null && IsMordent(n, next1, next2))
                        {
                            n.EndSinceBeginningOfSongInTicks = next2.EndSinceBeginningOfSongInTicks;
                            membersOfMordents.Add(next1);
                            membersOfMordents.Add(next2);
                            break;
                        }
                    }
                    retObj.Add(n);

                }
            }
            return retObj;
        }

        public static List<Note> RemoveTurns(List<Note> notes)
        {
            var retObj = new List<Note>();
            var voices = Utilities.GetVoices(notes);
            // We put in this list the notes that have been found to be part of a mordent
            // and therefore have been absorved to a previous note, so we don't compute them twice
            var membersOfTurns = new List<Note>();
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
                    if (membersOfTurns.Contains(n)) continue;
                    var neighboors = GetNeighboorsOfNote(n, notes, 2, 4, true)
                    .OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();

                    var sliceOf6 = neighboors.Take(6).ToArray();
                    var sliceOf7 = neighboors.Take(7).ToArray();

                    if (IsTurn(sliceOf6))
                    {
                        var left = n;
                        var right = sliceOf6[5];
                        var middle = new Note
                        {
                            Pitch = (byte)Math.Round(neighboors.Take(4).Average(x => x.Pitch)),
                            StartSinceBeginningOfSongInTicks = sliceOf6[0].StartSinceBeginningOfSongInTicks,
                            EndSinceBeginningOfSongInTicks = sliceOf6[4].EndSinceBeginningOfSongInTicks,
                            Instrument = n.Instrument,
                            IsPercussion = false,
                            Voice = n.Voice,
                            Volume = (byte)Math.Round(neighboors.Take(4).Average(x => (int)x.Volume))
                        };
                        retObj.Add(left);
                        retObj.Add(middle);
                        retObj.Add(right);
                        foreach (var m in sliceOf6)
                            membersOfTurns.Add(m);
                    }
                    else if (IsTurn(sliceOf7))
                    {
                        var left = n;
                        var right = sliceOf7[6];
                        var middle = new Note
                        {
                            Pitch = (byte)Math.Round(neighboors.Take(5).Average(x => (int)x.Pitch)),
                            StartSinceBeginningOfSongInTicks = sliceOf7[0].StartSinceBeginningOfSongInTicks,
                            EndSinceBeginningOfSongInTicks = sliceOf7[5].EndSinceBeginningOfSongInTicks,
                            Instrument = n.Instrument,
                            IsPercussion = false,
                            Voice = n.Voice,
                            Volume = (byte)Math.Round(neighboors.Take(5).Average(x => (int)x.Volume))
                        };
                        retObj.Add(left);
                        retObj.Add(middle);
                        retObj.Add(right);
                        foreach (var m in sliceOf7)
                            membersOfTurns.Add(m);
                    }
                    else
                        retObj.Add(n);
                }
            }
            return retObj;
        }

        /// <summary>
        /// A trill, is a rapid alternation between an indicated note and the one above it.
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<Note> RemoveTrills(List<Note> notes)
        {
            var retObj = new List<Note>();
            var voices = Utilities.GetVoices(notes);
            // We put in this list the notes that have been found to be part of a trill
            // and therefore have been absorved to a previous note, so we don't compute them twice
            var membersOfTrills = new List<Note>();
            foreach (var voice in voices.Keys)
            {
                var voiceNotes = voices[voice].OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
                foreach (var n in voiceNotes)
                {
                    if (membersOfTrills.Contains(n)) continue;
                    if (n.IsPercussion)
                    {
                        retObj.Add(n);
                        continue;
                    }
                    var neighboors = GetNeighboorsOfNote(n, notes, 2, 12, true)
                        .OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
                    if (neighboors.Count() < 8)
                    {
                        retObj.Add(n);
                        continue;
                    }
                    var j = 0;
                    while (neighboors[j].Pitch == neighboors[j + 2].Pitch &&
                        neighboors[j + 1].Pitch == neighboors[j + 3].Pitch &&
                        neighboors[j].Pitch != neighboors[j + 1].Pitch) j += 2;
                    if (j >= 8)
                    {
                        // We replace the trill by a note that has the pitch of the first note and last
                        // until the end of the trill
                        var extendedNote = new Note
                        {
                            Pitch = (byte)Math.Round(neighboors.Take(4).Average(x => (int)x.Pitch)),
                            StartSinceBeginningOfSongInTicks = n.StartSinceBeginningOfSongInTicks,
                            EndSinceBeginningOfSongInTicks = neighboors[j + 1].EndSinceBeginningOfSongInTicks,
                            Instrument = n.Instrument,
                            IsPercussion = false,
                            Voice = n.Voice,
                            Volume = (byte)Math.Round(neighboors.Take(j * 2).Average(x => (int)x.Volume))
                        };
                        retObj.Add(extendedNote);
                        for (int i = 1; i < j; i++)
                            membersOfTrills.Add(neighboors[i]);
                    }
                    else
                        retObj.Add(n);

                }
            }
            return retObj;
        }
        /// <summary>
        /// Neighboors of a note are notes that start at the same time or up to x quarter notes later
        /// and that have a pitch that is not more than y semitones higher or lower
        /// include_n is a flag to indicate if the note n has to be included in the list returned
        /// </summary>
        /// <param name="n"></param>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> GetNeighboorsOfNote(
            Note n,
            List<Note> notes,
            int distanceInSemitones = 2,
            int distanceInQuarterNotes = 4,
            bool include_n = false)
        {
            var retObj = notes.Where(x => x != n && Math.Abs(x.Pitch - n.Pitch) <= distanceInSemitones &&
             (x.StartSinceBeginningOfSongInTicks >= n.StartSinceBeginningOfSongInTicks) &&
             (x.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) < 96 * distanceInQuarterNotes)
                .ToList();
            if (include_n) retObj = retObj.Prepend(n).ToList();
            return retObj;
        }

  



        /// <summary>
        /// A mordent is a rapid alternation between an indicated note, the note above 
        /// or below  and the indicated note again
        /// </summary>
        /// <param name="n"></param>
        /// <param name="next1"></param>
        /// <param name="next2"></param>
        /// <returns></returns>
        private static bool IsMordent(Note n, Note next1, Note next2)
        {
            if (n.Pitch != next2.Pitch) return false;
            if (Math.Abs(n.Pitch - next1.Pitch) > 2 || n.Pitch == next1.Pitch) return false;
            if ((n.DurationInTicks + next1.DurationInTicks) * 2 <= next2.DurationInTicks)
                return true;
            return false;
        }

        /// <summary>
        /// A turn is a short figure consisting of the note above the one indicated, the note itself, 
        /// the note below the one indicated, and the note itself again.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private static bool IsTurn(Note[] n)
        {
            var length = n.Length;
            if (length < 6 || length > 7) return false;
            if (!AreNotesPitchesClose(n)) return false;
            if (!(n[0].Pitch > n[1].Pitch && n[1].Pitch > n[2].Pitch)) return false;
            if (!(n[n.Length - 1].Pitch > n[n.Length - 2].Pitch && n[n.Length - 2].Pitch > n[n.Length - 3].Pitch)) return false;
            var durationOfExtremes = n[0].DurationInTicks + n[n.Length - 1].DurationInTicks;
            var durationOfInteriors = 0;
            for (var i = 1; i < n.Length - 1; i++) durationOfInteriors += n[i].DurationInTicks;
            if (length == 6 && durationOfExtremes > 2 * durationOfInteriors)
                return true;
            if (length == 7 && durationOfExtremes > 1.6 * durationOfInteriors)
                return true;
            return false;
        }

        /// <summary>
        /// Given a group of notes, it returns true if all the notes are inside an interval of
        /// 2 semitones. If there is an interval of 3 or more semitones between 2 notes of the
        /// group, it returns false
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private static bool AreNotesPitchesClose(Note[] n)
        {
            for (int i = 0; i < n.Length - 1; i++)
            {
                for (int j = 1; j < n.Length; j++)
                {
                    if (Math.Abs(n[i].Pitch - n[j].Pitch) > 2) return false;
                }
            }
            return true;
        }
    }
}
