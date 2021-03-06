﻿using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Core.Criteria
{
    /// <summary>
    /// Represents a criterion which accepts any response with given header.
    /// </summary>
    [PublicAPI]
    public class AcceptHeaderCriterion : IResponseCriterion
    {
        private readonly string headerName;

        public AcceptHeaderCriterion([NotNull] string headerName)
            => this.headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));

        /// <inheritdoc />
        public ResponseVerdict Decide(Response response) =>
            response.Headers[headerName] != null ? ResponseVerdict.Accept : ResponseVerdict.DontKnow;
    }
}
