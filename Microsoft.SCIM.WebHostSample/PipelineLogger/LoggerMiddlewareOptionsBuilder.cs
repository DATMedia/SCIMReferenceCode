using System;
using System.Collections.Generic;
using System.Text;

namespace PipelineLogger
{
    class LoggerMiddlewareOptionsBuilder : ILoggerMiddlewareOptionsBuilder
    {
        internal LoggerMiddlewareDescription Description
        {
            get;
            private set;
        }

        internal LoggerMiddlewareOptionsBuilder()
        {
            this.Description = new LoggerMiddlewareDescription();
        }

        ILoggerMiddlewareOptionsBuilder ILoggerMiddlewareOptionsBuilder.MiddlewareAfter(string middlewareAfter)
        {
            this.Description.MiddlewareAfter = middlewareAfter;
            return this;
        }

        ILoggerMiddlewareOptionsBuilder ILoggerMiddlewareOptionsBuilder.MiddlewareBefore(string middlewareName)
        {
            this.Description.MiddlewareBefore = middlewareName;
            return this;
        }
    }
}
