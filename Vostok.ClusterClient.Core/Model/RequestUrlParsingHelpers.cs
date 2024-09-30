using System;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model
{
    internal static class RequestUrlParsingHelpers
    {
        public static bool TryParseUrlPath([CanBeNull] string url, out string path, out int? questionInd)
        {
            path = null;
            questionInd = null;

            if (url == null)
                return false;

            var question = url.IndexOf("?", StringComparison.Ordinal);
            if (question < 0)
            {
                path = url;
                return true;
            }

            path = url.Substring(0, question);
            questionInd = question;

            return true;
        }
    }
}