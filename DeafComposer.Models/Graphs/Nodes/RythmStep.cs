namespace DeafComposer.Models.Graphs.Nodes
{
    /// <summary>
    /// Node that represents a note in a rythm pattern
    /// </summary>
    public class RythmStep: MusicalNode
    {
        /// <summary>
        /// The number of ticks from the previous note
        /// </summary>
        public int DeltaTicks { get; set; }
        public VolumeRythm Volume { get; set; }
        public RythmStep NextRythmStep { get; set; }
    }

    public enum VolumeRythm
    {
        mute = 0,
        normal = 1,
        accented = 2
    }
}
