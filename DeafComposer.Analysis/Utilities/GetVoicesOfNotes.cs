using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        /// <summary>
        /// If we have a group of notes from a song, all played with the same instrument,
        /// but corresponding to different voices (or tracks), this method splits the list
        /// in as many voices, one list per voice.
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static Dictionary<int, List<Note>> GetVoices(List<Note> notes)
        {
            var voicesNotes = new Dictionary<int, List<Note>>();
            foreach (var note in notes)
            {
                if (!voicesNotes.Keys.Contains(note.Voice))
                    voicesNotes[note.Voice] = new List<Note>();
                voicesNotes[note.Voice].Add(note);
            }
            return voicesNotes;
        }
    }
}
