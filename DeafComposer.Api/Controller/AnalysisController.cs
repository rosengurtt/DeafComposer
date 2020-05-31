using DeafComposer.Analysis.Artifacts;
using DeafComposer.Api.ErrorHandling;
using DeafComposer.Api.Helpers;
using DeafComposer.Midi;
using DeafComposer.Models.Entities;
using DeafComposer.Models.Enums;
using DeafComposer.Persistence;
using Melanchall.DryWetMidi.Core;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController: ControllerBase
    {
        private IRepository Repository;
        public AnalysisController(IRepository Repository)
        {
            this.Repository = Repository;
        }
        [HttpGet]
        [Route("pitchPattern")]
        public async Task<ActionResult> ProcessSong(int songId, int simplificationVersion)
        {
            var songita = await Repository.GetSongByIdAsync(songId);
            var simpl = await Repository.GetSongSimplificationBySongIdAndVersionAsync(songId, simplificationVersion);

            ProcessPatterns(songita, simpl, ArtifactType.PitchPattern);
            ProcessPatterns(songita, simpl, ArtifactType.RythmPattern);
            ProcessPatterns(songita, simpl, ArtifactType.MelodyPattern);

            return Ok(new ApiOKResponse("Data processed"));
        }


        private void ProcessPatterns(Song songita, SongSimplification simpl, ArtifactType type)
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
