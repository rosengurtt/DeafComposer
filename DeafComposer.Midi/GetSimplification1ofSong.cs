using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using System.Linq;
using DeafComposer.Models.Helpers;

namespace DeafComposer.Midi
{
    public static partial class MidiUtilities
    {
        /// <summary>
        /// Simplification 1 is a version of the original Midi file where the timings of the notes have been "improved" so
        /// when we generate the music notation representation it makes more sense. For example if a note would be displayed
        /// as a sixteenth tied with a thirtysecond, when it really should be shown as a sixteenth, it makes that adjusment.
        /// It also splits the voices that are polyphonic in monophonic voices. Polyphonic means that there are notes playing
        /// simultaneously, but they don't start and end together. If we have a chord of 3 notes starting and ending together
        /// we can represent that in music notation with a single voice.
        /// So simplification 1 should sound exactly as the original midi file, but it has been massaged so it is easier to
        /// draw in musical notation
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public static SongSimplification GetSimplification1ofSong(Song song)
        {
            var notesObj0 = song.SongSimplifications[0].Notes;

            var percusionNotes = notesObj0.Where(n => n.IsPercussion == true).ToList();

            var notesObj1 = QuantizeNotes(notesObj0);
            if (notesObj1.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }

            var notesObj2 = CorrectNotesTimings(notesObj1);
            if (notesObj2.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }


            var notesObj3 = FixLengthsOfChordNotes(notesObj2);
            if (notesObj3.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }

            var notesObj4 = RemoveDuplicationOfNotes(notesObj3);
            if (notesObj4.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }
            // Split voices that have more than one melody playing at the same time
            var notesObj5 = SplitPolyphonicVoiceInMonophonicVoices(notesObj4);
            if (notesObj5.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }
            var notesObj6 = FixDurationOfLastNotes(notesObj5, song.Bars);
            if (notesObj6.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }

            // Reorder voices so when we have for ex the left and right hand of a piano in 2 voices, the right hand comes first
            var notesObj7 = ReorderVoices(notesObj6);
            if (notesObj7.Where(x => x.DurationInTicks == 0).Count() > 0)
            {

            }


            SetIdsOfModifiedNotesToZero(notesObj0, notesObj7);

            var retObj = new SongSimplification()
            {
                Notes = notesObj7,
                SimplificationVersion = 1,
                NumberOfVoices = GetVoicesOfNotes(notesObj7).Count(),
                SongId=song.Id
            };
            return retObj;
        }
        /// <summary>
        /// When we clone notes we keep the same Id, but if we are going to save data to the database, if we have
        /// 2 different notes with the same Id that is a problem, so we have to set to zero the id of cloned notes
        /// for EF to create a new record for the modified note
        /// </summary>
        /// <param name="originalNotes"></param>
        /// <param name="currentNotes"></param>
        private static void SetIdsOfModifiedNotesToZero(List<Note> originalNotes, List<Note> currentNotes)
        {
            foreach(var n in originalNotes)
            {
                var m = currentNotes.Where(x => x.Id == n.Id).FirstOrDefault();
                if (m == null) continue;
                if (!m.IsEqual(n)) m.Id = 0;
            }
        }

 






        // Returns the numbers of the voices which consist of percusion notes
        // Voices that have percusion notes, have only percusion notes
        // Percusion notes and melodic notes are never mixed together in the same voice
        private static List<byte> getPercusionVoicesOfNotes(List<Note> notes)
        {
            var instrumentsNotes = notes.Where(n => n.IsPercussion == true);
            return instrumentsNotes.Select(n => n.Voice).Distinct().ToList();
        }



    }
}

