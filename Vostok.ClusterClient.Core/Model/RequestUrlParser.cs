using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Core.Model;

internal readonly struct RequestUrlParser
{
    private readonly Dictionary<string, string> query = new();

    public readonly string Path = null;

    public RequestUrlParser([CanBeNull] string url)
    {
        if (!RequestUrlParsingHelpers.TryParseUrlPath(url, out Path, out var question))
            return;

        if (!question.HasValue)
            return;

        var parameters = url!.Substring(question.Value + 1).Split('&');
        foreach (var parameter in parameters)
        {
            var tokens = parameter.Split('=');
            query[Uri.UnescapeDataString(tokens[0])] =
                tokens.Length > 1 && !string.IsNullOrEmpty(tokens[1]) ? Uri.UnescapeDataString(tokens[1]) : string.Empty;
        }
    }

    public bool TryGetQueryParameter([CanBeNull] string key, out string value)
    {
        if (key == null)
        {
            value = null;
            return false;
        }

        return query.TryGetValue(key, out value);
    }

    public IEnumerable<KeyValuePair<string, string>> GetQueryParameters() =>
        query;
}