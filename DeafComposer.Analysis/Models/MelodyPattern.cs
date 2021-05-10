using System;
using System.Collections.Generic;
using System.Linq;

namespace DeafComposer.Analysis.Models
{
    public class MelodyPattern
    {
        public MelodyPattern(MelodyMatch match)
        {
            Start=match.Start;
            End = match.End;
            RelativeNotes = match.Slice1.RelativeNotes;
            RelativeNotes.ForEach(x => { x.Pitch = 0; x.DeltaPitchInSemitones = 0; });
        }

        public List<int> Pitches
        {
            get
            {
                return RelativeNotes.Select(x => x.DeltaPitch).ToList();
            }
        }
        public List<long> Metric
        {
            get
            {
                return RelativeNotes.Select(x => x.Tick).ToList();
            }
        }

        public string AsString { 
            get
            {
                return string.Join(",", RelativeNotes.Where(y=>y.DeltaTick!=0 && y.DeltaPitch!=0).Select(x => $"({x.DeltaTick}, {x.DeltaPitch})"));
            }
        }

        public long Start { get; set; }
        public long End { get; set; }
        public long Duration
        {
            get
            {
                return End - Start;
            }
        }

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
