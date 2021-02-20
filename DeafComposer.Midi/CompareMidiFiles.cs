using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeafComposer.Midi
{
    /// Methods used to investigate problems
    public static partial class MidiUtilities
    {
        public static MidiFileDifferences CompareMidiFiles(string file1Encoded, string file2Encoded)
        {
            var retObj = new MidiFileDifferences { In1NotIn2 = new List<Note>(), In2NotIn1 = new List<Note>(), NotesDifferent = new List<NoteDifference>() };
            var notesFile1 = GetNotesOfMidiFile(file1Encoded);
            var notesFile2 = GetNotesOfMidiFile(file2Encoded);

            foreach (var n in notesFile1)
            {
                if (notesFile2.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks &&
                x.EndSinceBeginningOfSongInTicks == n.EndSinceBeginningOfSongInTicks &&
                x.Pitch == n.Pitch &&
                x.Volume == n.Volume &&
                x.Instrument == n.Instrument).Count() == 1) continue;

                var similarNotes = notesFile2.Where(x =>
                     Math.Abs(x.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) < 3 &&
                  Math.Abs(x.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) < 50 &&
                  x.Pitch == n.Pitch &&
                  x.Volume == n.Volume &&
                  x.Instrument == n.Instrument).ToList();
                if (similarNotes.Count == 0)
                    retObj.In1NotIn2.Add((Note)n.Clone());
                else
                    retObj.NotesDifferent.Add(new NoteDifference { note1 = n, note2 = similarNotes[0] });
            }
            foreach (var n in notesFile2)
            {
                if (notesFile1.Where(x => x.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks &&
                x.EndSinceBeginningOfSongInTicks == n.EndSinceBeginningOfSongInTicks &&
                x.Pitch == n.Pitch &&
                x.Volume == n.Volume &&
                x.Instrument == n.Instrument).Count() == 1) continue;

                var similarNotes = notesFile1.Where(x =>
                     Math.Abs(x.StartSinceBeginningOfSongInTicks - n.StartSinceBeginningOfSongInTicks) < 3 &&
                  Math.Abs(x.EndSinceBeginningOfSongInTicks - n.EndSinceBeginningOfSongInTicks) < 50 &&
                  x.Pitch == n.Pitch &&
                  x.Volume == n.Volume &&
                  x.Instrument == n.Instrument).ToList();
                if (similarNotes.Count == 0)
                    retObj.In2NotIn1.Add((Note)n.Clone());
                else
                {
                    // Check if we had already captured this difference
                    if (retObj.NotesDifferent.Where(x => x.note1.TempId == similarNotes[0].TempId && x.note2.TempId == n.TempId).Count() == 0)
                        retObj.NotesDifferent.Add(new NoteDifference { note2 = (Note)n.Clone(), note1 = (Note)similarNotes[0].Clone() });
                }
            }
            return retObj;
        }


        public static MidiFileDifferences CompareListOfNotes(List<Note> notes1, List<Note> notes2)
        {
            var retObj = new MidiFileDifferences
            {
                In1NotIn2 = new List<Note>(),
                In2NotIn1 = new List<Note>(),
                NotesDifferent = new List<NoteDifference>(),
                DifferentStart = new List<NoteDifference>(),
                DifferentEnd = new List<NoteDifference>(),
                VoiceChange = new List<NoteDifference>()
            };

            foreach (var n in notes1)
            {
                var match = notes2.Where(y => y.TempId == n.TempId).FirstOrDefault();
                if (match == null)
                    retObj.In1NotIn2.Add(n);
                else
                {
                    if (n.StartSinceBeginningOfSongInTicks != match.StartSinceBeginningOfSongInTicks)
                        retObj.DifferentStart.Add(new NoteDifference { note1 = n, note2 = match });
                    if (n.EndSinceBeginningOfSongInTicks != match.EndSinceBeginningOfSongInTicks)
                        retObj.DifferentEnd.Add(new NoteDifference { note1 = n, note2 = match });
                    if (n.Voice != match.Voice)
                        retObj.VoiceChange.Add(new NoteDifference { note1 = n, note2 = match });
                }
            }
            foreach (var n in notes2)
            {
                if (!notes1.Select(y => y.TempId).Contains(n.TempId))
                    retObj.In2NotIn1.Add(n);
            }

            return retObj;
        }

        public static string GetBase64encodedFile(string filePath)
        {
            Byte[] bytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(bytes);
        }


        public static List<NoteEvolution> GetNotesEvolution(List<Note> notes, List<NoteEvolution> evolutionUntilNow = null)
        {
            if (evolutionUntilNow == null)
                evolutionUntilNow = new List<NoteEvolution>();

            var length = evolutionUntilNow.Count > 0 ? evolutionUntilNow[0].start.Count : 0;

            foreach (var n in notes)
            {
                var ne = evolutionUntilNow.Where(x => x.TempId == n.TempId).FirstOrDefault();

                if (ne == null)
                {
                    ne = new NoteEvolution
                    {
                        TempId = n.TempId,
                        start = new List<long?>(),
                        end = new List<long?>(),
                        voice = new List<byte?>()
                    };
                    for (var i = 0; i < length; i++)
                    {
                        ne.start.Add(null);
                        ne.end.Add(null);
                        ne.voice.Add(null);
                        evolutionUntilNow.Add(ne);
                    }
                    evolutionUntilNow.Add(ne);
                }
                ne.start.Add(n.StartSinceBeginningOfSongInTicks);
                ne.end.Add(n.EndSinceBeginningOfSongInTicks);
                ne.voice.Add(n.Voice);
            }
            return evolutionUntilNow;
        }
    }

    public class MidiFileDifferences
    {
        public List<Note> In1NotIn2 { get; set; }
        public List<Note> In2NotIn1 { get; set; }
        public List<NoteDifference> DifferentStart { get; set; }
        public List<NoteDifference> DifferentEnd { get; set; }
        public List<NoteDifference> NotesDifferent { get; set; }
        public List<NoteDifference> VoiceChange { get; set; }
    }
    public class NoteDifference
    {
        public Note note1 { get; set; }
        public Note note2 { get; set; }
    }

    public class NoteEvolution
    {
        public Guid TempId { get; set; }
        public List<long?> start { get; set; }
        public List<long?> end { get; set; }
        public List<byte?> voice { get; set; }
    }

}
