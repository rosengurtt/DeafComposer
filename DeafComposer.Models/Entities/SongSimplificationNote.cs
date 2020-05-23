using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Models.Entities
{
    /// <summary>
    /// Used for a join table that associates notes to song simplifications
    /// </summary>
    public class SongSimplificationNote
    {
        public long Id { get; set; }
        public long SongSimplificationId { get; set; }
        public long NoteId { get; set; }
    }
}
