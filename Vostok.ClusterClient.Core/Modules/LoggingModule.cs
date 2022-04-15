using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Misc;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Modules
{
    internal class LoggingModule : IRequestModule
    {
        private readonly LoggingMode loggingMode;
        private readonly bool logRequests;
        private readonly bool logResults;
        private readonly string targetService;

        public LoggingModule(LoggingMode loggingMode, bool logRequests, bool logResults, string targetService)
        {
            this.loggingMode = loggingMode;
            this.logRequests = loggingMode == LoggingMode.Detailed && logRequests;
            this.logResults = loggingMode == LoggingMode.Detailed && logResults;
            this.targetService = targetService;
        }

        public async Task<ClusterResult> ExecuteAsync(IRequestContext context, Func<IRequestContext, Task<ClusterResult>> next)
        {
            if (logRequests)
                LogRequestDetails(context);

            var result = await next(context).ConfigureAwait(false);

            if (logResults)
            {
                if (result.Status == ClusterResultStatus.Success)
                    LogSuccessfulResult(context, result);
                else
                    LogFailedResult(context, result);
            }

            if (loggingMode != LoggingMode.Detailed)
            {
                LogSingleMessage(context, result);
            }

            return result;
        }

        #region Logging

        private void LogRequestDetails(IRequestContext context) =>
            context.Log.Info("Sending request '{Request}' to '{TargetService}'. Timeout = {Timeout}. Strategy = '{Strategy}'.",
                new
                {
                    Request = context.Request.ToString(false, false),
                    TargetService = targetService ?? "somewhere",
                    Timeout = context.Budget.Total.ToPrettyString(),
                    TimeoutMs = context.Budget.Total.TotalMilliseconds,
                    Strategy = context.Parameters.Strategy?.ToString()
                });

        private void LogSuccessfulResult(IRequestContext context, ClusterResult result) =>
            context.Log.Info(
                "Success. Response code = {ResponseCode:D} ('{ResponseCode}'). Time = {ElapsedTime}.",
                new
                {
                    ResponseCode = result.Response.Code,
                    ElapsedTime = context.Budget.Elapsed.ToPrettyString(),
                    ElapsedTimeMs = context.Budget.Elapsed.TotalMilliseconds
                });

        private void LogFailedResult(IRequestContext context, ClusterResult result)
        {
            const string message = "Request '{Request}' to '{TargetService}' has failed with status '{Status}'. Response code = {ResponseCode:D} ('{ResponseCode}'). Time = {ElapsedTime}.";
            var properties = new
            {
                Request = context.Request.ToString(false, false),
                TargetService = targetService ?? "somewhere",
                result.Status,
                ResponseCode = result.Response.Code,
                ElapsedTime = context.Budget.Elapsed.ToPrettyString(),
                ElapsedTimeMs = context.Budget.Elapsed.TotalMilliseconds
            };

            if (result.Status == ClusterResultStatus.Canceled)
                context.Log.Warn(message, properties);
            else
                context.Log.Error(message, properties);
        }

        private void LogSingleMessage(IRequestContext context, ClusterResult result)
        {
            switch (loggingMode)
            {
                case LoggingMode.SingleShortMessage:
                    LogShortMessage(context, result);
                    break;
                case LoggingMode.SingleVerboseMessage:
                    LogVerboseMessage(context, result);
                    break;
            }
        }

        private void LogShortMessage(IRequestContext context, ClusterResult result)
        {
            const string template = "'{Request}' to '{TargetService}'. Code = {ResponseCode:D}. Time = {ElapsedTime}.";
            var properties = new
            {
                Request = context.Request.ToString(false, false),
                TargetService = targetService ?? "somewhere",
                ResponseCode = result.Response.Code,
                ElapsedTime = context.Budget.Elapsed.ToPrettyString(),
                ElapsedTimeMs = context.Budget.Elapsed.TotalMilliseconds
            };

            switch (result.Status)
            {
                case ClusterResultStatus.Success:
                    context.Log.Info(template, properties);
                    break;
                case ClusterResultStatus.Canceled:
                    context.Log.Warn(template, properties);
                    break;
                default:
                    context.Log.Error(template, properties);
                    break;
            }
        }

        private void LogVerboseMessage(IRequestContext context, ClusterResult result)
        {
            const string template = "'{Request}' to '{TargetService}', Timeout = {Timeout}, Strategy = '{Strategy}'. {Status} in {ElapsedTime}, Code = {ResponseCode:D}. Replicas result = {ReplicasResult}";
            var properties = new
            {
                Request = context.Request.ToString(false, false),
                TargetService = targetService ?? "somewhere",
                Timeout = context.Budget.Total.ToPrettyString(),
                TimeoutMs = context.Budget.Total.TotalMilliseconds,
                Strategy = context.Parameters.Strategy?.ToString(),
                result.Status,
                ElapsedTime = context.Budget.Elapsed.ToPrettyString(),
                ElapsedTimeMs = context.Budget.Elapsed.TotalMilliseconds,
                ResponseCode = result.Response.Code,
                ReplicasResult = ConvertResultsToProperties(result.ReplicaResults)
            };

            switch (result.Status)
            {
                case ClusterResultStatus.Success:
                    context.Log.Info(template, properties);
                    break;
                case ClusterResultStatus.Canceled:
                    context.Log.Warn(template, properties);
                    break;
                default:
                    context.Log.Error(template, properties);
                    break;
            }
        }

        private object ConvertResultsToProperties(IList<ReplicaResult> replicaResults)
        {
            var properties = new object[replicaResults.Count];
            for (var i = 0; i < replicaResults.Count; i++)
            {
                var res = replicaResults[i];
                properties[i] = new
                {
                    res.Replica,
                    ResponseCode = (int)res.Response.Code,
                    res.Verdict,
                    ElapsedTime = res.Time.ToPrettyString()
                };
            }

            return properties;
        }

        #endregion
    }
}