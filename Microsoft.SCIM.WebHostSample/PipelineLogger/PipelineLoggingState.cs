using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipelineLogger
{
    internal abstract class PipelineLoggingState
    {
        protected readonly ILoggingStateService _loggingStateService;
        protected PipelineLoggingState(ILoggingStateService loggingStateService)
        {
            _loggingStateService = loggingStateService;
        }
        internal abstract Task<PipelineLoggingState> OnPreInvoke(LoggerMiddlewareDescription loggerMiddlewareDescription, HttpContext httpContext);
        internal abstract Task<PipelineLoggingState> OnPostInvoke(LoggerMiddlewareDescription loggerMiddlewareDescription, HttpContext httpContext);
        internal abstract Task<PipelineLoggingState> OnLoggingServiceDispose(HttpContext httpContext);
    }
}
