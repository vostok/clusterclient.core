using System;
using System.Linq;
using System.Text;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Misc
{
    internal static class LoggingUtils
    {
        public static void AppendQueryString(StringBuilder builder, Uri uri, RequestParametersLoggingSettings querySettings, RequestUrlParser requestUrlParser)
        {
            var writtenFirst = false;
            foreach (var pair in requestUrlParser.GetQueryParameters().Where(kvp => querySettings.IsEnabledForKey(kvp.Key)))
            {
                if (!writtenFirst)
                {
                    builder.Append('?');
                    writtenFirst = true;
                }
                else
                {
                    builder.Append('&');
                }

                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(pair.Value);
            }
        }

        public static void AppendHeaders(StringBuilder builder, Headers headers, RequestParametersLoggingSettings headersSettings, bool singleLineManner, bool appendTitle)
        {
            var writtenFirst = false;
            var addDelimiter = false;
            foreach (var pair in headers)
            {
                if (!headersSettings.IsEnabledForKey(pair.Name))
                    continue;

                if (!writtenFirst && appendTitle)
                {
                    if (singleLineManner)
                    {
                        builder.Append(" Headers: (");
                    }

                    writtenFirst = true;
                }

                if (singleLineManner)
                {
                    if (addDelimiter)
                        builder.Append(", ");

                    builder.Append(pair.Name);
                    builder.Append('=');
                    builder.Append(pair.Value);

                    addDelimiter = true;
                }
                else
                {
                    builder.AppendLine();
                    builder.Append(pair.Name);
                    builder.Append('=');
                    builder.Append(pair.Value);
                }
            }

            if (singleLineManner && writtenFirst)
                builder.Append(')');
        }
    }
}