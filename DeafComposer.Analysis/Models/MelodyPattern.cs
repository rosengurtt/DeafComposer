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
            Duration = match.Duration;
            RelativeNotes = match.Slice1.RelativeNotes;
            RelativeNotes.ForEach(x => { x.Pitch = 0; x.DeltaPitchInSemitones = 0; });
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
                if (mp.RelativeNotes.Count != RelativeNotes.Count) return false;
                for (var i = 0; i < RelativeNotes.Count; i++)
                    if (RelativeNotes[i].DeltaPitch != mp.RelativeNotes[i].DeltaPitch || RelativeNotes[i].Tick != mp.RelativeNotes[i].Tick) return false;
                return true;
            }
        }
    }
}
