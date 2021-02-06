using DeafComposer.Models.Entities;
using DeafComposer.Models.Helpers;
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
                NumberOfVoices = notesObj.Voices().Count()
            };
            return retObj;
        }
    }
}
