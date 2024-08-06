using System;
using Vostok.Commons.Time;

namespace Vostok.Clusterclient.Core.Misc;

internal static class TimeSpanExtensions
{
    public static TimeSpan? SelectConnectionTimeoutForLastAttempt(TimeSpan lastAttemptConnectionTimeBudget, TimeSpan? connectionTimeoutFromParameters)
    {
        return connectionTimeoutFromParameters == null 
            ? lastAttemptConnectionTimeBudget 
            : TimeSpanArithmetics.Max(lastAttemptConnectionTimeBudget, connectionTimeoutFromParameters.Value);
    }
}