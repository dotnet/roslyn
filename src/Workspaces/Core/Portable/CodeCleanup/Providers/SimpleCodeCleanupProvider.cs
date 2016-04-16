// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    /// <summary>
    /// Helper class that implements <see cref="ICodeCleanupProvider"/> using delegates passed to its constructor.
    /// </summary>
    internal class SimpleCodeCleanupProvider : ICodeCleanupProvider
    {
        private readonly Func<Document, IEnumerable<TextSpan>, CancellationToken, Task<Document>> _documentDelegatee;
        private readonly Func<SyntaxNode, IEnumerable<TextSpan>, Workspace, CancellationToken, SyntaxNode> _syntaxDelegatee;

        public SimpleCodeCleanupProvider(string name,
            Func<Document, IEnumerable<TextSpan>, CancellationToken, Task<Document>> documentDelegatee = null,
            Func<SyntaxNode, IEnumerable<TextSpan>, Workspace, CancellationToken, SyntaxNode> syntaxDelegatee = null)
        {
            Contract.Requires(documentDelegatee != null || syntaxDelegatee != null);

            this.Name = name;
            _documentDelegatee = documentDelegatee;
            _syntaxDelegatee = syntaxDelegatee;
        }

        public string Name { get; }

        public async Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            if (_documentDelegatee != null)
            {
                return await _documentDelegatee(document, spans, cancellationToken).ConfigureAwait(false);
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = _syntaxDelegatee(root, spans, document.Project.Solution.Workspace, cancellationToken);

            if (root != newRoot)
            {
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        public Task<SyntaxNode> CleanupAsync(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            if (_syntaxDelegatee != null)
            {
                return Task.FromResult(_syntaxDelegatee(root, spans, workspace, cancellationToken));
            }

            return Task.FromResult(root);
        }
    }
}
