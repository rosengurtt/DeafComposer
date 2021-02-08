using DeafComposer.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Models.Helpers
{
    public static class Extensions
    {
        public static List<T> Clone<T>(this List<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static List<byte> Voices(this List<Note> notes)
        {
            return notes.Select(n => n.Voice).Distinct().OrderBy(x => x).ToList();
        }
        public static List<byte> NonPercussionVoices(this List<Note> notes)
        {
            return notes.Where(m => m.IsPercussion == false).Select(n => n.Voice).Distinct().OrderBy(x=>x).ToList();
        }
        public static List<byte> SubVoices(this List<Note> notes)
        {
            return notes.Select(n => n.SubVoice).Distinct().OrderBy(x => x).ToList();
        }
        public static List<byte> NonPercussionSubVoices(this List<Note> notes)
        {
            return notes.Where(m => m.IsPercussion == false).Select(n => n.SubVoice).Distinct().OrderBy(x => x).ToList();
        }
        /// <summary>
        /// Returns the notes that are part of chords
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public static List<Note> InChords(this List<Note> notes)
        {
            return notes.Where(n =>
            notes.Where(m => m.Id != n.Id && m.StartSinceBeginningOfSongInTicks == n.StartSinceBeginningOfSongInTicks &&
            m.EndSinceBeginningOfSongInTicks == n.EndSinceBeginningOfSongInTicks).Count() > 2).ToList();
        }

        /// <summary>
        /// Returns a dictionary where the key are the subvoices present in notes, and the value is the average pitch of the
        /// notes of that subvoice
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public  static Dictionary<int,double> SubVoicesPitchAverage(this List<Note> notes)
        {
            var retObj = new Dictionary<int, double>();
            var subVoices = notes.Select(n => n.SubVoice).Distinct().OrderBy(x => x).ToList();
            foreach (var sv in subVoices)
            {
                retObj[sv] = notes.Where(y => y.SubVoice == sv).Average(z => z.Pitch);
            }
            return retObj;
        }
    }
}
