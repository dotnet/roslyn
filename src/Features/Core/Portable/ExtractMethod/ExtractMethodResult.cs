// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal sealed class ExtractMethodResult
    {
        /// <summary>
        /// True if the extract method operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The reasons why the extract method operation did not succeed.
        /// </summary>
        public ImmutableArray<string> Reasons { get; }

        private readonly AsyncLazy<(Document document, SyntaxToken? invocationNameToken)>? _lazyData;

        internal ExtractMethodResult(
            bool succeeded,
            ImmutableArray<string> reasons,
            Func<CancellationToken, Task<(Document document, SyntaxToken? invocationNameToken)>>? getDocumentAsync)
        {
            Succeeded = succeeded;

            Reasons = reasons.NullToEmpty();

            if (getDocumentAsync != null)
                _lazyData = AsyncLazy.Create(getDocumentAsync);
        }

        public static ExtractMethodResult Fail(OperationStatus status)
            => new(status.Succeeded, status.Reasons, getDocumentAsync: null);

        public static ExtractMethodResult Success(
            OperationStatus status,
            Func<CancellationToken, Task<(Document document, SyntaxToken? invocationNameToken)>> getDocumentAsync)
        {
            return new(status.Succeeded, status.Reasons, getDocumentAsync);
        }

        public Task<(Document document, SyntaxToken? invocationNameToken)> GetDocumentAsync(CancellationToken cancellationToken)
            => _lazyData!.GetValueAsync(cancellationToken);
    }
}
