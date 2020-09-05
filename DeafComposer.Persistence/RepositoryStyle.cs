using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    partial class Repository
    {

        public async Task<List<Style>> GetStylesAsync(
            int pageNo = 0,
            int pageSize = 10,
            string startWith = null)
        {
            if (string.IsNullOrEmpty(startWith))
                return await dbContext.Styles.OrderBy(x => x.Name).Skip((pageNo) * pageSize).Take(pageSize).ToListAsync();
            else
                return await dbContext.Styles.OrderBy(x => x.Name).Where(x => x.Name.StartsWith(startWith)).Skip((pageNo) * pageSize).Take(pageSize).ToListAsync();
        }
        public async Task<int> GetNumberOfStylesAsync(
            string startWith = null)
        {
            if (string.IsNullOrEmpty(startWith))
                return await dbContext.Styles.OrderBy(x => x.Name).CountAsync();
            else
                return await dbContext.Styles.OrderBy(x => x.Name)
                    .Where(x => x.Name.StartsWith(startWith)).CountAsync();
        }
        public async Task<Style> GetStyleByIdAsync(long styleId)
        {
            return await dbContext.Styles.FindAsync(styleId);
        }
        public async Task<Style> GetStyleByNameAsync(string name)
        {
            return await dbContext.Styles.Where(s => s.Name == name).FirstOrDefaultAsync();
        }
        public async Task<Style> AddStyleAsync(Style style)
        {
            dbContext.Styles.Add(style);
            await dbContext.SaveChangesAsync();
            return style;
        }

        public async Task<Style> UpdateStyleAsync(Style style)
        {
            var styles = await dbContext.Styles.FindAsync(style.Id);
            if (styles == null)
                throw new ApplicationException($"No style with id {style.Id}");

            dbContext.Entry(await dbContext.Styles.FirstOrDefaultAsync(x => x.Id == style.Id))
                    .CurrentValues.SetValues(style);
            await dbContext.SaveChangesAsync();
            return style;
        }

        public async Task DeleteStyleAsync(long styleId)
        {
            var style = await dbContext.Styles.FindAsync(styleId);
            if (style == null)
                throw new ApplicationException("There is no style with that id");

            dbContext.Styles.Remove(style);
            await dbContext.SaveChangesAsync();
        }
    }
}
