// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection
{
    internal abstract partial class AbstractCaseCorrectionService : ICaseCorrectionService
    {
        protected abstract void AddReplacements(SemanticModel? semanticModel, SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, ConcurrentDictionary<SyntaxToken, SyntaxToken> replacements, CancellationToken cancellationToken);

        public async Task<Document> CaseCorrectAsync(Document document, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
        {
            if (!spans.Any())
            {
                return document;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                throw new NotSupportedException(WorkspacesResources.Document_does_not_support_syntax_trees);
            }

            var semanticModel = await document.GetSemanticModelForSpanAsync(spans.Collapse(), cancellationToken).ConfigureAwait(false);

            var newRoot = CaseCorrect(semanticModel, root, spans, document.Project.Solution.Workspace, cancellationToken);
            return (root == newRoot) ? document : document.WithSyntaxRoot(newRoot);
        }

        public SyntaxNode CaseCorrect(SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            return CaseCorrect(semanticModel: null, root, spans, workspace, cancellationToken);
        }

        private SyntaxNode CaseCorrect(SemanticModel? semanticModel, SyntaxNode root, ImmutableArray<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CaseCorrection_CaseCorrect, cancellationToken))
            {
                var normalizedSpanCollection = new NormalizedTextSpanCollection(spans);
                var replacements = new ConcurrentDictionary<SyntaxToken, SyntaxToken>();

                using (Logger.LogBlock(FunctionId.CaseCorrection_AddReplacements, cancellationToken))
                {
                    AddReplacements(semanticModel, root, normalizedSpanCollection.ToImmutableArray(), workspace, replacements, cancellationToken);
                }

                using (Logger.LogBlock(FunctionId.CaseCorrection_ReplaceTokens, cancellationToken))
                {
                    return root.ReplaceTokens(replacements.Keys, (oldToken, _) => replacements[oldToken]);
                }
            }
        }
    }
}
