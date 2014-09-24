// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers
{
    public abstract class MultipleCodeFixProviderBase : ICodeFixProvider
    {
        public abstract IEnumerable<string> GetFixableDiagnosticIds();

        public virtual FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        internal abstract Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken);

        async Task<IEnumerable<CodeAction>> ICodeFixProvider.GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var actions = SpecializedCollections.EmptyEnumerable<CodeAction>();
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nodeToFix = root.FindNode(diagnostic.Location.SourceSpan);

                var newActions = await GetFixesAsync(document, model, root, nodeToFix, cancellationToken).ConfigureAwait(false);

                if (newActions != null)
                {
                    actions = actions.Concat(newActions);
                }
            }

            return actions;
        }
    }
}
