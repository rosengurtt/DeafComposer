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
            var nonPercusionNotes = song.SongSimplifications[0].Notes.Where(n => n.IsPercussion == false).ToList();


            var notesObj1 = QuantizeNotes(nonPercusionNotes);
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

            //// Reorder voices so when we have for ex the left and right hand of a piano in 2 voices, the right hand comes first
            //var notesObj7 = ReorderVoices(notesObj6);
            //if (notesObj7.Where(x => x.DurationInTicks == 0).Count() > 0)
            //{

            //}


            var qtyNonPercusionVoices = notesObj6.NonPercussionVoices().Count();

            var percusionNotes = song.SongSimplifications[0].Notes.Where(n => n.IsPercussion == true).ToList();

            var percusionNotes2 = QuantizeNotes(percusionNotes);
            var soret = GetNotesChanged(percusionNotes, percusionNotes2);


            var percusionNotes3 = ReassignPercussionVoices(percusionNotes2, qtyNonPercusionVoices);


            var notesObj8 = (notesObj6.Concat(percusionNotes3)).OrderBy(x => x.StartSinceBeginningOfSongInTicks).ToList();

            SetIdsOfModifiedNotesToZero(song.SongSimplifications[0].Notes, notesObj8);


            var retObj = new SongSimplification()
            {
                Notes = notesObj8,
                SimplificationVersion = 1,
                NumberOfVoices = notesObj8.Voices().Count(),
                SongId = song.Id
            };
            return retObj;
        }

        private static List<(Note,Note)> GetNotesChanged(List<Note> initial, List<Note> final)
        {
            var retObj = new List<(Note, Note)>();
            foreach (var n in initial)
            {
                var m = final.Where(m => m.Id == n.Id).FirstOrDefault();
                if (m.StartSinceBeginningOfSongInTicks != n.StartSinceBeginningOfSongInTicks)
                    retObj.Add((n, m));
            }
            return retObj;
        }

        /// <summary>
        /// When we process the non percusion notes, we may end up with more voices than originally in the song and even if we don't add
        /// voices we may be changing the voice numbers to order them highest pitch first
        /// So we have to assign new voice numbers to the percussion voices to avoid conflicts. We keep the different percussion voices as
        /// they were but we assign them numbers that start from the highest non percusion voice + 1 and upwards
        /// </summary>
        /// <param name="notes"></param>
        /// <param name="qtyNonPercusionVoices"></param>
        /// <returns></returns>
        private static  List<Note> ReassignPercussionVoices(List<Note> notes, int qtyNonPercusionVoices)
        {
            var retObj = new List<Note>();
            var percusionVoices = getPercusionVoicesOfNotes(notes);
            var counter = 0;
            foreach(var v in percusionVoices)
            {
                byte newVoiceNumber = (byte)(qtyNonPercusionVoices + counter);
                var voiceNotes = notes.Where(n => n.Voice == v).ToList().Clone();
                foreach (var m in voiceNotes)
                    m.Voice = newVoiceNumber;
                retObj = retObj.Concat(voiceNotes).ToList();
                counter++;
            }
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

