using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeafComposer.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo4j.Driver;

namespace DeafComposer.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = new string[]{"application/json"};
                options.EnableForHttps = true;
            });
            // The next statement disables the asp.net default automatic validation, so we can control the
            // response we send back when a request is invalid
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOriginsPolicy", // I introduced a string constant just as a label "AllowAllOriginsPolicy"
                builder =>
                {
                    builder.AllowAnyOrigin();
                });
            });
            var connection = Configuration.GetConnectionString("DeafComposer");
            services.AddDbContext<DBContext>(options => options.UseSqlServer(connection,
            opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds)));
            services.AddControllers();

            services.AddTransient<IRepository, Repository>();

            var neo4jUrl = Configuration.GetSection("neo4j:url").Value;
           var neo4jUsername = Configuration.GetSection("neo4j:username").Value;
            var neo4jPassword = Configuration.GetSection("neo4j:password").Value;
            var driver = GraphDatabase.Driver(neo4jUrl, AuthTokens.Basic(neo4jUsername, neo4jPassword));
            services.AddSingleton<IDriver>(driver);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();
            app.UseStatusCodePagesWithReExecute("/error/{0}");
            app.UseExceptionHandler("/error/500");
            app.UseCors("AllowAllOriginsPolicy");
 

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
