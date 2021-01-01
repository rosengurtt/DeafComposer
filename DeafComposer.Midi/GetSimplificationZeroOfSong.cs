using DeafComposer.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        public static SongSimplification GetSimplificationZeroOfSong(string base64encodedMidiFile)
        {

            var notesObj = GetNotesOfMidiFile(base64encodedMidiFile);

            var retObj = new SongSimplification()
            {
                Notes = notesObj,
                SimplificationVersion = 0,
                NumberOfVoices = GetVoicesOfNotes(notesObj).Count()
            };
            return retObj;
        }
        // Given a group of notes that may be played by several voices, returns all the different
        // voices found in the notes
        private static List<int> GetVoicesOfNotes(List<Note> notes)
        {
            var retObj = new List<int>();
            foreach (var v in notes.Select(n => n.Voice).Distinct())
                retObj.Add(v);
            return retObj;
        }
    }
}
