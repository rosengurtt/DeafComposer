using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using System.Linq;
using DeafComposer.Models.Helpers;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// If we have 2 notes with the same pitch and the same instrument playing at the same time, this
        /// is problematic for the analysis and displaying. So we do one of 2 things:
        /// - if the notes start at the same time (or almost at the same time) we remove one
        /// - if the notes start in different times, we shorten the first, so they don't play simultaneously
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        private static List<Note> RemoveDuplicationOfNotes(List<Note> notes)
        {
            // We first copy all the notes to retObj, we will then remove and alter notes in rettObj, but the original notes are left unchanged
            var retObj = notes.Clone().OrderBy(n => n.StartSinceBeginningOfSongInTicks).ToList();
            // If 2 notes with the same pitch and the same instrument start at the same time, we remove the 
            // one with the lower volume, or if the volume is more or less the same, the shortest one
            var notesToRemove = new List<Note>();
            var volumeTolerance = 5;    // The amount the volumes have to differ to consider them different
            var voices = retObj.NonPercussionVoices();
            foreach (var v in voices)
            {
                var notesOfVoice = retObj.Where(w => w.Voice == v)
                                    .OrderBy(z => z.StartSinceBeginningOfSongInTicks).ToList();
                foreach (var n in notesOfVoice)
                {
                    var duplicateCandidates = notesOfVoice.Where(m => m.Id != n.Id && GetIntersectionOfNotesInTicks(n, m) > 0 && m.Pitch == n.Pitch);

                    foreach(var m in duplicateCandidates)
                    {
                        var timeTolerance = GetToleranceForComparingNotes(n, m);
                        if (StartDifference(m,n) <= timeTolerance)
                        {
                            // They start together, Select the one to remove and store it in notesToRemove
                            if (Math.Abs(n.Volume - m.Volume) > volumeTolerance)
                            {
                                var quietestNote = n.Volume <= m.Volume ? n : m;
                                notesToRemove.Add(quietestNote);
                                continue;
                            }
                            var shortestNote = n.DurationInTicks <= m.DurationInTicks ? n : m;
                            notesToRemove.Add(shortestNote);
                        }
                        else
                        {
                            // They don't start together. Shorten the first one
                            var firstNote = n.StartSinceBeginningOfSongInTicks <= m.StartSinceBeginningOfSongInTicks ? n : m;
                            var secondNote = n.StartSinceBeginningOfSongInTicks <= m.StartSinceBeginningOfSongInTicks ? m : n;
                            firstNote.EndSinceBeginningOfSongInTicks = secondNote.StartSinceBeginningOfSongInTicks;
                        }
                    }             
                }
            }
            // Now remove the duplicate notes
            foreach (var n in notesToRemove) retObj.Remove(n);
            return retObj.OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();
        }

        /// <summary>
        /// Given 2 notes, it returns the interval of time in ticks when the 2 notes are playing simultaneously
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static long GetIntersectionOfNotesInTicks(Note n, Note m)
        {
            return Math.Min(m.EndSinceBeginningOfSongInTicks, n.EndSinceBeginningOfSongInTicks) -
                 Math.Max(m.StartSinceBeginningOfSongInTicks, n.StartSinceBeginningOfSongInTicks);
        }

    }
}
