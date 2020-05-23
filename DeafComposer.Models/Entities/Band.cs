using System;
using System.Collections.Generic;
using System.Text;

namespace DeafComposer.Models.Entities
{
    public class Band
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public long StyleId { get; set; }
        public Style Style { get; set; }
    }
}
