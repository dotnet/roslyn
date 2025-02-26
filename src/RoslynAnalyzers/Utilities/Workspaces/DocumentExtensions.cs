// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !MICROSOFT_CODEANALYSIS_PUBLIC_API_ANALYZERS

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal static class DocumentExtensions
    {
        public static async ValueTask<SemanticModel> GetRequiredSemanticModelAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.TryGetSemanticModel(out var semanticModel))
                return semanticModel;

            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel ?? throw new InvalidOperationException("SyntaxTree is required to accomplish the task but is not supported by document");
        }

        public static async ValueTask<SyntaxTree> GetRequiredSyntaxTreeAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.TryGetSyntaxTree(out var syntaxTree))
                return syntaxTree;

            syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return syntaxTree ?? throw new InvalidOperationException("SyntaxTree is required to accomplish the task but is not supported by document");
        }

        public static async ValueTask<SyntaxNode> GetRequiredSyntaxRootAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.TryGetSyntaxRoot(out var root))
                return root;

            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root ?? throw new InvalidOperationException("SyntaxTree is required to accomplish the task but is not supported by document");
        }
    }
}

#endif
