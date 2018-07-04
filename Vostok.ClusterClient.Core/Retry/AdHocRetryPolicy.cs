﻿using System;
using System.Collections.Generic;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Core.Retry
{
    /// <summary>
    /// Represents a retry policy which uses external predicate to make a decision.
    /// </summary>
    public class AdHocRetryPolicy : IRetryPolicy
    {
        private readonly Predicate<IList<ReplicaResult>> criterion;

        public AdHocRetryPolicy(Predicate<IList<ReplicaResult>> criterion)
        {
            this.criterion = criterion;
        }

        public bool NeedToRetry(IList<ReplicaResult> results) => criterion(results);
    }
}