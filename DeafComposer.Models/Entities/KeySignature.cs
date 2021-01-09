using DeafComposer.Models.Enums;

namespace DeafComposer.Models.Entities
{
    public class KeySignature
    {
        public long Id { get; set; }
        // A positive number from 1 to 7 means number of sharps
        // A negative number from 1 to 7 means flats
        public int key { get; set; }
        public ScaleType scale { get; set; }
    }
}
