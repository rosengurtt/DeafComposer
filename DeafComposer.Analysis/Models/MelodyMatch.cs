namespace DeafComposer.Analysis.Models
{
    /// <summary>
    /// Represents a couple of passages in a song that are identical or similar
    /// When they are identical (except for a transposicion) Differences=0. Otherwise, for each note that doesn't match, Differences increase by 1
    /// AreTransposed and NotesAreTheSame gives information about the notes having the same pitch or a transposed pitch
    /// When NotesAreTheSame=true and AreTransposed=true it means they are separate a number of octaves but the notes are the same
    /// </summary>
    public class MelodyMatch
    {
        public NotesSlice Slice1 { get; set; }
        public NotesSlice Slice2 { get; set; }

        public long Start { get; set; }
        public long End { get; set; }

        public long DurationInTicks
        {
            get
            {
                return End - Start;
            }
        }

        public double DurationInBeats
        {
            get
            {
                return DurationInTicks  * 4 / Slice1.Bar.TimeSignature.Denominator / (double) 96;
            }
        }

        public (byte, long,long) LocationSlice1
        {
            get
            {
                return (Slice1.Voice, Slice1.BarNumber, Slice1.BeatNumberFromBarStart);
            }
        }
        public (byte, long, long) LocationSlice2
        {
            get
            {
                return (Slice2.Voice, Slice2.BarNumber, Slice2.BeatNumberFromBarStart);
            }
        }
        /// <summary>
        /// The number of notes that match
        /// </summary>
        public int Matches
        {
            get
            {
                return Slice2.Notes.Count;
            }
        }
        /// <summary>
        /// The number of notes that don't match
        /// </summary>
        public int Differences
        {
            get
            {
                var dif = 0;
                var deltaPitch = Slice1.Notes[0].Pitch - Slice2.Notes[0].Pitch;
                for (var i=0; i < Slice1.Notes.Count; i++)
                {
                    if (Slice1.Notes[i].Pitch - Slice2.Notes[i].Pitch != deltaPitch) dif++;
                }
                return dif;
            }
        }
        /// <summary>
        /// False if the matches have exactly the same pitch
        /// </summary>
        public bool AreTransposed { 
            get
            {
                return Slice1.Notes[0].Pitch == Slice2.Notes[0].Pitch;
            }
        }

        public bool NotesAreTheSame
        {
            get
            {
                return Slice1.Notes[0].Pitch % 12 == Slice2.Notes[0].Pitch % 12;
            }
        }


    }
}
