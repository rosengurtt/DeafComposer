using DeafComposer.Models.Entities;
using System.Collections.Generic;

namespace DeafComposer.Models.Graphs
{
    public class NoteNode
    {
        public Interval RelativePitch { get; set; }
        public int TicksFromStart { get; set; }
        public MovesToEdge Edge { get; set; }
        public List<NoteNode> PlaysWith { get; set; }
    }

}
