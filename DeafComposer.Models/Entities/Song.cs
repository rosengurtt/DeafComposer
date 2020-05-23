
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeafComposer.Models.Entities
{
    public class Song
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string MidiBase64Encoded { get; set; }
        public long BandId { get; set; }
        public Band Band { get; set; }
        public long StyleId { get; set; }
        public Style Style { get; set; }
        public SongStats SongStats { get; set; }
        [NotMapped]
        public List<SongSimplification> SongSimplifications { get; set; }
        public List<Bar> Bars { get; set; }
        public List<TempoChange> TempoChanges { get; set; }
    }
}
