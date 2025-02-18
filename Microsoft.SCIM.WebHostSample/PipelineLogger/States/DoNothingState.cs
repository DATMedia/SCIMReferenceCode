using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipelineLogger.States
{
    internal sealed class DoNothingState : PipelineLoggingState
    {
        internal DoNothingState(ILoggingStateService loggingStateService) : base(loggingStateService)
        {
        }

        internal override Task<PipelineLoggingState> OnLoggingServiceDispose(HttpContext httpContext) =>
            Task.FromResult<PipelineLoggingState>(this);

        internal override Task<PipelineLoggingState> OnPostInvoke(LoggerMiddlewareDescription loggerMiddlewareDescription, HttpContext httpContext)
            => Task.FromResult<PipelineLoggingState>(this);

        internal override Task<PipelineLoggingState> OnPreInvoke(LoggerMiddlewareDescription loggerMiddlewareDescription, HttpContext httpContext)
            => Task.FromResult<PipelineLoggingState>(this);
    }
}
