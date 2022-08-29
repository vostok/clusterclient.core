using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using StringComparison = System.StringComparison;

namespace Vostok.Clusterclient.Core.Model;

internal readonly struct RequestUrlParser
{
    private readonly Dictionary<string, string> query = new();

    public RequestUrlParser([CanBeNull] string url)
    {
        if (url == null)
            return;
            
        var question = url.IndexOf("?", StringComparison.Ordinal);
        if (question < 0)
            return;
        url = url.Substring(question + 1);

        var parameters = url.Split('&');
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
}