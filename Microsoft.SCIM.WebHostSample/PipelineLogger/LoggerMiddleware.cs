using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipelineLogger
{
    public class LoggerMiddleware : IDisposable
    {
        private readonly RequestDelegate _next;
        private readonly LoggerMiddlewareDescription _description;

        public LoggerMiddleware(RequestDelegate next, LoggerMiddlewareDescription description)
        {
            _next = next;
            this._description = description;
        }

        void IDisposable.Dispose()
        {
        }

        public async Task Invoke(HttpContext httpContext, LoggingService service)
        {
            await service.OnPreInvoke(this._description, httpContext);
            try
            {
                await _next(httpContext);
            }
            finally
            {
                await service.OnPostInvoke(this._description, httpContext);
            }
        }
    }
}
