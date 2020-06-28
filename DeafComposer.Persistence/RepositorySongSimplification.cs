using DeafComposer.Models.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    partial class Repository
    {
        public async Task<SongSimplification> GetSongSimplificationBySongIdAndVersionAsync(long songId, int version, bool includeBendings=false)
        {
            var songSimpl = await dbContext.SongSimplifications
                .Where(s => s.SongId == songId && s.SimplificationVersion == version)
                .FirstOrDefaultAsync();
            try
            {
                if (songSimpl != null)
                {
                    if (includeBendings)
                    {
                        songSimpl.Notes = await (from ss in dbContext.SongSimplifications
                                                 join ssn in dbContext.SongSimplificationNotes on ss.Id equals ssn.SongSimplificationId
                                                 join n in dbContext.Notes on ssn.NoteId equals n.Id
                                                 where ss.SongId == songId && ss.SimplificationVersion == version
                                                 select n).Include(m => m.PitchBending).ToListAsync();
                    }
                    else
                    {
                        //songSimpl.Notes 
                        var sacamela = await (from ss in dbContext.SongSimplifications
                                                 join ssn in dbContext.SongSimplificationNotes on ss.Id equals ssn.SongSimplificationId
                                                 join n in dbContext.Notes on ssn.NoteId equals n.Id
                                                 where ss.SongId == songId && ss.SimplificationVersion == version
                                                 select n).ToListAsync();
                    }
                }
            }
            catch (Exception dfdsfas)
            {

            }
            return songSimpl;
        }

        private List<Note> GetNotesOfSimplification(long songSimplificationId)
        {
            var retObj = new List<Note>();
            using (var sqlCnn = new SqlConnection(ConnectionString))
            {
                sqlCnn.Open();
                using (var sqlCmd = new SqlCommand(@"Select n.Id, n.Pitch, n.Volume,
                                                    n.StartSinceBeginningOfSongInTicks,
                                                    n.EndSinceBeginningOfSongInTicks,
                                                    n.Instrument, n.IsPercussion, n.Voice
                                                    from Notes n inner join SongSimplificationNotes ssn
                                                    on n.Id =  ssn.NoteId
                                                    where ssn.SongSimplificationId = @SongSimplificationId
                                                    order by StartSinceBeginningOfSongInTicks", sqlCnn))
                {
                    using (var adapter = new SqlDataAdapter() { SelectCommand = sqlCmd })
                    {
                        var paramSSId = new SqlParameter()
                        {
                            ParameterName = "@SongSimplificationId",
                            DbType = DbType.UInt64,
                            Value = songSimplificationId
                        };
                        sqlCmd.Parameters.Add(paramSSId);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                        {
                            if (ds.Tables[0].Rows.Count == 0) return null;
                            else
                            {
                                foreach(DataRow row in ds.Tables[0].Rows)
                                {
                                    var note = new Note()
                                    {
                                        Id = (long)row.ItemArray[0],
                                        Pitch = (byte)row.ItemArray[1],
                                        Volume = (byte)row.ItemArray[2],
                                        StartSinceBeginningOfSongInTicks = (long)row.ItemArray[3],
                                        EndSinceBeginningOfSongInTicks = (long)row.ItemArray[4],
                                        Instrument = (byte)row.ItemArray[5],
                                        IsPercussion = (bool)row.ItemArray[6],
                                        Voice = (byte)row.ItemArray[7],
                                    };
                                    retObj.Add(note);
                                }
                            }
                        }
                    }
                }
            }
            return retObj;
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
