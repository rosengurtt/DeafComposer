using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis.Artifacts
{
    public static partial class ArtifactUtilities
    {
        public static Dictionary<Artifact, List<Instance>> FindArtifactsInSong(
        Song song,
        SongSimplification simpl,
        ArtifactType ArtifactType,
        int minLengthToSearch = 3,
        int maxLengthToSearch = 12)
        {
            var retObj = new Dictionary<Artifact, List<Instance>>();
            foreach (var instr in song.SongStats.Instruments)
            {
                var notes = simpl.NotesOfInstrument(instr);
                var voicesNotes = Utilities.GetVoices(notes.ToList());
                foreach (var voice in voicesNotes.Keys)
                {
                    List<Note> melody =  Utilities.GetMelodyOfVoice(voicesNotes[voice]).ToList();
                    var elements = new List<string>();
                    switch (ArtifactType)
                    {
                        case ArtifactType.PitchPattern:
                            elements = Utilities.GetDeltaPitchesOfNotesAsListOfStrings(melody);
                            break;
                        case ArtifactType.RythmPattern:
                            elements = Utilities.GetDurationsOfNotesAsListOfStrings(melody);
                            break;
                        case ArtifactType.MelodyPattern:
                            elements = Utilities.GetDurationsAndPitchesOfNotesAsListOfStrings(melody);
                            break;
                    }
                    var Artifacts = FindPatternsInListOfStrings(elements, minLengthToSearch, maxLengthToSearch);
                    foreach (var art in Artifacts)
                    {
                        var artif = new Artifact() { AsString = art.Key, ArtifactTypeId = ArtifactType };
                        var instances = new List<Instance>();
                        foreach (var instance in art.Value)
                        {
                            var ArtifactLength = art.Key.Split(",").Length;
                            var instNotes = melody.ToArray()[instance..(instance + ArtifactLength)].ToList();
                            var i = new Instance()
                            {
                                Artifact = artif,
                                Notes = instNotes,
                                SongSimplificationId = simpl.Id
                            };
                            instances.Add(i);
                        }
                        retObj[artif] = instances;
                    }
                }

            }
            return SimplifyArtifactsInstances(retObj);
        }

 

        private static Note FindNoteOfSong(Note note, Song song, int version, int instr)
        {
            return song.SongSimplifications[version].Notes
                            .Where(n => n.Instrument == instr & n.Pitch == note.Pitch &
                            n.StartSinceBeginningOfSongInTicks == note.StartSinceBeginningOfSongInTicks &
                            n.EndSinceBeginningOfSongInTicks == note.EndSinceBeginningOfSongInTicks)
                            .FirstOrDefault();
        }


        public static Dictionary<Artifact, List<Instance>> SimplifyArtifactsInstances(Dictionary<Artifact, List<Instance>> ArtifactsInst)
        {
            var Artifacts = ArtifactsInst.Keys.ToList();

            var simplifiedArtifactsInstances = new Dictionary<Artifact, List<Instance>>();
            foreach (var pat in Artifacts)
            {
                var patSimplified = SimplifyArtifact(pat);
                if (!simplifiedArtifactsInstances.Keys.Contains(patSimplified))
                {
                    simplifiedArtifactsInstances[patSimplified] = new List<Instance>();
                }
                foreach (var oc in ArtifactsInst[pat])
                {
                    var clonito = oc.Clone();
                    clonito.Artifact = patSimplified;
                    simplifiedArtifactsInstances[patSimplified].Add(clonito);
                }
            }
            return simplifiedArtifactsInstances;
        }

    }
}
