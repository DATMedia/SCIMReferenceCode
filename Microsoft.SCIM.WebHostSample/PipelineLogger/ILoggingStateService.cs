using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipelineLogger
{
    // THis is the service that the logging states call
    interface ILoggingStateService
    {
        void Output(string text);
        void SwapResponseBodyStream(HttpResponse bodyStream);
        Task ReinstateResponseBodyStream();
        Task Decrement();
        void Increment();
    }
}
