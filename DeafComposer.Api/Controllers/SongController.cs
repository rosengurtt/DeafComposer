using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Threading.Tasks;
using DeafComposer.Persistence;
using DeafComposer.Api.ErrorHandling;
using DeafComposer.Models.Entities;
using System.Collections.Generic;
using DeafComposer.Midi;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Linq;
using Melanchall.DryWetMidi.Core;

namespace DeafComposer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongController : ControllerBase
    {
        private IRepository Repository;

        public SongController(IRepository Repository)
        {
            this.Repository = Repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable>> GetSongs(
            int pageNo = 0,
            int pageSize = 10,
            string contains = null,
            long? styleId = null,
            long? bandId = null)
        {
            var totalSongs =await Repository.GetNumberOfSongsAsync(contains, styleId, bandId);
         
            var songs = await Repository.GetSongsAsync(pageNo, pageSize, contains, styleId, bandId);
            var retObj = new
            {
                pageNo,
                pageSize,
                totalItems = totalSongs,
                totalPages = (int)Math.Ceiling((double)totalSongs / pageSize),
                songs
            };
            return Ok(new ApiOKResponse(retObj));
        }

        // GET: api/Song/5
        [HttpGet("{songId}")]
        public async Task<IActionResult> GetSong(int songId, int? simplificationVersion)
        {
            Song song =  await Repository.GetSongByIdAsync(songId, simplificationVersion);
            if (song == null)
                return NotFound(new ApiResponse(404));

            song.MidiBase64Encoded = null;

           // AddeDrumVoiceWithAllPitches(song);

            return Ok(new ApiOKResponse(song));
        }
        private void AddeDrumVoiceWithAllPitches(Song song)
        {
            var newVoice = new List<Note>();

            var newVoiceNo = song.SongSimplifications[1].NumberOfVoices;
            song.SongSimplifications[1].NumberOfVoices = newVoiceNo+1;

            for (byte i =35; i<80; i++)
            {
                var note = new Note
                {
                    StartSinceBeginningOfSongInTicks = 96 * (i-35),
                    EndSinceBeginningOfSongInTicks = 96 * (i -34),
                    Pitch = i,
                    Voice = (byte)newVoiceNo,
                    Volume = 100,
                    IsPercussion = true,
                    Instrument = 0,
                    Id = 1234567 + i

                };
                song.SongSimplifications[1].Notes.Add(note);
            }
        }

        [HttpGet("{songId}/midi")]
        public async Task<IActionResult> GetSongMidi(int songId, int tempoInBeatsPerMinute, int simplificationVersion = 1, int startInSeconds = 0, string mutedTracks = null)
        {
            try
            {
                Song song = await Repository.GetSongByIdAsync(songId);
                if (song == null)
                    return null;
                int[] tracksToMute = mutedTracks?.Split(',').Select(x => int.Parse(x)).ToArray();

                
                var tempoInMicrosecondsPerBeat = 120 * 500000 / tempoInBeatsPerMinute;
                // If the tempoInBeatsPerMinute parameter is passed, we recalculate all the tempo changes. The tempo change shown to the
                // user in the UI is the first one, so if the user changes it, we get the proportion of the new tempo to the old one and
                // we change all tempos in the same proportion
                var tempoChanges = tempoInBeatsPerMinute != 0 ? song.TempoChanges.Select(x => new TempoChange
                {
                    Id = x.Id,
                    SongId = songId,
                    TicksSinceBeginningOfSong = x.TicksSinceBeginningOfSong,
                    MicrosecondsPerQuarterNote = (int)Math.Round((double)x.MicrosecondsPerQuarterNote * tempoInMicrosecondsPerBeat / song.TempoChanges[0].MicrosecondsPerQuarterNote)
                }).ToList() :
                song.TempoChanges;
                var songSimplification= await Repository.GetSongSimplificationBySongIdAndVersionAsync(songId, simplificationVersion, false, tracksToMute);
                var base64encodedMidiBytes = MidiUtilities.GetMidiBytesFromNotes(songSimplification.Notes, tempoChanges);
                var ms = new MemoryStream(MidiUtilities.GetMidiBytesFromPointInTime(base64encodedMidiBytes, startInSeconds));

                return File(ms, MediaTypeNames.Text.Plain, song.Name);
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        // GET: api/Song/5/Info
        [HttpGet("{songId}/Info")]
        public async Task<IActionResult> GetSongInfo(int songId)
        {
            Song song = await Repository.GetSongByIdAsync(songId);
            song.MidiBase64Encoded = "";
            if (song == null)
                return NotFound(new ApiResponse(404));

            return Ok(new ApiOKResponse(song));
        }


        [HttpGet("{songId}/simplifications")]
        public async Task<IActionResult> GetSongSimplifications(long songId)
        {
            var simpl = await Repository.GetSongsSimplificationsOfsongAsync(songId);

            if (simpl == null)
                return NotFound(new ApiResponse(404));

            return Ok(new ApiOKResponse(simpl));
        }
        [HttpGet("{songId}/simplifications/{simpVersion}")]
        public async Task<IActionResult> GetSongSimplification(long songId, int simpVersion)
        {
            var simpl = await Repository.GetSongSimplificationBySongIdAndVersionAsync(songId, simpVersion, true);

            if (simpl == null)
                return NotFound(new ApiResponse(404));

            return Ok(new ApiOKResponse(simpl));
        }
        // PUT: api/Song/5
        [HttpPut("{songId}")]
        public async Task<ActionResult> PutSong(int songId, Song song)
        {
            if (song.Id != songId)
                return BadRequest(new ApiBadRequestResponse("Id passed in url does not match id passed in body."));

            try
            {
                await Repository.UpdateSongAsync(song);
                return Ok(new ApiOKResponse(song));
            }
            catch (ApplicationException)
            {
                return NotFound(new ApiResponse(404, "No song with that id"));
            }
        }

        // POST: api/Song
        [HttpPost]
        public async Task<ActionResult> PostSong(Song song)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var addedSong = await Repository.AddSongAsync(song);
                    return Ok(new ApiOKResponse(addedSong));
                }
                catch (DbUpdateException)
                {
                    return Conflict(new ApiResponse(409, "There is already a Song with that name."));
                }
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(ModelState));
            }
        }

        // DELETE: api/Song/5
        [HttpDelete("{songId}")]
        public async Task<ActionResult> DeleteSong(int songId)
        {
            try
            {
                await Repository.DeleteSongAsync(songId);
                return Ok(new ApiOKResponse("Record deleted"));
            }
            catch (ApplicationException)
            {
                return NotFound(new ApiResponse(404, "No song with that id"));
            }
        }
    }
}