// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    internal class SimplificationCodeCleanupProvider : ICodeCleanupProvider
    {
        public string Name => PredefinedCodeCleanupProviderNames.Simplification;

        public Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
        {
            return Simplifier.ReduceAsync(document, spans, null, cancellationToken);
        }

        public Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            // Simplifier doesn't work without semantic information
            return Task.FromResult(root);
        }
    }
}
