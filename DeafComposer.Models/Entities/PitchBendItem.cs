using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// Represents a Midi pitch event
    /// The midi bending events apply to all notes in a channel. When we process a midi file we
    /// add PitchBendItem objects to each note that was playing when the bending occured
    /// </summary>
    public class PitchBendItem
    {
        public long Id { get; set; }
        public long TicksSinceBeginningOfSong { get; set; }
        public ushort Pitch { get; set; }

        public long NoteId { get; set; }
        public Note Note { get; set; }

        public PitchBendItem Clone()
        {
            return new PitchBendItem
            {
                TicksSinceBeginningOfSong = this.TicksSinceBeginningOfSong,
                Pitch = this.Pitch
            };
        }
    }
}
