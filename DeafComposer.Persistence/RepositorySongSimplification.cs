using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    partial class Repository
    {
        public async Task<SongSimplification> GetSongSimplificationAsync(long songId, int version)
        {
            var songSimpl = await dbContext.SongSimplifications
                .Where(s => s.SongId == songId && s.SimplificationVersion == version)
                .FirstOrDefaultAsync();
            try
            {

                songSimpl.Notes = await (from ss in dbContext.SongSimplifications
                                         join ssn in dbContext.SongSimplificationNotes on ss.Id equals ssn.SongSimplificationId
                                         join n in dbContext.Notes on ssn.NoteId equals n.Id
                                         where ss.SongId == songId && ss.SimplificationVersion == version
                                         select n).ToListAsync();
            }
            catch (Exception dfdsfas)
            {

            }
            return songSimpl;
        }

        public async Task UpdateSongSimplificationAsync(SongSimplification simpl)
        {
            dbContext.SongSimplifications.Update(simpl);
            await dbContext.SaveChangesAsync();
        }

        public async Task<SongSimplification> AddSongSimplificationAsync(SongSimplification simpl)
        {
            try
            {
                dbContext.SongSimplifications.Add(simpl);
                await dbContext.SaveChangesAsync();
                var newNotes = simpl.Notes.Where(n => n.Id == 0).ToList();
                foreach (var n in newNotes)
                {
                    dbContext.Notes.Add(n);
                }
                await dbContext.SaveChangesAsync();
                foreach (var n in simpl.Notes)
                {
                    dbContext.SongSimplificationNotes.Add(new SongSimplificationNote
                    {
                        NoteId = n.Id,
                        SongSimplificationId = simpl.Id
                    });
                }
                await dbContext.SaveChangesAsync();
            }
            catch (Exception fdsafasdfa)
            {

            }
            return simpl;

        }

        public async Task<List<Note>> GetSongSimplificationNotesAsync(long songSimplificationId)
        {
            return await (from ss in dbContext.SongSimplifications
                          join ssn in dbContext.SongSimplificationNotes on ss.Id equals ssn.SongSimplificationId
                          join n in dbContext.Notes on ssn.NoteId equals n.Id
                          where ss.Id == songSimplificationId
                          select n).ToListAsync();
        }
    }
}
