using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PipelineLogger
{
    public class LoggingServiceOptions
    {
        public string ServerName { get; internal set; }
    }

    public class LoggingService : IDisposable
    {
        private readonly ILogger _logger;
        private PipelineLoggingState _state;
        private readonly LoggingStateService _loggingStateService;

        internal LoggingService(ILoggerFactory logger, LoggingServiceOptions options)
        {
            _logger = logger.CreateLogger($"{options.ServerName}");
            _loggingStateService = new LoggingStateService(_logger);
            _state = new States.InitialPipelineLoggingState(_loggingStateService);
        }

        void IDisposable.Dispose()
        {
        }

        internal void WriteToLogs(string text)
        {
            this._logger.LogInformation(text);
        }

        internal async Task OnPreInvoke(LoggerMiddlewareDescription lmDescription, HttpContext httpContext)
        {
            this._state = await this._state.OnPreInvoke(lmDescription, httpContext);
        }

        internal async Task OnPostInvoke(LoggerMiddlewareDescription lmDescription, HttpContext httpContext)
        {
            this._state = await this._state.OnPostInvoke(lmDescription, httpContext);
        }
    }
}
