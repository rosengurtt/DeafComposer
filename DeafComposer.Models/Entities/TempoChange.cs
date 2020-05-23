using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Models.Entities
{
    public class TempoChange
    {
        public long Id { get; set; }

        public long SongId { get; set; }

        /// <summary>
        /// This is the standard way of handling tempos in Midi files
        /// </summary>
        public long MicrosecondsPerQuarterNote { get; set; }

        /// <summary>
        /// Place in the song where the tempo changes
        /// </summary>
        public long TicksSinceBeginningOfSong { get; set; }

        /// <summary>
        /// This is the standard way of handling tempos in music
        /// </summary>
        public long TempoAsBeatsPerQuarterNote
        {
            get
            {
                return 120 * 500000 / MicrosecondsPerQuarterNote;
            }
        }
    }
}
