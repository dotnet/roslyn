// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.AnalyzerPowerPack
{
    public abstract class MultipleCodeFixProviderBase : CodeFixProvider
    {
        internal abstract Task<IEnumerable<CodeAction>> GetFixesAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nodeToFix = root.FindNode(diagnostic.Location.SourceSpan);

                var newActions = await GetFixesAsync(document, model, root, nodeToFix, cancellationToken).ConfigureAwait(false);

                if (newActions != null)
                {
                    foreach (var a in newActions)
                    {
                        context.RegisterCodeFix(a, diagnostic);
                    }
                }
            }
        }
    }
}
