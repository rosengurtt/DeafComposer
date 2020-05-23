using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using DeafComposer.Persistence;
using DeafComposer.Api.ErrorHandling;
using DeafComposer.Models.Entities;

namespace DeafComposer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StyleController : ControllerBase
    {
        private IRepository Repository;

        public StyleController(IRepository Repository)
        {
            this.Repository = Repository;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable>> GetStylesAsync(int pageNo = 1, int pageSize = 10, string startWith = null)
        {
            var totaStyles = await Repository.GetNumberOfStylesAsync(pageNo, pageSize, startWith);
            var styles = await Repository.GetStylesAsync(pageNo, pageSize, startWith);
            var retObj = new
            {
                page = pageNo,
                totalPages = (int)Math.Ceiling((double)totaStyles / pageSize),
                styles
            };
            return Ok(new ApiOKResponse(retObj));
        }

        // GET: api/Style/5
        [HttpGet("{styleId}")]
        public async Task<IActionResult> GetStyleAsync(int styleId)
        {
            var styles = await Repository.GetStyleByIdAsync(styleId);

            if (styles == null)
                return NotFound(new ApiResponse(404));

            return Ok(new ApiOKResponse(styles));
        }

        // PUT: api/Style/5
        [HttpPut("{styleId}")]
        public async Task<ActionResult> PutStyle(int styleId, Style style)
        {
            if (style.Id != styleId)
            {
                return BadRequest(new ApiBadRequestResponse("Id passed in url does not match id passed in body."));
            }

            try
            {
                var stylete = await Repository.UpdateStyleAsync(style);
                return Ok(new ApiOKResponse(stylete));
            }
            catch (ApplicationException)
            {
                return NotFound(new ApiResponse(404, "No style with that id"));
            }
        }

        // POST: api/Style
        [HttpPost]
        public async Task<ActionResult> PostStyle(Style style)
        {
            if (ModelState.IsValid)
            {
                    var stylete = await Repository.AddStyleAsync(style);
                    return Ok(new ApiOKResponse(stylete));               
            }
            else
                return BadRequest(new ApiBadRequestResponse(ModelState));
        }

        // DELETE: api/Style/5
        [HttpDelete("{styleId}")]
        public async Task<ActionResult> DeleteStyle(int styleId)
        {
            try
            {
                await Repository.DeleteStyleAsync(styleId);
                return Ok(new ApiOKResponse("Record deleted"));
            }
            catch (ApplicationException)
            {
                return NotFound(new ApiResponse(404, "No style with that id"));
            }
        }
    }
}
