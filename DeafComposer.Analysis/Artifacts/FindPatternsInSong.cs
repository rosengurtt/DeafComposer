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
        int version,
        ArtifactType ArtifactType,
        int minLengthToSearch = 3,
        int maxLengthToSearch = 12)
        {
            var retObj = new Dictionary<Artifact, List<Instance>>();
            foreach (var instr in song.SongStats.Instruments)
            {
                var notes = song.SongSimplifications[version].NotesOfInstrument(instr);
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
                    foreach (var pat in Artifacts)
                    {
                        var patito = new Artifact() { AsString = pat.Key, ArtifactTypeId = ArtifactType };
                        var ocur = new List<Instance>();
                        foreach (var oc in pat.Value)
                        {
                            var firstNote = melody[oc];
                            var ArtifactLength = pat.Key.Split(",").Length;
                            var ocNotes = melody.ToArray()[oc..(oc + ArtifactLength)].ToList();
                            var o = new Instance()
                            {
                                Artifact = patito,
                                Notes = ocNotes,
                                SongSimplificationId = song.SongSimplifications[version].Id
                            };
                            ocur.Add(o);
                        }
                        retObj[patito] = ocur;
                    }
                }

            }
            return SimplifyArtifactsOcurrences(retObj);
        }

 

        private static Note FindNoteOfSong(Note note, Song song, int version, int instr)
        {
            return song.SongSimplifications[version].Notes
                            .Where(n => n.Instrument == instr & n.Pitch == note.Pitch &
                            n.StartSinceBeginningOfSongInTicks == note.StartSinceBeginningOfSongInTicks &
                            n.EndSinceBeginningOfSongInTicks == note.EndSinceBeginningOfSongInTicks)
                            .FirstOrDefault();
        }


        public static Dictionary<Artifact, List<Instance>> SimplifyArtifactsOcurrences(Dictionary<Artifact, List<Instance>> ArtifactsOc)
        {
            var Artifacts = ArtifactsOc.Keys.ToList();

            var simplifiedArtifactsOcurrences = new Dictionary<Artifact, List<Instance>>();
            foreach (var pat in Artifacts)
            {
                var patSimplified = SimplifyArtifact(pat);
                if (!simplifiedArtifactsOcurrences.Keys.Contains(patSimplified))
                {
                    simplifiedArtifactsOcurrences[patSimplified] = new List<Instance>();
                }
                foreach (var oc in ArtifactsOc[pat])
                {
                    var clonito = oc.Clone();
                    clonito.Artifact = patSimplified;
                    simplifiedArtifactsOcurrences[patSimplified].Add(clonito);
                }
            }
            return simplifiedArtifactsOcurrences;
        }

    }
}
