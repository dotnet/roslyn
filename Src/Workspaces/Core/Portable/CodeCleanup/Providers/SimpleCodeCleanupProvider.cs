// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Func<Document, IEnumerable<TextSpan>, CancellationToken, Task<Document>> documentDelegatee;
        private readonly Func<SyntaxNode, IEnumerable<TextSpan>, Workspace, CancellationToken, SyntaxNode> syntaxDelegatee;

        public SimpleCodeCleanupProvider(string name,
            Func<Document, IEnumerable<TextSpan>, CancellationToken, Task<Document>> documentDelegatee = null,
            Func<SyntaxNode, IEnumerable<TextSpan>, Workspace, CancellationToken, SyntaxNode> syntaxDelegatee = null)
        {
            Contract.Requires(documentDelegatee != null || syntaxDelegatee != null);

            this.Name = name;
            this.documentDelegatee = documentDelegatee;
            this.syntaxDelegatee = syntaxDelegatee;
        }

        public string Name { get; private set; }

        public async Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            if (this.documentDelegatee != null)
            {
                return await this.documentDelegatee(document, spans, cancellationToken).ConfigureAwait(false);
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = this.syntaxDelegatee(root, spans, document.Project.Solution.Workspace, cancellationToken);

            if (root != newRoot)
            {
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        public SyntaxNode Cleanup(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            if (this.syntaxDelegatee != null)
            {
                return this.syntaxDelegatee(root, spans, workspace, cancellationToken);
            }

            return root;
        }
    }
}
