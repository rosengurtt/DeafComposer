using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Analysis.Models
{
    /// <summary>
    /// Represents a note in a slice. Instedad of having absolutes vales like a real pitch and the ticks from the start of the song, it has values related to
    /// the start of the slice and the pitch of first note of the slice
    /// </summary>
    public class RelativeNote
    {
        public long Tick { get; set; }

        /// <summary>
        /// Represents the difference between the previous pitch and this one
        /// </summary>
        public int DeltaPitch { get; set; }

        /// <summary>
        /// The pitch in relation to the first pitch of a slice
        /// </summary>
        public int Pitch { get; set; }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                var rn = (RelativeNote)obj;
                return DeltaPitch == rn.DeltaPitch && Tick == rn.Tick;
            }
        }
    }
}
