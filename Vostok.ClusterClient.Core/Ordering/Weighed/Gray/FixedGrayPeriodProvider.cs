﻿using System;

namespace Vostok.ClusterClient.Core.Ordering.Weighed.Gray
{
    /// <summary>
    /// Represents a simple gray period provider which always return a given fixed value.
    /// </summary>
    public class FixedGrayPeriodProvider : IGrayPeriodProvider
    {
        private readonly TimeSpan grayPeriod;

        public FixedGrayPeriodProvider(TimeSpan grayPeriod)
        {
            this.grayPeriod = grayPeriod;
        }

        public TimeSpan GetGrayPeriod() => grayPeriod;
    }
}