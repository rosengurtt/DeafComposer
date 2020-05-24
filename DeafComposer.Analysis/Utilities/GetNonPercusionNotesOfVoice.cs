using DeafComposer.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis
{
    public static partial class Utilities   
    {
        public static List<Note> GetNonPercusionNotesOfVoice(List<Note> notes, byte voice)
        {
            return notes.Where(n => n.Voice == voice && n.IsPercussion == false)
                .OrderBy(m => m.StartSinceBeginningOfSongInTicks).ToList();
        }
        private static List<Note> GetNonPercusionNotes(List<Note> notes)
        {
            return notes.Where(n => n.IsPercussion == false)
                .OrderBy(m => m.StartSinceBeginningOfSongInTicks).ToList();
        }
    }
}
