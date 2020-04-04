using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using GraphQL;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.GraphQl;
using PlayTogetherApi.Services;
using ElastiLog;
using ElastiLog.Middleware;

namespace PlayTogetherApi.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.Configure<ElastiLogConfiguration>(Configuration.GetSection("Logging:ElastiLog"));
            services.AddSingleton(Configuration);
            services.AddSingleton<FriendLogicService>();
            services.AddSingleton<UserStatisticsService>();
            services.AddSingleton<ObservablesService>();

            services.AddScoped<AuthenticationService>();
            services.AddScoped<S3Service>();
            services.AddScoped<PushMessageService>();

            services.AddDbContext<PlayTogetherDbContext>(opt =>
            {
                var connectionString = Configuration.GetSection("PlayTogetherConnectionString").Value;
                opt.UseNpgsql(connectionString);
            });

            services.AddScoped<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));
            services.AddScoped<PlayTogetherSchema>();
            services
                .AddGraphQL(options => {
                    options.ExposeExceptions = false;
                })
                .AddWebSockets()
                .AddGraphTypes(ServiceLifetime.Scoped)
                .AddUserContextBuilder(httpContext => httpContext.User);

            services
                .AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy",
                        builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        /*.AllowCredentials()*/);
                });

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                    };
                });

            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddElastiLog();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseElastiLog();
            loggerFactory.UseElastiLog(serviceProvider);

            app.UseHttpsRedirection();
            app.UseCors("CorsPolicy");

            app.UseAuthentication();

            app.UseWebSockets();
            app.UseGraphQLWebSockets<PlayTogetherSchema>();
            app.UseGraphQL<PlayTogetherSchema>();
            app.UseGraphQLPlayground(options: new GraphQLPlaygroundOptions() { Path = "/" });

            app.UseMvc();
        }
    }
}
