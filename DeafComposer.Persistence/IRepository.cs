using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    public interface IRepository
    {
        #region Songs
        Task<List<Song>> GetSongsAsync(int page, int pageSize, string startWith = null, long? bandId = null);
        Task<int> GetNumberOfSongsAsync(string startWith = null, long? bandId = null);
        Task<Song> GetSongByIdAsync(long songId);
        Task<Song> GetSongByNameAndBandAsync(string songName, string bandName);
        Task<Song> UpdateSongAsync(Song song);
        Task<Song> AddSongAsync(Song song);
        Task DeleteSongAsync(long songId);
        #endregion
        #region Style
        Task<List<Style>> GetStylesAsync(int pageNo, int pageSize, string startWith);
        Task<int> GetNumberOfStylesAsync(int pageNo, int pageSize, string startWith);
        Task<Style> GetStyleByIdAsync(long styleId);
        Task<Style> GetStyleByNameAsync(string name);
        Task<Style> AddStyleAsync(Style style);
        Task<Style> UpdateStyleAsync(Style style);
        Task DeleteStyleAsync(long styleId);
        #endregion
        #region Bands
        Task<List<Band>> GetBandsAsync(int pageNo, int pageSize, string startWith = null, long? styleId = null);
        Task<int> GetNumberOfBandsAsync(string startWith = null, long? styleId = null);
        Task<Band> GetBandByIdAsync(long bandId);
        Task<Band> GetBandByNameAsync(string name);
        Task<Band> AddBandAsync(Band band);
        Task<Band> UpdateBandAsync(Band band);
        Task DeleteBandAsync(long bandId);
        #endregion
        #region Time Signatures
        Task<TimeSignature> GetTimeSignatureAsync(TimeSignature timeSignature);
        #endregion
        #region Artifacts
        Task<Artifact> GetArtifactByIdAsync(long ArtifactId);
        Artifact GetArtifactByStringAndType(string ArtifactString, ArtifactType ArtifactType);
        Artifact AddArtifact(Artifact Artifact);
        Task<Instance> GetInstanceByIdAsync(long InstanceId);
        Task<List<Instance>> GetInstancesForSongVersionIdAndArtifactIdAsync(long songSimplificationId, long ArtifactId);
        Instance AddInstance(Instance oc);
        Task<List<Instance>> GetArtifactInstancesOfSongSimplificationAsync(long songSimplificationId);
        bool AreInstancesForSongSimplificationAlreadyProcessed(long songSimplificationId);
        #endregion
        #region SongSimplifications
        Task<SongSimplification> AddSongSimplificationAsync(SongSimplification simpl);
        Task UpdateSongSimplificationAsync(SongSimplification simpl);
        Task<SongSimplification> GetSongSimplificationAsync(long songId, int version);
        Task<List<Note>> GetSongSimplificationNotesAsync(long songSimplificationId);
        #endregion

    }
}
