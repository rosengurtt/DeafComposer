using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    partial class Repository
    {
        public async Task<Song> GetSongByIdAsync(long songId)
        {
            return await dbContext.Songs.Include(x => x.Style)
                .Include(x => x.Band)
                .Include(z => z.SongStats)
                .FirstOrDefaultAsync(x => x.Id == songId);
        }
        public async Task<Song> GetSongByNameAndBandAsync(string songName, string bandName)
        {
            return await dbContext.Songs.FirstOrDefaultAsync(x => x.Name == songName & x.Band.Name == bandName);
        }

        public async Task<List<Song>> GetSongsAsync(int pageNo = 1,
            int pageSize = 1000,
            string startWith = null,
            long? bandId = null)
        {
            if (bandId != null && bandId > 0)
            {
                return await dbContext.Songs
                    .Where(x => x.BandId == bandId).Skip((pageNo - 1) * pageSize)
                    .Take(pageSize).ToListAsync();
            }
            if (string.IsNullOrEmpty(startWith))
                return await dbContext.Songs.Skip((pageNo - 1) * pageSize)
                    .Take(pageSize).ToListAsync();
            else
                return await dbContext.Songs.Where(x => x.Name.StartsWith(startWith))
                    .Skip((pageNo - 1) * pageSize).Take(pageSize).ToListAsync();

        }


        public async Task<int> GetNumberOfSongsAsync(
            string startWith = null,
            long? bandId = null)
        {
            if (bandId != null && bandId > 0)
            {
                return await dbContext.Songs
                    .Where(x => x.BandId == bandId).CountAsync();
            }
            if (string.IsNullOrEmpty(startWith))
                return await dbContext.Songs.CountAsync();
            else
                return await dbContext.Songs.Where(x => x.Name.StartsWith(startWith)).CountAsync();
        }


        public async Task<Song> UpdateSongAsync(Song song)
        {
            dbContext.Entry(await dbContext.Songs.FirstOrDefaultAsync(x => x.Id == song.Id))
                .CurrentValues.SetValues(song);
            await dbContext.SaveChangesAsync();
            return song;
        }
        public async Task<Song> AddSongAsync(Song song)
        {
            dbContext.Songs.Add(song);
            await dbContext.SaveChangesAsync();
            dbContext.SongAnalysis.Add(new SongAnalysis { SongId = song.Id, HavePatternsBeenFound = false });
            await dbContext.SaveChangesAsync();
            return song;
        }


        public async Task DeleteSongAsync(long songId)
        {
            var songItem = await dbContext.Songs.Include(x => x.Style)
                .FirstOrDefaultAsync(x => x.Id == songId);
            if (songItem == null)
                throw new ApplicationException($"No song with id {songId}");

            dbContext.Songs.Remove(songItem);
            await dbContext.SaveChangesAsync();
        }
        public async Task<bool> HavePatternsOfSongBeenFound(long songId)
        {
            return await dbContext.SongAnalysis.Where(x => x.SongId == songId)
                .Select(x => x.HavePatternsBeenFound).FirstOrDefaultAsync();
        }

        public async Task UpdateAnalysisStatusOfSong(long songId, bool havePatternsBeenFound)
        {
            var sa = await dbContext.SongAnalysis.Where(x => x.SongId == songId).FirstOrDefaultAsync();
            sa.HavePatternsBeenFound = havePatternsBeenFound;
            await dbContext.SaveChangesAsync();
        }
    }
}
