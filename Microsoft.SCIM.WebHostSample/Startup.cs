//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SCIM.WebHostSample
{
	using System.Text;
	using System.Threading.Tasks;
	using Microsoft.AspNetCore.Authentication;
	using Microsoft.AspNetCore.Authentication.JwtBearer;
	using Microsoft.AspNetCore.Builder;
	using Microsoft.AspNetCore.Hosting;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Routing;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using Microsoft.IdentityModel.Logging;
	using Microsoft.IdentityModel.Tokens;
	using Microsoft.SCIM.WebHostSample.Provider;
	using Newtonsoft.Json;
	using PipelineLogger;

	public class Startup
	{
		private readonly IWebHostEnvironment environment;
		private readonly IConfiguration configuration;

		public IMonitor MonitoringBehavior { get; set; }
		public IProvider ProviderBehavior { get; set; }

		public Startup(IWebHostEnvironment env, IConfiguration configuration)
		{
			this.environment = env;
			this.configuration = configuration;

			this.MonitoringBehavior = new ConsoleMonitor();
			this.ProviderBehavior = new InMemoryProvider();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			// Because we are testing
			IdentityModelEventSource.ShowPII = true;


			services.AddSingleton<SecurityConfigurationProvider>();

			void ConfigureMvcNewtonsoftJsonOptions(MvcNewtonsoftJsonOptions options) => options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

			void ConfigureAuthenticationOptions(AuthenticationOptions options)
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			}

			void ConfigureJwtBearerOptons(JwtBearerOptions options)
			{
				var securityConfigurationProvider = new SecurityConfigurationProvider(this.configuration);
				var securityConfiguration = securityConfigurationProvider.GetSecurityConfiguration();
				options.TokenValidationParameters = new()
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidIssuer = securityConfiguration.TokenIssuer,
					ValidAudience = securityConfiguration.TokenAudience,
					IssuerSigningKey = securityConfiguration.SigningKey,
				};
				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = context =>
					{
						return Task.CompletedTask;
					},
					OnForbidden = context =>
					{
						return Task.CompletedTask;
					},
					OnChallenge = context =>
					{
						return Task.CompletedTask;
					},
					OnMessageReceived = messageReceivedContext =>
					{
						return Task.CompletedTask;
					},
					OnAuthenticationFailed = AuthenticationFailed
				};
				options.IncludeErrorDetails = true;
			}

			services.AddAuthentication(ConfigureAuthenticationOptions).AddJwtBearer(ConfigureJwtBearerOptons);
			services.AddControllers().AddNewtonsoftJson(ConfigureMvcNewtonsoftJsonOptions);

			services.AddSingleton(typeof(IProvider), this.ProviderBehavior);
			services.AddSingleton(typeof(IMonitor), this.MonitoringBehavior);
			services.AddPipelineLogging(
				pipeLineLoggingOptions =>
				{
					pipeLineLoggingOptions.ServerName("SCIM");
				}
			);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app)
		{
			if (this.environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHsts();
			app.UseRouting();
			app.UseHttpsRedirection();
			app.InsertPipelineLogger(l => l.MiddlewareBefore("HttpsRedirection").MiddlewareAfter("Authentication"));
			app.UseAuthentication();
			app.InsertPipelineLogger(l => l.MiddlewareBefore("UseAuthentication").MiddlewareAfter("UseAuthorization"));
			app.UseAuthorization();
			app.InsertPipelineLogger(l => l.MiddlewareBefore("UseAuthorization").MiddlewareAfter("UseEndpoints"));

			app.UseEndpoints(
				(IEndpointRouteBuilder endpoints) =>
				{
					endpoints.MapDefaultControllerRoute();
				});
		}

		private Task AuthenticationFailed(AuthenticationFailedContext arg)
		{
			// For debugging purposes only!
			string authenticationExceptionMessage = $"{{AuthenticationFailed: '{arg.Exception.Message}'}}";

			arg.Response.ContentLength = authenticationExceptionMessage.Length;
			arg.Response.Body.WriteAsync(
				Encoding.UTF8.GetBytes(authenticationExceptionMessage),
				0,
				authenticationExceptionMessage.Length);

			return Task.FromException(arg.Exception);
		}
	}
}
