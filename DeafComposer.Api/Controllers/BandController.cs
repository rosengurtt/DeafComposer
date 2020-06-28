using Microsoft.AspNetCore.Mvc;
using DeafComposer.Api.ErrorHandling;
using DeafComposer.Models.Entities;
using DeafComposer.Persistence;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace DeafComposer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BandController : ControllerBase
    {
        private IRepository Repository;

        public BandController( IRepository Repository)
        {
            this.Repository = Repository;
        }
        // GET: api/Band?pageSize=10&pageNo=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable>> GetBands(
            int pageNo = 1,
            int pageSize = 10000,
            string startWith = null,
            int? styleId = null)
        {
            var totalBands = await Repository.GetNumberOfBandsAsync(startWith, styleId);

            var bands = await Repository.GetBandsAsync(pageNo, pageSize, startWith, styleId);
            var retObj = new
            {
                page = pageNo,
                totalPages = (int)Math.Ceiling((double)totalBands / pageSize),
                bands
            };
            return Ok(new ApiOKResponse(retObj));
        }

        // GET: api/Band/5
        [HttpGet("{bandId}")]
        public async Task<IActionResult> GetBand(int bandId)
        {
            var band = await Repository.GetBandByIdAsync(bandId);

            if (band == null)
                return NotFound(new ApiResponse(404, $"No band with id {bandId}"));

            return Ok(new ApiOKResponse(band));
        }

        // PUT: api/Band/5
        [HttpPut("{bandId}")]
        public async Task<ActionResult> PutBand(int bandId, Band band)
        {
            if (band.Id != bandId)
                return BadRequest(new ApiBadRequestResponse("Id passed in url does not match id passed in body."));
            
            try
            {
                var bandita = await Repository.UpdateBandAsync(band);
                return Ok(new ApiOKResponse(bandita));
            }
            catch (ApplicationException)
            {
                return NotFound(new ApiResponse(404, $"No band with id {bandId}"));
            }
        }

        // POST: api/Band
        [HttpPost]
        public async Task<ActionResult> PostBand(Band band)
        {
            if (ModelState.IsValid)
            {
                var bandita = await Repository.AddBandAsync(band);
                return Ok(new ApiOKResponse(bandita));
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(ModelState));
            }
        }

        // DELETE: api/Band/5
        [HttpDelete("{bandId}")]
        public async Task<ActionResult> DeleteBand(int bandId)
        {
            try
            {
                await Repository.DeleteBandAsync(bandId);
                return Ok(new ApiOKResponse("Record deleted"));
            }
            catch (ApplicationException) {
                return NotFound(new ApiResponse(404, $"No band with id {bandId}"));
            }
        }
    }
}
