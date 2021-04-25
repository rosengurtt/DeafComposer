using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Analysis.Models
{
    class Occurrence
    {
        public long SongId { get; set; }
        public byte Voice { get; set; }
        public long BarNumber { get; set; }
        public long Beat { get; set; }
    }
}
