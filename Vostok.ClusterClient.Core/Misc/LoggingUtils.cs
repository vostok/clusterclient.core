using System;
using System.Linq;
using System.Text;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Misc
{
    internal static class LoggingUtils
    {
        public static void AppendQueryString(StringBuilder builder, Uri uri, RequestParametersLoggingSettings querySettings)
        {
            if (querySettings.IsEnabledForAllKeys())
            {
                builder.Append(uri.Query);
                return;
            }

            var writtenFirst = false;
            var requestUrlParser = new RequestUrlParser(uri.ToString());
            foreach (var pair in requestUrlParser.Where(kvp => querySettings.IsEnabledForKey(kvp.Key)))
            {
                if (!writtenFirst)
                {
                    builder.Append('?');
                    writtenFirst = true;
                }

                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(pair.Value);
            }
        }

        public static void AppendHeaders(StringBuilder builder, Headers headers, RequestParametersLoggingSettings headersSettings, bool singleLineManner)
        {
            var writtenFirst = false;
            foreach (var pair in headers)
            {
                if (!headersSettings.IsEnabledForKey(pair.Name))
                    continue;

                if (!writtenFirst)
                {
                    if (singleLineManner)
                    {
                        builder.Append(" ");
                    }
                    else
                    {
                        builder.AppendLine();
                    }

                    builder.Append("Headers:");
                    writtenFirst = true;
                }

                if (singleLineManner)
                {
                    builder.Append(" (");
                    builder.Append(pair.Name);
                    builder.Append('=');
                    builder.Append(pair.Value);
                    builder.Append(')');
                }
                else
                {
                    builder.AppendLine();
                    builder.Append('\t');
                    builder.Append(pair.Name);
                    builder.Append('=');
                    builder.Append(pair.Value);
                }
            }
        }
    }
}