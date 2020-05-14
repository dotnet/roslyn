// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SolutionCrawler.State
{
    internal abstract class AbstractDocumentAnalyzerState<T> : AbstractAnalyzerState<DocumentId, Document, T>
    {
        protected abstract string StateName { get; }

        protected override DocumentId GetCacheKey(Document value)
            => value.Id;

        protected override Solution GetSolution(Document value)
            => value.Project.Solution;

        protected override bool ShouldCache(Document value)
            => value.IsOpen();

        protected override Task<Stream> ReadStreamAsync(IPersistentStorage storage, Document value, CancellationToken cancellationToken)
            => storage.ReadStreamAsync(value, StateName, cancellationToken);

        protected override Task<bool> WriteStreamAsync(IPersistentStorage storage, Document value, Stream stream, CancellationToken cancellationToken)
            => storage.WriteStreamAsync(value, StateName, stream, cancellationToken);
    }
}
