using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    public partial class Repository : IRepository
    {
        public async Task<Artifact> GetArtifactByIdAsync(long patternId)
        {
            return await dbContext.Artifacts.FindAsync(patternId);
        }


        /// <summary>
        /// This one uses ado.net because EF is a peace of sheet. Thank you Machosoft for your
        /// crapp software
        /// </summary>
        /// <param name="patternString"></param>
        /// <param name="patternType"></param>
        /// <returns></returns>
        public Artifact GetArtifactByStringAndType(string patternString, ArtifactType patternType)
        {
            using (var sqlCnn = new SqlConnection(ConnectionString))
            {
                sqlCnn.Open();
                using (var sqlCmd = new SqlCommand(@"Select Id
                                                    from Artifacts 
                                                    where AsString = @AsString and 
                                                    ArtifactTypeId = @TypeId", sqlCnn))
                {
                    using (var adapter = new SqlDataAdapter() { SelectCommand = sqlCmd })
                    {
                        var paramArtifact = new SqlParameter()
                        {
                            ParameterName = "@AsString",
                            DbType = DbType.String,
                            Value = patternString
                        };
                        sqlCmd.Parameters.Add(paramArtifact);
                        var paramType = new SqlParameter()
                        {
                            ParameterName = "@TypeId",
                            DbType = DbType.Int32,
                            Value = (int)patternType
                        };
                        sqlCmd.Parameters.Add(paramType);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                        {
                            if (ds.Tables[0].Rows.Count == 0) return null;
                            else
                            {
                                var pat = new Artifact()
                                {
                                    AsString = patternString,
                                    ArtifactTypeId = patternType,
                                    Id = (long)ds.Tables[0].Rows[0].ItemArray[0]
                                };
                                return pat;
                            }
                        }
                    }
                }
            }
            return null;
        }
        public Artifact AddArtifact(Artifact pattern)
        {
            using (var sqlCnn = new SqlConnection(ConnectionString))
            {
                sqlCnn.Open();
                using (var sqlCmd = new SqlCommand(@"insert into Artifacts(AsString, ArtifactTypeId)
                                              values (@AsString, @ArtifactTypeId);
                                              SELECT SCOPE_IDENTITY();", sqlCnn))
                {
                    var AsString = new SqlParameter()
                    {
                        ParameterName = "@AsString",
                        DbType = DbType.String,
                        Value = pattern.AsString
                    };
                    sqlCmd.Parameters.Add(AsString);
                    var paramArtifactTypeId = new SqlParameter()
                    {
                        ParameterName = "@ArtifactTypeId",
                        DbType = DbType.Int32,
                        Value = pattern.ArtifactTypeId
                    };
                    sqlCmd.Parameters.Add(paramArtifactTypeId);
                    pattern.Id = Convert.ToInt64(sqlCmd.ExecuteScalar());
                }
            }
            return pattern;
        }


        public async Task<Instance> GetInstanceByIdAsync(long ocId)
        {
            return await dbContext.Instances.FindAsync(ocId);
        }

        public bool AreInstancesForSongSimplificationAlreadyProcessed(long songSimplificationId)
        {
            using (var sqlCnn = new SqlConnection(ConnectionString))
            {
                sqlCnn.Open();
                using (var sqlCmd = new SqlCommand(@"Select count(*) from Instances
                                              where SongSimplificationId=@id", sqlCnn))
                {
                    var adapter = new SqlDataAdapter() { SelectCommand = sqlCmd };
                    var paramId = new SqlParameter()
                    {
                        ParameterName = "@id",
                        DbType = DbType.Int64,
                        Value = songSimplificationId
                    };
                    sqlCmd.Parameters.Add(paramId);
                    var count = Convert.ToInt32(sqlCmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }
        public async Task<List<Instance>> GetArtifactInstancesOfSongSimplificationAsync(long songSimplificationId)
        {
            return await dbContext.Instances
                .Where(x => x.SongSimplificationId == songSimplificationId)
                .Include(y => y.Artifact).ToListAsync();
        }

        public async Task<List<Instance>> GetInstancesForSongVersionIdAndArtifactIdAsync(
            long SongSimplificationId, long patternId)
        {

            var occurs = await dbContext.Instances.Join(
                dbContext.SongSimplifications,
                occurrence => occurrence.SongSimplificationId,
                songVersion => songVersion.Id,
                (occurrence, songSimplification) => new Instance
                {
                    Id = occurrence.Id,
                    SongSimplificationId = songSimplification.Id,
                    ArtifactId = occurrence.ArtifactId,
                    Artifact = occurrence.Artifact
                }
                )
                .Where(a => a.SongSimplificationId == SongSimplificationId & a.ArtifactId == patternId).ToListAsync();
            foreach (var oc in occurs)
            {
                oc.Notes = await dbContext.Notes.Join(
                    dbContext.InstanceNotes.Where(occur => occur.InstanceId == oc.Id),
                    note => note.Id,
                    occurrenceNote => occurrenceNote.NoteId,
                    (note, occurrenceNote) => note).ToListAsync();
            }
            return occurs;
        }

        public Instance AddInstance(Instance oc)
        {
            using (var sqlCnn = new SqlConnection(ConnectionString))
            {

                sqlCnn.Open();
                using (var sqlCmd1 = new SqlCommand(@"insert into Instances(SongSimplificationId,ArtifactId)
                                              values (@SongSimplificationId, @ArtifactId);
                                              SELECT SCOPE_IDENTITY();", sqlCnn))
                {
                    var paramSongSimplificationId = new SqlParameter()
                    {
                        ParameterName = "@SongSimplificationId",
                        DbType = DbType.Int64,
                        Value = oc.SongSimplificationId
                    };
                    sqlCmd1.Parameters.Add(paramSongSimplificationId);
                    var paramArtifactId = new SqlParameter()
                    {
                        ParameterName = "@ArtifactId",
                        DbType = DbType.Int64,
                        Value = oc.Artifact.Id
                    };
                    sqlCmd1.Parameters.Add(paramArtifactId);
                    oc.Id = Convert.ToInt64(sqlCmd1.ExecuteScalar());
                }

                foreach (var note in oc.Notes)
                {
                    using (var sqlCmd2 = new SqlCommand(@"insert into InstanceNotes(InstanceId,NoteId)
                                              values (@InstanceId, @NoteId)", sqlCnn))
                    {
                        var paramInstanceId = new SqlParameter()
                        {
                            ParameterName = "@InstanceId",
                            DbType = DbType.Int64,
                            Value = oc.Id
                        };
                        sqlCmd2.Parameters.Add(paramInstanceId);
                        var paramNoteId = new SqlParameter()
                        {
                            ParameterName = "@NoteId",
                            DbType = DbType.Int64,
                            Value = note.Id
                        };
                        sqlCmd2.Parameters.Add(paramNoteId);
                        sqlCmd2.ExecuteNonQuery();
                    }
                }
                return oc;
            }
        }
    }
}
