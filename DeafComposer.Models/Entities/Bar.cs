﻿namespace DeafComposer.Models.Entities
{

    /// <summary>
    /// Represents a bar in a song (in Spanish "un compas")
    /// </summary>
    public class Bar
    {

        public long Id { get; set; }
        public long BarNumber { get; set; }
        public long TicksFromBeginningOfSong { get; set; }

        public long EndTick
        {
            get
            {
                int standardTicksPerQuarterNote = 96;
                int barLengthInTicks = TimeSignature.Numerator * (int)standardTicksPerQuarterNote;
                return TicksFromBeginningOfSong + barLengthInTicks;
            }
        }

        public long TimeSignatureId { get; set; }
        public TimeSignature TimeSignature { get; set; }

        public long KeySignatureId { get; set; }
        public KeySignature KeySignature { get; set; }




        /// <summary>
        /// This flag is used when quantizing the duration of notes
        /// We aproximate the durations to whole quarters, quavers, etc.
        /// and we don't want to aproximate a triplet duration by a quaver for example
        /// </summary>
        public bool HasTriplets { get; set; }


        public long TempoInMicrosecondsPerQuarterNote { get; set; }


    }
}
