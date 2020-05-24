﻿using DeafComposer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Persistence
{
    public partial class Repository : IRepository
    {
        private readonly DBContext dbContext;
        private readonly string ConnectionString;
        public Repository(DBContext dbcontext, IConfiguration configuration)
        {
            dbContext = dbcontext;
            ConnectionString = configuration.GetSection("ConnectionStrings:PlagiatorSql").Value;
        }

        public async Task<TimeSignature> GetTimeSignatureAsync(TimeSignature ts)
        {
            return await dbContext.TimeSignatures.Where(x => x.Numerator == ts.Numerator &
            x.Denominator == ts.Denominator).FirstOrDefaultAsync();
        }
    }
}