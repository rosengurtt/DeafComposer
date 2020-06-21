using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Artifacts
{
    public static partial class ArtifactUtilities
    {
        public static Dictionary<Artifact, List<Instance>> FindChordsInSong(SongSimplification simpl)
        {
            var durationOfHalfBeatInTicks = 48;
            var retObj = new Dictionary<Artifact, List<Instance>>();
            // We create a dictionary where the keys are points it time
            // one for every half beat of the song and the values are the notes 
            // played in that half beat
            // The assumption is that we don't have more than 2 chords in 1 beat
            // Since a note can last several beats, the same note and 
            // the same chord can be in many entries
            // of the dictionary
            var beatNotes = new Dictionary<long, List<Note>>();
            foreach (Note n in simpl.Notes)
            {
                if (n.IsPercussion) continue;
                var startBeat = n.StartSinceBeginningOfSongInTicks / durationOfHalfBeatInTicks;
                var endBeat = n.EndSinceBeginningOfSongInTicks / durationOfHalfBeatInTicks;
                for (var i = startBeat; i <= endBeat; i++)
                {
                    if (!beatNotes.ContainsKey(i)) beatNotes[i] = new List<Note>();
                    beatNotes[i].Add(n);
                }
            }
            // We now merge the consecutive half beat intervals that have the same notes
            var index = 0;
            var halfBeatsWithChords = beatNotes.Keys.OrderBy(x => x).ToList();

            while (index < halfBeatsWithChords[halfBeatsWithChords.Count - 1])
            {
                var j = 1;
                while (ArePitchesTheSame(beatNotes[index], beatNotes[index + j]))
                    j++;
                // At this point we have a chord that starts in halfbeat index and ends in halfbeat index+j
                var startHalfBeat = halfBeatsWithChords[index];
                var endHalfBeat = halfBeatsWithChords[index+j];
                var chordAsString = GetChordAsStringFromNotes(beatNotes[index]);
                if (!retObj.Keys.Where(x => x.AsString == chordAsString).Any()) {
                    var artif = new Artifact { AsString = chordAsString, ArtifactTypeId = ArtifactType.Chord };
                    retObj[artif] = new List<Instance>();
                }
                var arti = retObj.Keys.Where(x => x.AsString == chordAsString).FirstOrDefault();
                retObj[arti].Add(new Instance
                {
                    Artifact = arti,
                    Notes = beatNotes[index],
                    SongSimplificationId = simpl.Id
                });
            }
            return retObj;
        }
        private static bool ArePitchesTheSame(List<Note> group1, List<Note> group2)
        {
            foreach (var p in group1.Select(x => x.Pitch))
            {
                if (!group2.Select(x => x.Pitch).Contains(p)) return false;
            }
            foreach (var p in group2.Select(x => x.Pitch))
            {
                if (!group1.Select(x => x.Pitch).Contains(p)) return false;
            }
            return true;
        }
   private static string GetChordAsStringFromNotes(List<Note> notes)
        {
            return string.Join(",", notes.OrderBy(n => n.Pitch).Select(m => m.Pitch));
        }
    }

}
