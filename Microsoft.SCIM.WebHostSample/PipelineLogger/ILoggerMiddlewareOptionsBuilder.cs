using System;
using System.Collections.Generic;
using System.Text;

namespace PipelineLogger
{
    public interface ILoggerMiddlewareOptionsBuilder
    {
        ILoggerMiddlewareOptionsBuilder MiddlewareBefore(string middlewareName);
        ILoggerMiddlewareOptionsBuilder MiddlewareAfter(string middlewareAfter);
    }
}
