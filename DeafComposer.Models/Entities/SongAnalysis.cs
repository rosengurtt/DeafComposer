
namespace DeafComposer.Models.Entities
{
    public class SongAnalysis
    {
        public long Id { get; set; }
        public long SongId { get; set; }
        public bool HavePatternsBeenFound { get; set; }
    }
}
