using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model
{
    internal static class RequestUrlParsingHelpers
    {
        public static bool TryParseUrlPath([CanBeNull] string url, out string path, out int questionInd)
        {
            path = null;
            questionInd = -1;

            if (url == null)
                return false;

            questionInd = url.IndexOf("?", StringComparison.Ordinal);
            if (questionInd < 0)
            {
                path = url;
                return true;
            }

            path = url.Substring(0, questionInd);

            return true;
        }
    }
}