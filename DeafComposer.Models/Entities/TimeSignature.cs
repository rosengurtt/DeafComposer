using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// Represents the standard time signature used in music, a couple of numbers expressed as a fraction
    /// </summary>
    public class TimeSignature
    {
        public long Id { get; set; }
        public int Numerator { get; set; }
        public int Denominator { get; set; }
    }
}
