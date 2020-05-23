using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeafComposer.Models.Entities
{
    public class Style
    {
        public long Id { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public ICollection<Band> Bands { get; set; }
        [JsonIgnore]
        public ICollection<Song> Songs { get; set; }
    }
}
