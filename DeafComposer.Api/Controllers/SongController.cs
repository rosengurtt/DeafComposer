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
        public async Task<IActionResult> GetSong(int songId, int simplificationVersion)
        {
            Song song =  await Repository.GetSongByIdAsync(songId);
            if (song == null)
                return NotFound(new ApiResponse(404));

            song.MidiBase64Encoded = null;
            song.SongSimplifications = new List<SongSimplification>();
            song.SongSimplifications.Add(
                await Repository.GetSongSimplificationBySongIdAndVersionAsync(songId, simplificationVersion));
    

            return Ok(new ApiOKResponse(song));
        }

        [HttpGet("{songId}/midi")]
        public async Task<IActionResult> GetSongMidi(int songId, int simplificationVersion, int startInSeconds = 0)
        {
            Song song = await Repository.GetSongByIdAsync(songId);
            if (song == null)
                return null;

            song.SongSimplifications = new List<SongSimplification>();
            song.SongSimplifications.Add(
                await Repository.GetSongSimplificationBySongIdAndVersionAsync(songId, simplificationVersion));

            try
            {
                var ms = new MemoryStream(MidiUtilities.GetMidiBytesFromPointInTime(song.MidiBase64Encoded, startInSeconds));

                //var ms2= new MemoryStream(MidiUtilities.GetMidiBytesFromPointInTime(song.MidiBase64Encoded, startInSeconds));
                //using (FileStream file=new FileStream(@"c:\music\sorete.mid", FileMode.Create, System.IO.FileAccess.Write))
                //{
                //    var bytes = new byte[ms2.Length];
                //    ms2.Read(bytes, 0, (int)ms.Length);
                //    file.Write(bytes, 0, bytes.Length);
                //    ms2.Close();
                //}

                return File(ms, MediaTypeNames.Text.Plain, song.Name);
            }
            catch(Exception ex)
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