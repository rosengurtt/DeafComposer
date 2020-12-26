using DeafComposer.Models.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// An instance is an occurrence of an artifact at some point in a song. For example
    /// if the artifact is a chord, an instance is an occurrence of that chord during the
    /// song
    /// </summary>
    public class Instance
    {

        public long Id { get; set; }

        public long ArtifactId { get; set; }
        public Artifact Artifact { get; set; }

        [NotMapped]
        public List<Note> Notes { get; set; }

        public long SongSimplificationId { get; set; }
        public SongSimplification SongSimplification { get; set; }

        public Instance Clone()
        {
            var retObj = new Instance();
            retObj.Id = this.Id;
            retObj.Artifact = this.Artifact.Clone();
            retObj.SongSimplificationId = this.SongSimplificationId;
            retObj.SongSimplification = this.SongSimplification;
            retObj.Notes = this.Notes.Clone();
            return retObj;
        }
    }
}
