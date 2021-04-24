using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Analysis.Simplification
{

    static partial class SimplificationUtilities
    {
        public static List<Note> GetSimplifiedNotes(List<Note> notes, List<Bar> bars)
        {
            return RemoveNonEssentialNotes(RemoveNotesAlterations(notes, bars));
           // return RemoveNonEssentialNotes(notes);
        }
    }
}
