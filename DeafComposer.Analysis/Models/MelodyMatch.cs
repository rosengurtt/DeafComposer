namespace DeafComposer.Analysis.Models
{
    /// <summary>
    /// Represents a couple of passages in a song that are identical or similar
    /// When they are identical (except for a transposicion) Differences=0. Otherwise, for each note that doesn't match, Differences increase by 1
    /// AreTransposed gives information about the notes having the same pitch or a transposed pitch
    /// </summary>
    public class MelodyMatch
    {
        public NotesSlice Slice1 { get; set; }
        public NotesSlice Slice2 { get; set; }
        /// <summary>
        /// The number of notes that match
        /// </summary>
        public int Matches { get; set; }
        /// <summary>
        /// The number of notes that don't match
        /// </summary>
        public int Differences { get; set; }
        /// <summary>
        /// True if the matches have the same pitch
        /// </summary>
        public bool AreTransposed { get; set; }

        /// <summary>
        /// The tick in relation to the start of a slice, in which the matching notes start
        /// </summary>
        public long StartTick { get; set; }

        /// <summary>
        /// The tick in relation to the start of a slice, in which the matching notes end
        /// </summary>
        public long EndTick { get; set; }

    }
}
