namespace DeafComposer.Models.Entities
{

    /// <summary>
    /// Represents a bar in a song (in Spanish "un compas")
    /// </summary>
    public class Bar
    {

        public long Id { get; set; }
        public long BarNumber { get; set; }
        public long TicksFromBeginningOfSong { get; set; }

        public long TimeSignatureId { get; set; }
        public TimeSignature TimeSignature { get; set; }

        // A positive number from 1 to 7 means number of sharps
        // A negative number from 1 to 7 means flats
        public int KeySignature { get; set; }

        /// <summary>
        /// This flag is used when quantizing the duration of notes
        /// We aproximate the durations to whole quarters, quavers, etc.
        /// and we don't want to aproximate a triplet duration by a quaver for example
        /// </summary>
        public bool HasTriplets { get; set; }


        public long TempoInMicrosecondsPerQuarterNote { get; set; }


    }
}
