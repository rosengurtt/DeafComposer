using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Models.Entities
{
    public class Interval
    {
        public Interval(int semitones)
        {
            NoSemitones = semitones;
        }
        public int NoSemitones { get; set; }


        public string Name
        {
            get
            {
                int octaves = Math.Abs(NoSemitones) / 12;
                int absNoSemitones = Math.Abs(NoSemitones);
                if (octaves > 2)
                    absNoSemitones = Math.Abs(NoSemitones) % 12;

                string name = "";
                if (absNoSemitones == 0) name = "Unison";
                if (absNoSemitones >= 1 && absNoSemitones <= 2) name = "2nd";
                if (absNoSemitones >= 3 && absNoSemitones <= 4) name = "3rd";
                if (absNoSemitones == 5) name = "4th";
                if (absNoSemitones == 6) name = "Tritone";
                if (absNoSemitones == 7) name = "5th";
                if (absNoSemitones >= 8 && absNoSemitones <= 9) name = "6th";
                if (absNoSemitones >= 10 && absNoSemitones <= 11) name = "7th";
                if (absNoSemitones == 12) name = "8ve";
                if (absNoSemitones >= 13 && absNoSemitones <= 14) name = "9th";
                if (absNoSemitones >= 15 && absNoSemitones <= 16) name = "10th";
                if (absNoSemitones == 17) name = "11th";
                if (absNoSemitones == 18) name = "Tritone+8ve";
                if (absNoSemitones == 19) name = "12th";
                if (absNoSemitones >= 20 && absNoSemitones <= 21) name = "13th";
                if (absNoSemitones >= 22 && absNoSemitones <= 23) name = "14th";

                if (NoSemitones >= 24 && NoSemitones % 12 == 0) name = $"8ve*{octaves}";
                else if (octaves > 2) name = $"{name}+{octaves}ve";

                if (NoSemitones < 0) name = $"-{name}";
                return name;

            }
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
