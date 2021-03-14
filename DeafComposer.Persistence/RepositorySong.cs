using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace DeafComposer.Persistence
{
    partial class Repository
    {
        public async Task<Song> GetSongByIdAsync(long songId, int? simplificationVersion = null)
        {
            var retObj = await dbContext.Songs
                .Include(x => x.Style)
                .Include(x => x.Band)
                .Include(c => c.TempoChanges)
                .Include(z => z.SongStats)
                .Include(f => f.SongStats.TimeSignature)
                .Include(y => y.Bars)
                .ThenInclude(y => y.TimeSignature)
                .Include(k => k.Bars)
                .ThenInclude(k => k.KeySignature)
                .FirstOrDefaultAsync(x => x.Id == songId);

            retObj.Bars = retObj.Bars.OrderBy(x => x.BarNumber).ToList();
            retObj.TempoChanges = retObj.TempoChanges.OrderBy(x => x.TicksSinceBeginningOfSong).ToList();

            retObj.SongSimplifications = new List<SongSimplification>();
            var i = 0;
            if (simplificationVersion != null)
            {
                retObj.SongSimplifications.Add(await GetSongSimplificationBySongIdAndVersionAsync(songId, (int)simplificationVersion));
            }
            else
            {
                while (true)
                {
                    // we are not interested in simplification 0 (we add it as a null, so  simplification 1 is in SongSimplifications[1])
                    if (i == 0)
                        retObj.SongSimplifications.Add(null);
                    else
                    {
                        var simpl = await GetSongSimplificationBySongIdAndVersionAsync(songId, i);
                        if (simpl == null) break;
                        retObj.SongSimplifications.Add(simpl);
                    }
                    i++;
                }
            }

            return retObj;
        }
        public async Task<Song> GetSongByNameAndBandAsync(string songName, string bandName)
        {
            return await dbContext.Songs.FirstOrDefaultAsync(x => x.Name.ToLower() == songName.ToLower() & x.Band.Name.ToLower() == bandName.ToLower());
        }

        public async Task<List<Song>> GetSongsAsync(
            int pageNo = 0,
            int pageSize = 10,
            string contains = null,
            long? styleId = null,
            long? bandId = null)
        {
            IQueryable<Song> source = dbContext.Songs;
            source = source.Where(GetWhereStringForGetSongs(contains, styleId, bandId));

            return await source
                      .OrderBy(s => s.Name)
                      .Include(s => s.Band)
                      .Include(z => z.Style)
                      .Skip((pageNo) * pageSize)
                      .Take(pageSize)
                      .Select(y => new Song
                      {
                          Id = y.Id,
                          Name = y.Name,
                          Band = y.Band,
                          Style = y.Style
                      })
                      .ToListAsync();
        }

        private  string GetWhereStringForGetSongs(string contains = null,long? styleId = null,long? bandId = null)
        {
            string dynamicQuery = "1==1";
            if (bandId != null && bandId > 0) dynamicQuery += $" && BandId == {bandId}";
            if (styleId != null && styleId > 0) dynamicQuery += $" && StyleId == {styleId}";
            if (!string.IsNullOrEmpty(contains)) dynamicQuery += $" && Name.Contains(\"{contains}\")";
            return dynamicQuery;
        }


        public async Task<int> GetNumberOfSongsAsync(
            string contains = null,
            long? styleId=null,
            long? bandId = null)
        {
            IQueryable<Song> source = dbContext.Songs;
            source = source.Where(GetWhereStringForGetSongs(contains, styleId, bandId));

            return await source.CountAsync();
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
           // dbContext.SongAnalysis.Add(new SongAnalysis { SongId = song.Id, HavePatternsBeenFound = false });
           // await dbContext.SaveChangesAsync();
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
