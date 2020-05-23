
using DeafComposer.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// Artifacts are music objects like chords, rythm patterns, melodic patterns, etc
    /// 
    /// We use a special notation to represent this objects independently of what they are,
    /// that is a string that uses a comma separator.
    /// 
    /// A pitch pattern representation looks like: 
    /// 0,-2,1,-2
    /// Each digit is a pitch step (the pitch difference with the previous note)
    /// 
    /// A rythm pattern representation looks like:
    ///	1,2,4,2,4,2
    ///	Each digit is a relative duration
    ///	
    /// A Melody pattern representation looks like:
    /// (0-1),(-2-1),(-1-1),(-2-1)
    /// In this case, we have pairs of digits, the first being a pitch step and the second a duration
    /// 
    /// A Chord representation looks like this:
    /// 48,51,55,60  (this a C minor chord)
    /// 
    /// A Chord progression looks like this:
    /// (48-51-55-60),(50-53-57-62) (this is a C major followed by D major
    /// 
    /// We use the ArtifactTypeId field to tell which type of artifact it is
    /// 
    /// </summary>
    public class Artifact
    {
        public long Id { get; set; }

        public string AsString { get; set; }

        [NotMapped]
        public int Length
        {
            get
            {
                return AsString.Split(",").Length;
            }
        }

        public ArtifactType ArtifactTypeId { get; set; }

        public Artifact Clone()
        {
            return new Artifact()
            {
                AsString = this.AsString,
                ArtifactTypeId = this.ArtifactTypeId
            };
        }

    }
}
