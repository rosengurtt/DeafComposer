using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis
{
    public static partial class Utilities
    {
        public static List<string> GetDurationsAndPitchesOfNotesAsListOfStrings(List<Note> notes)
        {
            var pitches = GetDeltaPitchesOfNotesAsListOfStrings(notes);
            var durations = GetDurationsAndPitchesOfNotesAsListOfStrings(notes);
            return pitches.Zip(durations, (p, d) => $"({p}-{d})").ToList();
        }
    }
}
