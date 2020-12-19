using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// Represents a music note played at some point in a song
    /// </summary>
    public class Note
    {
        public Note()
        {
            PitchBending = new List<PitchBendItem>();
        }
        public long Id { get; set; }

        [NotMapped]
        [NonSerialized()]
        public Guid TempId; 

        public byte Pitch { get; set; }
        public byte Volume { get; set; }
        public long StartSinceBeginningOfSongInTicks { get; set; }
        public long EndSinceBeginningOfSongInTicks { get; set; }
        public bool IsPercussion { get; set; }

        /// <summary>
        /// Basically, the same as a track
        /// If there were no cases of 2 tracks with the same intrument, we would not need it
        /// But because we may have 2 pianos for example, if we don't keep separated we
        /// loose important information
        /// </summary>
        public byte Voice { get; set; }


        public byte Instrument { get; set; }
        public List<PitchBendItem> PitchBending { get; set; }

        public int DurationInTicks
        {
            get
            {
                return (int)(EndSinceBeginningOfSongInTicks - StartSinceBeginningOfSongInTicks);
            }
        }

        public Note Clone()
        {
            return new Note
            {
                Id = this.Id,
                TempId = this.TempId,
                EndSinceBeginningOfSongInTicks = this.EndSinceBeginningOfSongInTicks,
                StartSinceBeginningOfSongInTicks = this.StartSinceBeginningOfSongInTicks,
                Pitch = this.Pitch,
                Volume = this.Volume,
                Instrument = this.Instrument,
                PitchBending = PitchBending.Select(s => s.Clone()).ToList(),
                IsPercussion = this.IsPercussion,
                Voice = this.Voice
            };
        }
        public bool IsEqual(object n)
        {
            //Check for null and compare run-time types.
            if ((n == null) || !this.GetType().Equals(n.GetType()))
            {
                return false;
            }
            else
            {
                Note noty = (Note)n;
                if (noty.Pitch == Pitch &&
                    noty.StartSinceBeginningOfSongInTicks == StartSinceBeginningOfSongInTicks &&
                    noty.EndSinceBeginningOfSongInTicks == EndSinceBeginningOfSongInTicks &&
                    noty.Instrument == Instrument)
                    return true;
                return false;
            }
        }
    }
}
