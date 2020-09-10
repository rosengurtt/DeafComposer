using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace DeafComposer.Persistence
{
    partial class Repository
    {

        private string GetWhereStringForGetBands(string contains = null, long? styleId = null)
        {
            string dynamicQuery = "1==1";
            if (styleId != null && styleId > 0) dynamicQuery += $" && StyleId == {styleId}";
            if (!string.IsNullOrEmpty(contains)) dynamicQuery += $" && Name.Contains(\"{contains}\")";
            return dynamicQuery;
        }
        public async Task<List<Band>> GetBandsAsync(
                  int pageNo = 0,
                  int pageSize = 10,
                  string contains = null,
                  long? styleId = null)
        {
            IQueryable<Band> source = dbContext.Bands;
            source = source.Where(GetWhereStringForGetBands(contains, styleId));

            return await source
                      .OrderBy(s => s.Name)
                      .Include(z => z.Style)
                      .Skip((pageNo) * pageSize)
                      .Take(pageSize)
                      .ToListAsync();
        }
        public async Task<int> GetNumberOfBandsAsync(
            string contains = null,
            long? styleId = null)
        {
            IQueryable<Band> source = dbContext.Bands;
            source = source.Where(GetWhereStringForGetBands(contains, styleId));
            return await source.CountAsync();
        }

        public async Task<Band> GetBandByIdAsync(long bandId)
        {
            return await dbContext.Bands.Include(x => x.Style)
                .FirstOrDefaultAsync(x => x.Id == bandId);
        }
        public async Task<Band> GetBandByNameAsync(string name)
        {
            return await dbContext.Bands.Where(b => b.Name == name).FirstOrDefaultAsync();
        }
        public async Task<Band> AddBandAsync(Band band)
        {
            dbContext.Bands.Add(band);
            await dbContext.SaveChangesAsync();
            return band;
        }

        public async Task<Band> UpdateBandAsync(Band band)
        {
            var bands = await dbContext.Bands.FindAsync(band.Id);
            if (bands == null)
                throw new ApplicationException($"No band with id {band.Id}");

            dbContext.Entry(await dbContext.Bands
                .FirstOrDefaultAsync(x => x.Id == band.Id))
                .CurrentValues.SetValues(band);
            await dbContext.SaveChangesAsync();
            return band;
        }

        public async Task DeleteBandAsync(long bandId)
        {
            var bandItem = await dbContext.Bands.FirstOrDefaultAsync(x => x.Id == bandId);
            if (bandItem == null)
                throw new ApplicationException($"No band with id {bandId}");

            dbContext.Bands.Remove(bandItem);
            await dbContext.SaveChangesAsync();
        }
    }
}
