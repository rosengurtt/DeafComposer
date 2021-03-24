using DeafComposer.Models.Entities;

namespace DeafComposer.Models.Graphs
{
    public class MovesToEdge
    {
        public long DeltaTicks { get; set; }
        public Interval DeltaPitch { get; set; }
        public NoteNode NextNote { get; set; }
    }
}
