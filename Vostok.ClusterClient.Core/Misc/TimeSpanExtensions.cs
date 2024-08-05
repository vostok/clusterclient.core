using System;
using Vostok.Commons.Time;

namespace Vostok.Clusterclient.Core.Misc;

public static class TimeSpanExtensions
{
    public static TimeSpan? Max(TimeSpan lastAttemptConnectionTimeBudget, TimeSpan? connectionTimeoutFromParameters)
    {
        return connectionTimeoutFromParameters == null 
            ? lastAttemptConnectionTimeBudget 
            : TimeSpanArithmetics.Max(lastAttemptConnectionTimeBudget, connectionTimeoutFromParameters.Value);
    }
}