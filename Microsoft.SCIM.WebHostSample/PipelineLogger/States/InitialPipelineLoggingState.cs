using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace PipelineLogger.States
{
    internal sealed class InitialPipelineLoggingState : PipelineLoggingState
    {
        internal InitialPipelineLoggingState(ILoggingStateService loggingStateService) : base(loggingStateService)
        {
        }
        internal override Task<PipelineLoggingState> OnLoggingServiceDispose(HttpContext httpContext)
        {
            return Task.FromResult<PipelineLoggingState>(this);
        }

        internal override Task<PipelineLoggingState> OnPostInvoke(LoggerMiddlewareDescription loggerMiddlewareDescription, HttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        private static string GenerateHeaderDescription(string title, IEnumerable<KeyValuePair<string, StringValues>> headers)
        {
            var headerText = StringFormatter.Format(headers);
            return $"*** {title} ***\r\n{headerText}";
        }

        private static string GenerateHeaderDescription(string title, IEnumerable<KeyValuePair<string, string>> headers)
            => GenerateHeaderDescription(title, headers.Select(kvp => new KeyValuePair<string, StringValues>(kvp.Key, new StringValues(kvp.Value))));

        private static string GenerateRequestHeaders(HttpContextSnapshot httpContext)
            => GenerateHeaderDescription("Request Headers", httpContext.Request.Headers);

        private static string GenerateResponseHeaders(HttpContextSnapshot httpContext)
            => GenerateHeaderDescription("Response Headers", httpContext.Response.Headers);

        private static string GenerateIncomingMessageBanner(HttpContextSnapshot httpContext)
        {
            var message = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            return StringFormatter.GenerateBanner(message);
        }

        private static async Task<(MemoryStream newStream, byte[] contents)> Extract(Stream sourceStream)
        {
            MemoryStream newStream = new MemoryStream();
            await sourceStream.CopyToAsync(newStream);
            newStream.Seek(0, SeekOrigin.Begin);
            byte[] contents = new byte[newStream.Length];
            newStream.Read(contents, 0, contents.Length);
            newStream.Seek(0, SeekOrigin.Begin);
            return (
                    newStream,
                    contents
                );
        }

        internal override async Task<PipelineLoggingState> OnPreInvoke(LoggerMiddlewareDescription loggerMiddlewareDescription, HttpContext httpContext)
        {
            this._loggingStateService.SwapResponseBodyStream(httpContext.Response);

            var snapshot = HttpContextSnapshot.TakeSnapshot(httpContext);
            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateIncomingMessageBanner(snapshot));

            if (httpContext.Request.Query.Any())
            {
                sb.Append(GenerateHeaderDescription("Query Parameters", httpContext.Request.Query));
            }
            sb.Append(GenerateRequestHeaders(snapshot));
            if (httpContext.Request.Cookies.Any())
            {
                sb.Append(
                    GenerateHeaderDescription(
                        "Cookies",
                        httpContext.Request.Cookies.Select(
                            c =>
                                new KeyValuePair<string, StringValues>(c.Key, new StringValues(c.Value)))
                        )
                );
            }
            byte[] bodyContent;
            (httpContext.Request.Body, bodyContent) = await Extract(httpContext.Request.Body);
            if (bodyContent.Length > 0)
            {
                sb.AppendLine($"+++ Request Body +++\r\n{StringFormatter.FormatBody(httpContext.Request.Headers["Content-Type"], bodyContent)}");
            }

            if (snapshot.Response.Headers.Any())
            {
                sb.Append(GenerateResponseHeaders(snapshot));
            }

            sb.AppendLine($"+++ User +++\r\n{StringFormatter.Format(snapshot.User)}");

            this._loggingStateService.Output(sb.ToString());
            return new MiddlePipelineLoggingState(this._loggingStateService, snapshot);
        }
    }
}
