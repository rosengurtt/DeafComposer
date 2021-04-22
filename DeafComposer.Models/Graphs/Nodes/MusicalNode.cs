namespace DeafComposer.Models.Graphs.Nodes
{
    /// <summary>
    /// A root class for RythmPattern, RythmBeat, RythmStep, PitchPattern, PitchBeat, etc.
    /// Defines a FollowedBy property to another MusicalNode that represents an edge to another node of the same type
    /// (a RythmStep is "followed by" a RythmStep, a RythmBeat is "followed by" a RythmBeat)
    /// 
    /// The other fields give information of the song, voice, time, etc where the transition occurs
    /// </summary>
    public abstract class MusicalNode
    {
        public MusicalNode FollowedBy { get; set; }
        public long Tick { get; set; }
        public int Voice { get; set; }

        public long SongSimplification { get; set; }
        public long SongId { get; set; }
    }
}
