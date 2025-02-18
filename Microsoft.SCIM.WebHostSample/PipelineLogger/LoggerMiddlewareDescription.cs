using System;
using System.Collections.Generic;
using System.Text;

namespace PipelineLogger
{
    public class LoggerMiddlewareDescription
    {
        internal string MiddlewareBefore { get; set; }
        internal string MiddlewareAfter { get; set; }
    }
}
