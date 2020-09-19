using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Internal;
using ElastiLog;
using ElastiLog.Middleware;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.GraphQl;
using PlayTogetherApi.Services;

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
            services.AddSingleton<PasswordService>();
            services.AddSingleton<ObservablesService>();
            services.AddSingleton<FriendLogicService>();
            services.AddSingleton<UserStatisticsService>();

            services.AddScoped<InteractionsService>();
            services.AddScoped<AuthenticationService>();
            services.AddScoped<S3Service>();
            services.AddScoped<PushMessageService>();

            services.AddDbContext<PlayTogetherDbContext>(opt =>
            {
                var connectionString = Configuration.GetSection("PlayTogetherConnectionString").Value;
                opt.UseNpgsql(connectionString);
            });

            services.AddSingleton<PlayTogetherSchema>();
            services
                .AddGraphQL(options => {
                    options.EnableMetrics = false;
                    options.ExposeExceptions = true;
                })
                .AddNewtonsoftJson(deserializerSettings => { }, serializerSettings => { }) // For everything else
            //  .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment())
            //  .AddDataLoader()
                .AddWebSockets()
                .AddGraphTypes(typeof(PlayTogetherSchema), ServiceLifetime.Singleton)
                .AddUserContextBuilder(httpContext => new PlayTogetherUserContext { User = httpContext.User });
            services.Replace(ServiceDescriptor.Transient(typeof(IGraphQLExecuter<PlayTogetherSchema>), typeof(PlayTogetherGraphQLExecutor)));

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

            services.AddHostedService<StatisticsUpdateScheduler>();
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
