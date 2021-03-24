using DeafComposer.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Models.Graphs
{
    public class BeatPattern
    {
        public BeatPattern(List<Note> notas)
        {
            if (notas.Count == 0)
            {
                Name = "NullPattern";
                return;
            }
            Name = "";
            var sortedNotes = notas.OrderBy(y => y.StartSinceBeginningOfSongInTicks).ThenByDescending(z => z.Pitch).ToList();
            NoteNode currentNode = null;
            FirstNote = null;
            long beatLengthInTicks = 96;
            long startTick = (notas[0].StartSinceBeginningOfSongInTicks / beatLengthInTicks) * beatLengthInTicks;

            for (var i = 0; i < sortedNotes.Count; i++)
            {
                if (i == 0)
                {
                    currentNode = new NoteNode
                    {
                        RelativePitch = new Interval(0),
                        TicksFromStart = (int)(sortedNotes[i].StartSinceBeginningOfSongInTicks - startTick),
                        PlaysWith = new List<NoteNode>()
                    };
                    FirstNote = currentNode;
                    Name += $"{currentNode.TicksFromStart}_0";
                }
                // skip notes played together with currentNote
                var j = 0;
                while (i + j + 1 < sortedNotes.Count && sortedNotes[i].StartSinceBeginningOfSongInTicks == sortedNotes[i + j + 1].StartSinceBeginningOfSongInTicks) j++;
                if (i + j + 1 < sortedNotes.Count)
                {
                    var nextNode = new NoteNode
                    {
                        RelativePitch = new Interval(sortedNotes[i + j + 1].Pitch - sortedNotes[0].Pitch),
                        TicksFromStart = (int)(sortedNotes[i + j + 1].StartSinceBeginningOfSongInTicks - startTick),
                        PlaysWith = new List<NoteNode>()
                    };
                    var edgi = new MovesToEdge
                    {
                        DeltaPitch = new Interval(sortedNotes[i + j + 1].Pitch - sortedNotes[i].Pitch),
                        DeltaTicks = sortedNotes[i + j + 1].StartSinceBeginningOfSongInTicks - sortedNotes[i].StartSinceBeginningOfSongInTicks,
                        NextNote = nextNode
                    };
                    currentNode.Edge = edgi;
                    Name += $",{edgi.DeltaTicks}_{edgi.DeltaPitch}";
                    currentNode = nextNode;
                }
                i += j;
            }
            currentNode.Edge = null;
        }
        public string Name { get; set; }
        public NoteNode FirstNote { get; set; }
    }

}
