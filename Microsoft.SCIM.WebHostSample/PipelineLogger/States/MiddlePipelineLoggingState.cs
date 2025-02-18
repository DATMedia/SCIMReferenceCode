using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace PipelineLogger.States
{
    internal class MiddlePipelineLoggingState : PipelineLoggingState
    {
        private readonly HttpContextSnapshot snapshot;

        public MiddlePipelineLoggingState(ILoggingStateService loggingStateService, HttpContextSnapshot snapshot)
            : base(loggingStateService)
        {
            this.snapshot = snapshot;
        }

        internal override Task<PipelineLoggingState> OnLoggingServiceDispose(HttpContext httpContext)
        {
            throw new System.NotImplementedException();
        }

        private string GenerateHeadersDifferenceText(string headersLabel, DictionaryDifference<StringValues> headersDifference)
        {
            StringBuilder sb = new StringBuilder();
            if (headersDifference.Added.Any())
            {
                string addedHeaderText = $"=== Added {headersLabel} Headers ===\r\n{StringFormatter.Format(headersDifference.Added)}";
                sb.AppendLine(addedHeaderText);
            }
            if (headersDifference.Removed.Any())
            {
                string removedHeaderText = $"=== Removed {headersLabel} Headers ===\r\n{StringFormatter.Format(headersDifference.Removed)}";
                sb.AppendLine(removedHeaderText);
            }
            foreach (var changed in headersDifference.Modified)
            {
                string modifiedText = $"Changed {headersLabel} header {changed.Key} from {StringFormatter.Format(changed.Value.Before)} to {StringFormatter.Format(changed.Value.After)}";
                sb.AppendLine(modifiedText);
            }
            return sb.ToString();
        }

        private enum PipelineDirection
        {
            Inbound,
            Outbound,
        };

        private Task<PipelineLoggingState> TakeSnapshotAndLogDifference(PipelineDirection pipelineDirection, string previous, HttpContext httpContext)
        {
            var newSnapshot = HttpContextSnapshot.TakeSnapshot(httpContext);
            var difference = HttpContextSnapshot.Compare(this.snapshot, newSnapshot);
            if (!difference.Empty)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{pipelineDirection}: Middleware {previous} made the following changes:");
                sb.Append(GenerateHeadersDifferenceText("Request", difference.Request.Headers));
                if (difference.ChangedUser)
                {
                    sb.Append($"+++ Changed User +++\r\n{StringFormatter.Format(difference.User)}");
                }
                sb.Append(GenerateHeadersDifferenceText("Response", difference.Response.Headers));
                if (difference.Response.StatusCode != null)
                {
                    sb.Append($"Changed status code from {difference.Response.StatusCode.Before} to {difference.Response.StatusCode.After}");
                }
                if (difference.Response.BodyLength != null)
                {
                    sb.Append($"+++ Wrote to body +++\r\n{StringFormatter.FormatResponseBody(httpContext.Response.Headers["Content-Type"].FirstOrDefault(), httpContext.Response.Body)}");
                }
                this._loggingStateService.Output(sb.ToString());
            }
            return Task.FromResult<PipelineLoggingState>(new MiddlePipelineLoggingState(this._loggingStateService, newSnapshot));
        }
        internal override async Task<PipelineLoggingState> OnPostInvoke(LoggerMiddlewareDescription lmDescription, HttpContext httpContext)
        {
            var state = await TakeSnapshotAndLogDifference(PipelineDirection.Outbound, lmDescription.MiddlewareAfter, httpContext);
            await this._loggingStateService.Decrement();
            return state;
        }

        internal override Task<PipelineLoggingState> OnPreInvoke(LoggerMiddlewareDescription lmDescription, HttpContext httpContext)
        {
            this._loggingStateService.Increment();
            return TakeSnapshotAndLogDifference(PipelineDirection.Inbound, lmDescription.MiddlewareBefore, httpContext);
        }
    }
}