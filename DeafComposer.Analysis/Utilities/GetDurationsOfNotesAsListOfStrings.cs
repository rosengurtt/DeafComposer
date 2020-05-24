using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        public static List<string> GetDurationsOfNotesAsListOfStrings(List<Note> notes)
        {

          return notes.OrderBy(m => m.StartSinceBeginningOfSongInTicks)
                .Select(n => n.DurationInTicks.ToString()).ToList();

        }
    }
}
