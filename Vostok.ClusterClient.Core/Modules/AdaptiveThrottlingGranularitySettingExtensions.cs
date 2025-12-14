using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Core.Modules;

[PublicAPI]
public static class AdaptiveThrottlingGranularitySettingExtensions {
    public static RequestParameters SetAdaptiveThrottlingGranularity(this RequestParameters parameters, IReadOnlyDictionary<string, string> additionalGranularity)
    {
        var immutableGranularity = new ImmutableArrayDictionary<string, string>(additionalGranularity);
        return parameters.WithProperty(AdaptiveThrottlingModule.RequestParametersStatisticsGranularityPropertyKey, immutableGranularity);
    }

    public static RequestParameters WithAdaptiveThrottlingGranularity(this RequestParameters parameters, string key, string value)
    {
        ImmutableArrayDictionary<string, string> granularity;
        if (parameters.Properties.TryGetValue(AdaptiveThrottlingModule.RequestParametersStatisticsGranularityPropertyKey, out var obj) &&
            obj is ImmutableArrayDictionary<string, string> presentGranularity)
        {
            granularity = presentGranularity.Set(key, value);
            if (granularity == presentGranularity)
                return parameters;
        }
        else
        {
            granularity = new ImmutableArrayDictionary<string, string>();
            granularity.AppendUnsafe(key, value);
        }

        
        return parameters.WithProperty(AdaptiveThrottlingModule.RequestParametersStatisticsGranularityPropertyKey, granularity);
    }
    
    public static void SetAdaptiveThrottlingGranularity(this IRequestContext context, IReadOnlyDictionary<string, string> additionalGranularity)
    {
        context.Parameters = context.Parameters.SetAdaptiveThrottlingGranularity(additionalGranularity);
    }
}