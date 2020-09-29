// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    /// <summary>
    /// Helper class that implements <see cref="ICodeCleanupProvider"/> using delegates passed to its constructor.
    /// </summary>
    internal class SimpleCodeCleanupProvider : ICodeCleanupProvider
    {
        private readonly Func<Document, ImmutableArray<TextSpan>, CancellationToken, Task<Document>>? _documentDelegatee;
        private readonly Func<SyntaxNode, ImmutableArray<TextSpan>, Workspace, CancellationToken, SyntaxNode>? _syntaxDelegatee;

        public SimpleCodeCleanupProvider(string name,
            Func<Document, ImmutableArray<TextSpan>, CancellationToken, Task<Document>>? documentDelegatee = null,
            Func<SyntaxNode, ImmutableArray<TextSpan>, Workspace, CancellationToken, SyntaxNode>? syntaxDelegatee = null)
        {
            Debug.Assert(documentDelegatee != null || syntaxDelegatee != null);

            this.Name = name;
            _documentDelegatee = documentDelegatee;
            _syntaxDelegatee = syntaxDelegatee;
        }

        public string Name { get; }

        public Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
        {
            if (_documentDelegatee != null)
            {
                return _documentDelegatee(document, spans, cancellationToken);
            }

            return CleanupCoreAsync(document, spans, cancellationToken);
        }

        private async Task<Document> CleanupCoreAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
        {
            RoslynDebug.AssertNotNull(_syntaxDelegatee);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = _syntaxDelegatee(root, spans, document.Project.Solution.Workspace, cancellationToken);

            if (root != newRoot)
            {
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        public Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            if (_syntaxDelegatee != null)
            {
                return Task.FromResult(_syntaxDelegatee(root, spans, workspace, cancellationToken));
            }

            return Task.FromResult(root);
        }
    }
}
