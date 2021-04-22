using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeafComposer.Analysis.Models
{
    public class MelodyPattern
    {
        public MelodyPattern(MelodyMatch match)
        {
            Duration = match.EndTick - match.StartTick;
            RelativeNotes = match.Slice1.RelativeNotes
                .Select(x => new RelativeNote
                {
                    DeltaPitch = x.DeltaPitch,
                    Tick = x.Tick - match.StartTick
                }).ToList();
        }



        public long Duration { get; set; }

        public List<RelativeNote> RelativeNotes { get; set; }

        public bool AreEqual(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                var mp = obj as MelodyPattern;
                if (this.Duration != mp.Duration) return false;
                return RelativeNotes.SequenceEqual(mp.RelativeNotes);
            }
        }
    }
}
