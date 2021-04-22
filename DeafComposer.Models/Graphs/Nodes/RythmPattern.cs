using DeafComposer.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Models.Graphs.Nodes
{
    public class RythmPattern: MusicalNode
    {
        public RythmPattern(List<Note> notes, long beatStartTick, int voice)
        {
            Tick = beatStartTick;
            Voice = voice;
            QtyNotes = 0;
            IsUniform = true;

            if (notes.Count == 0)
            {
                Name = "NullPattern";
                return;
            }
            Name = "";
            int firstDeltaTick = 0;
            var sortedNotes = notes.OrderBy(y => y.StartSinceBeginningOfSongInTicks).ThenByDescending(z => z.Pitch).ToList();
            RythmStep currentNode = null;
            FirstRythmStep = null;
            long beatLengthInTicks = 96;
            long startTick = (notes[0].StartSinceBeginningOfSongInTicks / beatLengthInTicks) * beatLengthInTicks;
            var averageVolume = notes.Average(x => x.Volume);

            for (var i = 0; i < sortedNotes.Count; i++)
            {
                if (i == 0)
                {
                    currentNode = new RythmStep
                    {
                        DeltaTicks = (int)(sortedNotes[i].StartSinceBeginningOfSongInTicks - startTick),
                        Volume = sortedNotes[i].Volume > averageVolume * 1.3 ? VolumeRythm.accented : VolumeRythm.normal
                    };
                    HasAccents = currentNode.Volume == VolumeRythm.accented;
                    firstDeltaTick = currentNode.DeltaTicks;
                    FirstRythmStep = currentNode;
                    Name += $"{currentNode.DeltaTicks}";
                    if (currentNode.Volume == VolumeRythm.accented) Name += "acc";
                    QtyNotes++;
                }
                // skip notes played together with currentNote
                var j = 0;
                while (i + j + 1 < sortedNotes.Count && sortedNotes[i].StartSinceBeginningOfSongInTicks == sortedNotes[i + j + 1].StartSinceBeginningOfSongInTicks) j++;
                if (i + j + 1 < sortedNotes.Count)
                {
                    var nextNode = new RythmStep
                    {
                        DeltaTicks = (int)(sortedNotes[i + j + 1].StartSinceBeginningOfSongInTicks - sortedNotes[i].StartSinceBeginningOfSongInTicks),
                        Volume = sortedNotes[i].Volume > averageVolume * 1.3 ? VolumeRythm.accented : VolumeRythm.normal
                    };
                    currentNode.NextRythmStep = nextNode;
                    Name += $"_{nextNode.DeltaTicks}";
                    currentNode = nextNode;
                    HasAccents = HasAccents || currentNode.Volume == VolumeRythm.accented;
                    IsUniform = IsUniform && currentNode.DeltaTicks == firstDeltaTick;
                    QtyNotes++;
                }
                i += j;
            }
            currentNode.NextRythmStep = null;
        }
        public string Name { get; set; }
        public int QtyBeats { get; set; }

        /// <summary>
        /// Total of notes played
        /// </summary>
        public int QtyNotes { get; set; }

        /// <summary>
        /// True if all time intervals are equal
        /// </summary>
        public bool IsUniform { get; set; }

        /// <summary>
        /// True if there are notes with accents
        /// </summary>
        public bool HasAccents { get; set; }

        public RythmStep FirstRythmStep { get; set; }
        public RythmPattern NextRythmPattern { get; set; }
    }
}
