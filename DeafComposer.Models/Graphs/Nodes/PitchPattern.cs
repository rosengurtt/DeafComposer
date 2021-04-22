
using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Models.Graphs.Nodes
{
    public class PitchPattern: MusicalNode
    {
        public PitchPattern(List<Note> notes, long beatStartTick, int voice, Note previousNote = null)
        {
            Tick = beatStartTick;
            Voice = voice;
            QtyNotes = 0;
            if (notes.Count == 0)
            {
                Name = "NullPattern";
                return;
            }
            Name = "";

            var highestPitch = previousNote!=null? Math.Max(previousNote.Pitch, notes.Max(x => x.Pitch)): notes.Max(x => x.Pitch);
            var lowestPitch= previousNote != null ? Math.Min(previousNote.Pitch, notes.Min(x => x.Pitch)): notes.Min(x => x.Pitch);
            Range = new Interval(highestPitch - lowestPitch);

            var sortedNotes = notes.OrderBy(y => y.StartSinceBeginningOfSongInTicks).ThenByDescending(z => z.Pitch).ToList();
            var firstPitch = previousNote != null ? previousNote.Pitch : sortedNotes[0].Pitch;
            var lastPitch = notes.OrderByDescending(y => y.StartSinceBeginningOfSongInTicks).ThenByDescending(z => z.Pitch).FirstOrDefault().Pitch;
            TotalDeltaPitch = new Interval(lastPitch - firstPitch);

            IsMonotone = true;
            for (var i = 0; i < sortedNotes.Count - 1; i++)
            {
                if ((firstPitch - lastPitch) * (sortedNotes[i].Pitch - sortedNotes[i + 1].Pitch) < 0 ||
                   previousNote != null && (previousNote.Pitch - lastPitch) * (previousNote.Pitch - sortedNotes[0].Pitch) < 0)
                {
                    IsMonotone = false;
                    break;
                }
            }

            PitchStep currentNode = null;
            FirstPitchStep = null;

            for (var i = 0; i < sortedNotes.Count; i++)
            {
                if (i == 0)
                {
                    currentNode = new PitchStep
                    {
                        DeltaPitch = new Interval(previousNote != null ? (int)(sortedNotes[i].Pitch - previousNote.Pitch) : 0)
                    };
                    FirstPitchStep = currentNode;
                    Name += $"{currentNode.DeltaPitch.ToString()}";
                    QtyNotes++;
                }
                // skip notes played together with currentNote
                var j = 0;
                while (i + j + 1 < sortedNotes.Count && sortedNotes[i].StartSinceBeginningOfSongInTicks == sortedNotes[i + j + 1].StartSinceBeginningOfSongInTicks) j++;
                if (i + j + 1 < sortedNotes.Count)
                {
                    var nextNode = new PitchStep
                    {
                        DeltaPitch = new Interval(sortedNotes[i + j + 1].Pitch - sortedNotes[i].Pitch)
                    };
                    currentNode.NextPitchStep = nextNode;
                    Name += $"_{nextNode.DeltaPitch.ToString()}";
                    currentNode = nextNode;
                    QtyNotes++;
                }
                i += j;
            }
            currentNode.NextPitchStep = null;
        }

        public int QtyNotes { get; set; }
        /// <summary>
        /// The difference between the highest pitch and the lowest pitch
        /// </summary>
        public Interval Range { get; set; }

        /// <summary>
        /// The difference between the last pitch in the sequence and the pitch of the note before the pitch sequence
        /// A positive value means the pitch went up, negative if it went down
        /// </summary>
        public Interval TotalDeltaPitch { get; set; }
       

        /// <summary>
        /// If pitch changes always in the same direction
        /// </summary>
        public bool IsMonotone { get; set; }

        /// <summary>
        /// The largest delta pitch between 2 consecutive notes
        /// </summary>
        public int LargestDeltaPitch { get; set; }
        public string Name { get; set; }

        public PitchStep FirstPitchStep { get; set; }
        public PitchPattern NextPitchPattern { get; set; }
    }
}
