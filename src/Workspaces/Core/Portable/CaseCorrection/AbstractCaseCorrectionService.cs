// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CaseCorrection
{
    internal abstract partial class AbstractCaseCorrectionService : ICaseCorrectionService
    {
        protected abstract void AddReplacements(SemanticModel? semanticModel, SyntaxNode root, ImmutableArray<TextSpan> spans, ConcurrentDictionary<SyntaxToken, SyntaxToken> replacements, CancellationToken cancellationToken);

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

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(spans.Collapse(), cancellationToken).ConfigureAwait(false);

            var newRoot = CaseCorrect(semanticModel, root, spans, cancellationToken);
            return (root == newRoot) ? document : document.WithSyntaxRoot(newRoot);
        }

        public SyntaxNode CaseCorrect(SyntaxNode root, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
            => CaseCorrect(semanticModel: null, root, spans, cancellationToken);

        private SyntaxNode CaseCorrect(SemanticModel? semanticModel, SyntaxNode root, ImmutableArray<TextSpan> spans, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CaseCorrection_CaseCorrect, cancellationToken))
            {
                var normalizedSpanCollection = new NormalizedTextSpanCollection(spans);
                var replacements = new ConcurrentDictionary<SyntaxToken, SyntaxToken>();

                using (Logger.LogBlock(FunctionId.CaseCorrection_AddReplacements, cancellationToken))
                {
                    AddReplacements(semanticModel, root, normalizedSpanCollection.ToImmutableArray(), replacements, cancellationToken);
                }

                using (Logger.LogBlock(FunctionId.CaseCorrection_ReplaceTokens, cancellationToken))
                {
                    return root.ReplaceTokens(replacements.Keys, (oldToken, _) => replacements[oldToken]);
                }
            }
        }
    }
}
