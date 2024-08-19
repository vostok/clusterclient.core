using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Misc
{
    internal static class RequestParametersLoggingSettingsExtensions
    {
        public static bool IsEnabledForAllKeys([NotNull] this RequestParametersLoggingSettings settings) =>
            IsEmpty(settings.Whitelist) && IsEmpty(settings.Blacklist);

        public static bool IsEnabledForKey([NotNull] this RequestParametersLoggingSettings settings, [NotNull] string key)
        {
            if (IsEmpty(settings.Whitelist))
                return IsEmpty(settings.Blacklist) || !settings.Blacklist!.Contains(key);

            return settings.Whitelist!.Contains(key);
        }

        public static RequestParametersLoggingSettings ToCaseInsensitive([CanBeNull] this RequestParametersLoggingSettings settings)
            => settings == null
                ? null
                : new RequestParametersLoggingSettings(settings.Enabled)
                {
                    Whitelist = IsEmpty(settings.Whitelist) ? null : new HashSet<string>(settings.Whitelist!, StringComparer.OrdinalIgnoreCase),
                    Blacklist = IsEmpty(settings.Blacklist) ? null : new HashSet<string>(settings.Blacklist!, StringComparer.OrdinalIgnoreCase)
                };

        private static bool IsEmpty(IReadOnlyCollection<string> collection) => collection == null || collection.Count == 0;
    }
}