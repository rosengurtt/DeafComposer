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
        public async Task<ActionResult> ImportMidis(string musicFolderPath = @"C:\music\midi")
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
            foreach (var bar in song.Bars)
            {
                var timeSigBar = await Repository.GetTimeSignatureAsync(bar.TimeSignature);
                bar.TimeSignatureId = timeSigBar.Id;
                bar.TimeSignature = timeSigBar;

                var keySigBar = await Repository.GetKeySignatureAsync(bar.KeySignature);
                bar.KeySignatureId = keySigBar.Id;
                bar.KeySignature = keySigBar;
            }
            song.TempoChanges = MidiUtilities.GetTempoChanges(midiBase64encoded);
            song.SongStats.NumberBars = song.Bars.Count();
            song = await Repository.AddSongAsync(song);
            simplificationZero.SongId = song.Id;
            simplificationZero = await Repository.AddSongSimplificationAsync(simplificationZero);
            song.SongSimplifications = new List<SongSimplification>();
            song.SongSimplifications.Add(simplificationZero);

            var simplification1 = MidiUtilities.GetSimplification1ofSong(song);
            simplification1 = await Repository.AddSongSimplificationAsync(simplification1);
            song.SongSimplifications.Add(simplification1);

            return song;
        }

        [HttpGet("compare")]
        public async Task<ActionResult> Compare(string songName, string bandName, string styleName)
        {
            Style style = await Repository.GetStyleByNameAsync(styleName);
            Band band = await Repository.GetBandByNameAsync(bandName);
            var songPath = $"C:\\music\\test\\deepPurple\\Rock\\Deep Purple\\{songName}";
            var songita = await Repository.GetSongByNameAndBandAsync(songName, bandName);

            if (songita == null)
            {
                songita = await ProcesameLaSong(songPath, band, style);
            }
            songita.SongSimplifications = new List<SongSimplification>();
            songita.SongSimplifications.Add(
                  await Repository.GetSongSimplificationBySongIdAndVersionAsync(songita.Id, 0, false, null));
            var base64encodedMidiBytes = MidiUtilities.GetMidiBytesFromNotes(songita.SongSimplifications[0].Notes, songita.TempoChanges);
            var original = MidiFile.Read(songPath, null);
            var modified = MidiFile.Read(base64encodedMidiBytes);
            var losProgramChange = UbicameLosProgramChange(original);
            ComparameLasSongs(original, modified);
            var sinControlChange = SacameLosControlChange(original);
            var sinTracksAlPedo = SacameLosTracksAlPedo(sinControlChange);
            var sinBending = SacameLosBendings(sinTracksAlPedo);

            MidiUtilities.SaveMidiFile(modified, "C:\\music\\test\\deepPurple\\BurnModificada.mid");
            return Ok(new ApiOKResponse("Gracias por comparar."));
        }

        private List<(int,long, byte)> UbicameLosProgramChange(MidiFile songi)
        {
            var retObj = new List<(int, long, byte)>();
            var acum = MidiUtilities.ConvertDeltaTimeToAccumulatedTime(songi);
            var chunky = -1;
            foreach (TrackChunk chunk in songi.Chunks)
            {
                chunky++;
                var eventsToRemove = new List<MidiEvent>();

                foreach (MidiEvent eventito in chunk.Events)
                {
                    if (eventito is ProgramChangeEvent)
                    {
                        var soret = eventito as ProgramChangeEvent;
                        retObj.Add((chunky, eventito.DeltaTime, soret.ProgramNumber));
                    }
                }
            }
            return retObj;
        }
        private MidiFile SacameLosBendings(MidiFile songi)
        {
            var acum = MidiUtilities.ConvertDeltaTimeToAccumulatedTime(songi);
            var chunksToRemove = new List<TrackChunk>();
            foreach (TrackChunk chunk in songi.Chunks)
            {
                var eventsToRemove = new List<MidiEvent>();

                foreach (MidiEvent eventito in chunk.Events)
                {
                    if (eventito is PitchBendEvent)
                    {
                        eventsToRemove.Add(eventito);
                    }
                }
                eventsToRemove.ForEach(e => chunk.Events.Remove(e));
            }
            return MidiUtilities.ConvertAccumulatedTimeToDeltaTime(acum);
        }



        private MidiFile SacameLosTracksAlPedo(MidiFile songi)
        {
            var chunksToRemove = new List<TrackChunk>();
            foreach (TrackChunk chunk in songi.Chunks)
            {
                var borralo = true;
                foreach (MidiEvent eventito in chunk.Events)
                {
                    if (eventito is NoteOnEvent)
                    {
                        borralo = false;
                        break;
                    }

                }
                if (borralo) chunksToRemove.Add(chunk);
            }
            chunksToRemove.ForEach(c => songi.Chunks.Remove(c));
            return songi;
        }
        private  MidiFile SacameLosControlChange(MidiFile songi)
        {
            var acum = MidiUtilities.ConvertDeltaTimeToAccumulatedTime(songi);
            foreach (TrackChunk chunk in songi.Chunks)
            {
                var eventsToRemove = new List<MidiEvent>();

                foreach (MidiEvent eventito in chunk.Events)
                {
                    if (eventito is ControlChangeEvent)
                    {
                        eventsToRemove.Add(eventito);
                    }
                }
                eventsToRemove.ForEach(e => chunk.Events.Remove(e));
            }
            return MidiUtilities.ConvertAccumulatedTimeToDeltaTime(acum);
        }
        private void ComparameLasSongs(MidiFile original, MidiFile modified)
        {
            var originalNoteOns = GetNoteOns(original).OrderBy(x => x.Item1).ThenBy(y => y.Item2).ThenBy(z => z.Item2).ToList();
            var modifiedNoteOns = GetNoteOns(modified).OrderBy(x => x.Item1).ThenBy(y => y.Item2).ThenBy(z => z.Item2).ToList();

        }
        private List<(long, byte, byte)> GetNoteOns(MidiFile file)
        {
            var retObj = new List<(long, byte, byte)>();
            var chunkito = -1;
            foreach (TrackChunk chunk in file.Chunks)
            {
                long currentTick = 0;
                byte instrument = 0;
                chunkito++;


                foreach (MidiEvent eventito in chunk.Events)
                {
                    currentTick += eventito.DeltaTime;
                    if (eventito is ProgramChangeEvent)
                    {
                        var pg = eventito as ProgramChangeEvent;
                        instrument = (byte)pg.ProgramNumber.valor;
                    }
                    if (eventito is NoteOnEvent)
                    {
                        NoteOnEvent noteOnEvent = eventito as NoteOnEvent;
                        retObj.Add((currentTick, instrument, noteOnEvent.NoteNumber));
                    }
                }
            }
            return retObj;
        }

    }
}
