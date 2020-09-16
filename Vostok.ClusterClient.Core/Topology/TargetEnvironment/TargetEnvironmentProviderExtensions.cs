using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Topology.TargetEnvironment
{
    public static class TargetEnvironmentProviderExtensions
    {
        [NotNull]
        public static string Get([NotNull] this ITargetEnvironmentProvider provider)
        {
            return provider.Find() ?? throw new Exception($"Environment provider of type {provider.GetType()} returned null.");
        }
    }
}