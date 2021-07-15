﻿using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Criteria;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Ordering;
using Vostok.Clusterclient.Core.Ordering.Storage;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Core.Sending
{
    internal class RequestSender : IRequestSenderInternal
    {
        private readonly IClusterClientConfiguration configuration;
        private readonly IReplicaStorageProvider storageProvider;
        private readonly IResponseClassifier responseClassifier;
        private readonly IRequestConverter requestConverter;

        public RequestSender(
            IClusterClientConfiguration configuration,
            IReplicaStorageProvider storageProvider,
            IResponseClassifier responseClassifier,
            IRequestConverter requestConverter)
        {
            this.configuration = configuration;
            this.storageProvider = storageProvider;
            this.responseClassifier = responseClassifier;
            this.requestConverter = requestConverter;
        }

        public async Task<ReplicaResult> SendToReplicaAsync(
            ITransport transport,
            IReplicaOrdering replicaOrdering,
            Uri replica,
            Request request,
            int connectionAttempts,
            TimeSpan? connectionTimeout,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (configuration.Logging.LogReplicaRequests)
                LogRequest(replica, timeout);

            var timeBudget = TimeBudget.StartNew(timeout, TimeSpan.FromMilliseconds(1));

            var absoluteRequest = requestConverter.TryConvertToAbsolute(request, replica);

            var response = await SendRequestAsync(transport, absoluteRequest, timeBudget, connectionAttempts, connectionTimeout, cancellationToken).ConfigureAwait(false);

            var responseVerdict = responseClassifier.Decide(response, configuration.ResponseCriteria);

            var result = new ReplicaResult(replica, response, responseVerdict, timeBudget.Elapsed);

            if (configuration.Logging.LogReplicaResults)
                LogResult(result);

            replicaOrdering.Learn(result, storageProvider);

            return result;
        }

        private async Task<Response> SendRequestAsync(
            ITransport transport,
            [CanBeNull] Request request,
            TimeBudget timeBudget,
            int connectionAttempts,
            TimeSpan? connectionTimeout,
            CancellationToken cancellationToken)
        {
            if (request == null)
                return Responses.Unknown;

            transport = new ConnectionAttemptsTransport(transport, connectionAttempts);

            try
            {
                var response = await transport.SendAsync(request, connectionTimeout, timeBudget.Remaining, cancellationToken).ConfigureAwait(false);

                if (response.Code == ResponseCode.Canceled)
                    throw new OperationCanceledException();

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (StreamAlreadyUsedException)
            {
                LogStreamReuseFailure();
                return Responses.StreamReuseFailure;
            }
            catch (ContentAlreadyUsedException)
            {
                LogContentReuseFailure();
                return Responses.ContentReuseFailure;
            }
            catch (Exception error)
            {
                LogTransportException(error);
                return Responses.UnknownFailure;
            }
        }

        #region Logging

        private void LogRequest(Uri replica, TimeSpan timeout) =>
            configuration.Log.Info("Sending request to replica '{Replica}' with timeout {Timeout}.", replica, timeout.ToPrettyString());

        private void LogResult(ReplicaResult result) =>
            configuration.Log.Info(
                "Result: replica = '{Replica}'; code = {ResponseCode:D} ('{ResponseCode}'); verdict = {Verdict}; time = {ElapsedTime}.", 
                new
                {
                    Replica = result.Replica,
                    ResponseCode = result.Response.Code,
                    Verdict = result.Verdict,
                    ElapsedTime = result.Time.ToPrettyString(),
                    ElapsedTimeMs = result.Time.TotalMilliseconds
                });

        private void LogStreamReuseFailure() =>
            configuration.Log.Warn("Detected an attempt to use request body stream more than once, which is not allowed.");

        private void LogContentReuseFailure() =>
            configuration.Log.Warn($"Detected an attempt to produce request content body stream more than once, which is not allowed because {nameof(IContentProducer.IsReusable)} set to false.");

        private void LogTransportException(Exception error) =>
            configuration.Log.Error(error, "Transport implementation threw an exception.");

        #endregion
    }
}