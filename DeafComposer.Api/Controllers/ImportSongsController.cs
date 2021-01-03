using DeafComposer.Api.ErrorHandling;
using DeafComposer.Api.Helpers;
using DeafComposer.Midi;
using DeafComposer.Models.Entities;
using DeafComposer.Persistence;
using Melanchall.DryWetMidi.Core;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportSongsController : ControllerBase
    {
        private IRepository Repository;
        public ImportSongsController(IRepository Repository)
        {
            this.Repository = Repository;
        }
        [HttpGet]
        public async Task<ActionResult> ImportMidis(string musicFolderPath = @"C:\music\testVoicSplit\midi")
        {
            try
            {
                var styles = Directory.GetDirectories(musicFolderPath);
                foreach (var stylePath in styles)
                {
                    var styleName = FileSystemUtils.GetLastDirectoryName(stylePath);
                    Style style = await Repository.GetStyleByNameAsync(styleName);
                    if (style == null)
                    {
                        style = new Style() { Name = styleName };
                        await Repository.AddStyleAsync(style);
                    }
                    var bandsPaths = Directory.GetDirectories(stylePath);
                    foreach (var bandPath in bandsPaths)
                    {
                        var bandName = FileSystemUtils.GetLastDirectoryName(bandPath);
                        Band band = await Repository.GetBandByNameAsync(bandName);
                        if (band == null)
                        {
                            band = new Band()
                            {
                                Name = bandName,
                                Style = style
                            };
                            await Repository.AddBandAsync(band);
                        }
                        var songsPaths = Directory.GetFiles(bandPath);
                        foreach (var songPath in songsPaths)
                        {
                            var songita = await Repository.GetSongByNameAndBandAsync(Path.GetFileName(songPath), bandName);
                            if (songita == null)
                            {
                                var song = await ProcesameLaSong(songPath, band, style);
                            }
                        }
                    }
                }
            }
            catch (Exception soreton)
            {
                Log.Error(soreton, $"Exception raised when running ImportMidis");
                return new StatusCodeResult(500);
            }
            return Ok(new ApiOKResponse("All files processed"));
        }
        private async Task<Song> ProcesameLaSong(string songPath, Band band, Style style)
        {
            try
            {
                if (!songPath.ToLower().EndsWith(".mid")) return null;
                try
                {
                    var lelo = MidiFile.Read(songPath, null);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Song {songPath} esta podrida");
                    return null;
                }

                var midiBase64encoded = FileSystemUtils.GetBase64encodedFile(songPath);
                midiBase64encoded = MidiUtilities.NormalizeTicksPerQuarterNote(midiBase64encoded);

                Song song = new Song()
                {
                    Name = Path.GetFileName(songPath),
                    Band = band,
                    Style = style,
                    MidiBase64Encoded = midiBase64encoded,
                };
                song.SongStats = MidiUtilities.GetSongStats(midiBase64encoded);
                // The following line is  used to get the id of the time signature. If we don't
                // provide the id when saving the song, it will create a duplicated time signature
                // in the TimeSignatures table
                var timeSig = await Repository.GetTimeSignatureAsync(song.SongStats.TimeSignature);
                song.SongStats.TimeSignatureId = timeSig.Id;
                song.SongStats.TimeSignature = timeSig;


                var simplificationZero = MidiUtilities.GetSimplificationZeroOfSong(midiBase64encoded);
                song.Bars = MidiUtilities.GetBarsOfSong(midiBase64encoded, simplificationZero);
                foreach(var bar in song.Bars)
                {
                    var timeSigBar = await Repository.GetTimeSignatureAsync(bar.TimeSignature);
                    bar.TimeSignatureId = timeSigBar.Id;
                    bar.TimeSignature = timeSig;
                }
                song.TempoChanges = MidiUtilities.GetTempoChanges(midiBase64encoded);
                song.SongStats.NumberBars = song.Bars.Count();
                song = await Repository.AddSongAsync(song);
                simplificationZero.SongId = song.Id;
                simplificationZero = await Repository.AddSongSimplificationAsync(simplificationZero);
                song.SongSimplifications = new List<SongSimplification>();
                song.SongSimplifications.Add(simplificationZero);

                var simplification1 = MidiUtilities.GetSimplification1ofSong(song);
                simplification1=await Repository.AddSongSimplificationAsync(simplification1);
                song.SongSimplifications.Add(simplification1);

                return song;
            }
            catch (Exception sorete)
            {
                return null;
            }
        }

    }
    }
