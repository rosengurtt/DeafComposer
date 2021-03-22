using DeafComposer.Analysis.Artifacts;
using DeafComposer.Analysis.Patterns;
using DeafComposer.Analysis.Simplification;
using DeafComposer.Api.ErrorHandling;
using DeafComposer.Api.Helpers;
using DeafComposer.Midi;
using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using DeafComposer.Persistence;
using Melanchall.DryWetMidi.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Neo4j.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DeafComposer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController: ControllerBase
    {
        private IRepository Repository;
        private IDriver Driver;
        public AnalysisController(IRepository Repository, IDriver Driver)
        {
            this.Repository = Repository;
            this.Driver = Driver;
        }


        [HttpGet]
        [Route("patterns")]
        public async Task<ActionResult> ProcessPatterns(int songId)
        {
            var songita = await Repository.GetSongByIdAsync(songId, 1);
           
            await MelodyPattern.GetPatterns(songita, Driver);
            return null;
        }

        [HttpGet]
        [Route("pattern")]
        public async Task<ActionResult> ProcessPatternsOld(int songId, int simplificationVersion)
        {
            if (!await Repository.HavePatternsOfSongBeenFound(songId))
            {
                var songita = await Repository.GetSongByIdAsync(songId, simplificationVersion);
                var simpl = await Repository.GetSongSimplificationBySongIdAndVersionAsync(songId, simplificationVersion);

                ProcessPatternsOfType(songita, simpl, ArtifactType.PitchPattern);
                ProcessPatternsOfType(songita, simpl, ArtifactType.RythmPattern);
                ProcessPatternsOfType(songita, simpl, ArtifactType.MelodyPattern);
                await Repository.UpdateAnalysisStatusOfSong(songId, true);

                return Ok(new ApiOKResponse("Song processed"));
            }
            else
                return Ok(new ApiOKResponse("Song was already processed."));
        }

        /// <summary>
        /// Simplification 1 converts bendings to discrete notes
        /// Only apply to song that has bendings, and some of those bendings are large enough
        /// to reach a note that is at least a semitone higher or lower than the original note
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("simplification1")]
        public async Task<ActionResult> MakeSimplification1()
        {
            int totalSongs = await Repository.GetNumberOfSongsAsync();
            int currentPage=0;
            int pageSize = 10;
            while (currentPage < totalSongs/pageSize + 1)
            {
                currentPage++;
                var songsToProcess = await Repository.GetSongsAsync(currentPage, pageSize);
                foreach(var song in songsToProcess)
                {
                    // Check that song has bendings
                    var songStats = (await Repository.GetSongByIdAsync(song.Id)).SongStats;
                    if (songStats.TotalPitchBendEvents == 0) continue;

                    // Check that song has been processed already
                    var simpl = await Repository.GetSongSimplificationBySongIdAndVersionAsync(song.Id, 1);
                    if (simpl != null) continue;

                    // Process song
                    var simpl0 = await Repository.GetSongSimplificationBySongIdAndVersionAsync(song.Id, 0, true);
                    var notesWithoutBending = SimplificationUtilities.RemoveBendings(simpl0.Notes);
                    if (notesWithoutBending.Count > simpl0.Notes.Count)
                    {
                        var simpl1 = new SongSimplification
                        {
                            Notes = notesWithoutBending,
                            SongId = simpl0.SongId,
                            NumberOfVoices = simpl0.NumberOfVoices,
                            SimplificationVersion = 1
                        };
                        await Repository.AddSongSimplificationAsync(simpl1);
                    }
                }
            }
            return Ok(new ApiOKResponse("All songs processed"));
        }

        private void ProcessPatternsOfType(Song songita, SongSimplification simpl, ArtifactType type)
        {
            var patterns = ArtifactUtilities.FindArtifactsInSong(songita, simpl, type);
            foreach (var pat in patterns.Keys)
            {
                var patito = Repository.GetArtifactByStringAndType(pat.AsString, type);

                if (patito == null)
                {
                    patito = Repository.AddArtifact(pat);
                }
                foreach (var inst in patterns[pat])
                {
                    inst.Artifact = patito;
                    inst.ArtifactId = patito.Id;
                    inst.SongSimplificationId = simpl.Id;
                    Repository.AddInstance(inst);
                }
            }
        }
  
    
    }
}
