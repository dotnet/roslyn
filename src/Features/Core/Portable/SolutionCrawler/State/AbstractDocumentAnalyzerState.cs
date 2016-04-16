// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return value.Id;
        }

        protected override Solution GetSolution(Document value)
        {
            return value.Project.Solution;
        }

        protected override bool ShouldCache(Document value)
        {
            return value.IsOpen();
        }

        protected override Task<Stream> ReadStreamAsync(IPersistentStorage storage, Document value, CancellationToken cancellationToken)
        {
            return storage.ReadStreamAsync(value, StateName, cancellationToken);
        }

        protected override Task<bool> WriteStreamAsync(IPersistentStorage storage, Document value, Stream stream, CancellationToken cancellationToken)
        {
            return storage.WriteStreamAsync(value, StateName, stream, cancellationToken);
        }
    }
}
