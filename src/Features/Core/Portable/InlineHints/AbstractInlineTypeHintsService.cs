// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineTypeHintsService : IInlineTypeHintsService
    {
        protected abstract InlineTypeHint? TryGetTypeHint(
            SemanticModel semanticModel, SyntaxNode node,
            bool forImplicitVariableTypes,
            bool forLambdaParameterTypes,
            CancellationToken cancellationToken);

        public async Task<ImmutableArray<InlineTypeHint>> GetInlineTypeHintsAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var displayAllOverride = options.GetOption(InlineHintsOptions.DisplayAllOverride);
            var enabledForTypes = displayAllOverride || options.GetOption(InlineHintsOptions.EnabledForTypes);
            if (!enabledForTypes)
                return ImmutableArray<InlineTypeHint>.Empty;

            var forImplicitVariableTypes = displayAllOverride || options.GetOption(InlineHintsOptions.ForImplicitVariableTypes);
            var forLambdaParameterTypes = displayAllOverride || options.GetOption(InlineHintsOptions.ForLambdaParameterTypes);
            if (!forImplicitVariableTypes && !forLambdaParameterTypes)
                return ImmutableArray<InlineTypeHint>.Empty;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<InlineTypeHint>.GetInstance(out var result);

            foreach (var node in root.DescendantNodes(n => n.Span.IntersectsWith(textSpan)))
            {
                result.AddIfNotNull(TryGetTypeHint(
                    semanticModel, node,
                    forImplicitVariableTypes,
                    forLambdaParameterTypes, cancellationToken));
            }

            return result.ToImmutable();
        }
    }
}
