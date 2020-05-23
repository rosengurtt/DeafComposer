using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DeafComposer.Models.Entities
{
    public class SongStats
    {
        public long Id { get; set; }
        public long SongId { get; set; }
        public long DurationInSeconds { get; set; }
        public bool HasMoreThanOneInstrumentPerTrack { get; set; }
        public bool HasMoreThanOneChannelPerTrack { get; set; }
        public long HighestPitch { get; set; }
        public long LowestPitch { get; set; }
        public long NumberBars { get; set; }
        public long NumberOfTicks { get; set; }
        public long TempoInMicrosecondsPerBeat { get; set; }
        public long TempoInBeatsPerMinute { get; set; }
        public long TimeSignatureId { get; set; }
        public TimeSignature TimeSignature { get; set; }
        /// <summary>
        /// C1 and C2 count as different pitches
        /// </summary>
        public long TotalDifferentPitches { get; set; }
        /// <summary>
        /// C1 and C2 count as same pitch
        /// </summary>
        public long TotalUniquePitches { get; set; }
        public long TotalTracks { get; set; }
        public long TotalTracksWithoutNotes { get; set; }
        public long TotalBassTracks { get; set; }
        public long TotalChordTracks { get; set; }
        public long TotalMelodicTracks { get; set; }
        public long TotalPercussionTracks { get; set; }
        public long TotalInstruments { get; set; }
        public string InstrumentsAsString { get; set; }
        [NotMapped]
        public List<int> Instruments
        {
            get
            {
                return InstrumentsAsString.Split(",").Select(int.Parse).ToList();
            }
        }
        public long TotalPercussionInstruments { get; set; }
        public long TotalChannels { get; set; }
        public long TotalTempoChanges { get; set; }
        public long TotalEvents { get; set; }
        public long TotalNoteEvents { get; set; }
        public long TotalPitchBendEvents { get; set; }
        public long TotalProgramChangeEvents { get; set; }
        public long TotalControlChangeEvents { get; set; }
        public long TotalSustainPedalEvents { get; set; }
        public long TotalChannelIndependentEvents { get; set; }
    }
}
