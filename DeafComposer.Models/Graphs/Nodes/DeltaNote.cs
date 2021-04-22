namespace DeafComposer.Models.Graphs.Nodes
{
    /// <summary>
    /// Represents a combination of a pitch step and a rythm step
    /// </summary>
    public class DeltaNote
    {
        public int DeltaPitch { get; set; }
        public int DeltaTicks { get; set; }
        public VolumeRythm Volume { get; set; }
    }
}
