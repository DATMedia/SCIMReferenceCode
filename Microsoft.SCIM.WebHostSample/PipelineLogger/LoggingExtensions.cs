using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PipelineLogger
{
    public interface IPipeLineLoggingOptionsBuilder
    {
        IPipeLineLoggingOptionsBuilder ServerName(string serverName);
    }

    internal class PipelineLoggingOptionsBuilder : IPipeLineLoggingOptionsBuilder
    {
        public LoggingServiceOptions Options { get; private set; }

        public PipelineLoggingOptionsBuilder()
        {
            this.Options = new LoggingServiceOptions();
        }

        IPipeLineLoggingOptionsBuilder IPipeLineLoggingOptionsBuilder.ServerName(string serverName)
        {
            this.Options.ServerName = serverName;
            return this;
        }
    }

    public static class LoggingExtensions
    {
        public static IApplicationBuilder InsertPipelineLogger(this IApplicationBuilder builder, Action<ILoggerMiddlewareOptionsBuilder> buildOptions)
        {
            var optionsBuilder = new LoggerMiddlewareOptionsBuilder();
            buildOptions(optionsBuilder);
            return builder.UseMiddleware<LoggerMiddleware>(optionsBuilder.Description);
        }

        public static IServiceCollection AddPipelineLogging(this IServiceCollection serviceCollection, Action<IPipeLineLoggingOptionsBuilder> buildOptions)
        {
            PipelineLoggingOptionsBuilder optionsBuilder = new PipelineLoggingOptionsBuilder();
            buildOptions(optionsBuilder);
            return serviceCollection.AddScoped<LoggingService>((serviceProvider) => new LoggingService(serviceProvider.GetServices<ILoggerFactory>().First(), optionsBuilder.Options));
        }
    }
}
