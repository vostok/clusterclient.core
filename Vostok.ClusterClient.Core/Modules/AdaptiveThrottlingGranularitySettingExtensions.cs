using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Core.Modules;

[PublicAPI]
public static class AdaptiveThrottlingGranularitySettingExtensions {
    public static RequestParameters WithAdaptiveThrottlingGranularity(this RequestParameters parameters, IReadOnlyDictionary<string, string> additionalGranularityKeys)
    {
        var immutableGranularity = new ImmutableArrayDictionary<string, string>(additionalGranularityKeys);
        return parameters.WithProperty(AdaptiveThrottlingModule.RequestParametersStatisticsGranularityPropertyKey, immutableGranularity);
    }

    public static void SetAdaptiveThrottlingGranularity(this IRequestContext context, IReadOnlyDictionary<string, string> additionalGranularityKeys)
    {
        context.Parameters = context.Parameters.WithAdaptiveThrottlingGranularity(additionalGranularityKeys);
    }
}