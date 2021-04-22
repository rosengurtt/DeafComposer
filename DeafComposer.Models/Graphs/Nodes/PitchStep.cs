using DeafComposer.Models.Entities;

namespace DeafComposer.Models.Graphs.Nodes
{
    /// <summary>
    /// Represents the interval in semitones from the previous note to the current note in a sequence of pitches
    /// 
    /// A positive value means the note went up, a negative value it went down
    /// </summary>
    public class PitchStep: MusicalNode
    {
        public Interval DeltaPitch { get; set; }
        public PitchStep NextPitchStep { get; set; }
    }
}
