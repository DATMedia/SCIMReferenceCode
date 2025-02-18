using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PipelineLogger
{
    class LoggingStateService : ILoggingStateService, IDisposable
    {
        private readonly ILogger _logger;

        public LoggingStateService(ILogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _loggerScope?.Dispose();
        }

        private IDisposable _loggerScope;

        private static int _loggerScopeId = 0;

        void ILoggingStateService.Output(string text)
        {
            _loggerScope = _loggerScope ?? _logger.BeginScope(_loggerScopeId++);
            _logger.LogInformation(text);
        }

        MemoryStream _substituteBodyStream;
        Stream _originalBodyStream;
        HttpResponse _responseWithSwappedStream;

        int _counter = 0;
        void ILoggingStateService.SwapResponseBodyStream(HttpResponse httpResponse)
        {
            this._substituteBodyStream = new MemoryStream();
            this._originalBodyStream = httpResponse.Body;
            httpResponse.Body = this._substituteBodyStream;
            this._responseWithSwappedStream = httpResponse;
            _counter = 1;
        }

        async Task ILoggingStateService.ReinstateResponseBodyStream()
        {
            if (_substituteBodyStream == null)
            {
                return;
            }
            _substituteBodyStream.Seek(0, SeekOrigin.Begin);
            await _substituteBodyStream.CopyToAsync(_originalBodyStream);
            this._responseWithSwappedStream.Body = _originalBodyStream;
            _substituteBodyStream = null;
        }

        async Task ILoggingStateService.Decrement()
        {
            if (--_counter == 0)
            {
                await ((ILoggingStateService)this).ReinstateResponseBodyStream();
            }
        }

        void ILoggingStateService.Increment()
        {
            ++_counter;
        }
    }
}
