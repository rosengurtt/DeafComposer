namespace DeafComposer.Models.Graphs.Nodes
{
    /// <summary>
    /// Represents a sequence of notes.
    /// </summary>
    public class MelodyPattern
    {
        public string Name { get; set; }
        public int QtyBeats { get; set; }

        /// <summary>
        /// Total of notes played
        /// </summary>
        public int QtyNotes { get; set; }

        /// <summary>
        /// True if all time intervals are equal
        /// </summary>
        public bool IsUniform { get; set; }

        /// <summary>
        /// True if there are notes with accents
        /// </summary>
        public bool HasAccents { get; set; }
        /// <summary>
        /// The difference between the highest pitch and the lowest pitch
        /// </summary>
        public int Range { get; set; }

        /// <summary>
        /// The difference between the last pitch in the sequence and the pitch of the note before the pitch sequence
        /// A positive value means the pitch went up, negative if it went down
        /// </summary>
        public int TotalDeltaPitch { get; set; }

        /// <summary>
        /// If pitch changes always in the same direction
        /// </summary>
        public bool IsMonotone { get; set; }

        /// <summary>
        /// The largest delta pitch between 2 consecutive notes
        /// </summary>
        public int LargestDeltaPitch { get; set; }

        public PitchStep NextPitchStep { get; set; }
        public RythmPattern NextRythmBeat { get; set; }
    }
}
