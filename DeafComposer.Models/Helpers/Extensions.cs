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

    }
}
