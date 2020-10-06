// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineParameterNameHintsService : IInlineParameterNameHintsService
    {
        protected abstract void AddAllParameterNameHintLocations(
            SemanticModel semanticModel, SyntaxNode node, Action<InlineParameterHint> addHint,
            bool hideForParametersThatDifferBySuffix, bool hideForParametersThatMatchMethodIntent,
            CancellationToken cancellationToken);

        public async Task<ImmutableArray<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var forParameters = options.GetOption(InlineHintsOptions.EnabledForParameters);
            if (!forParameters)
                return ImmutableArray<InlineParameterHint>.Empty;

            var literalParameters = options.GetOption(InlineHintsOptions.ForLiteralParameters);
            var objectCreationParameters = options.GetOption(InlineHintsOptions.ForObjectCreationParameters);
            var otherParameters = options.GetOption(InlineHintsOptions.ForOtherParameters);
            if (!literalParameters && !objectCreationParameters && !otherParameters)
                return ImmutableArray<InlineParameterHint>.Empty;

            var hideForParametersThatDifferBySuffix = options.GetOption(InlineHintsOptions.HideForParametersThatDifferBySuffix);
            var hideForParametersThatMatchMethodIntent = options.GetOption(InlineHintsOptions.HideForParametersThatMatchMethodIntent);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = ArrayBuilder<InlineParameterHint>.GetInstance(out var result);

            Action<InlineParameterHint> addHint = AddHint;

            foreach (var node in root.DescendantNodes(textSpan))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddAllParameterNameHintLocations(
                    semanticModel, node, addHint,
                    hideForParametersThatDifferBySuffix,
                    hideForParametersThatMatchMethodIntent,
                    cancellationToken);
            }

            return result.ToImmutable();

            void AddHint(InlineParameterHint hint)
            {
                if (HintMatches(hint, literalParameters, objectCreationParameters, otherParameters))
                    result.Add(hint);
            }
        }

        private static bool HintMatches(InlineParameterHint hint, bool literalParameters, bool objectCreationParameters, bool otherParameters)
            => hint.Kind switch
            {
                InlineParameterHintKind.Literal => literalParameters,
                InlineParameterHintKind.ObjectCreation => objectCreationParameters,
                InlineParameterHintKind.Other => otherParameters,
                _ => throw ExceptionUtilities.UnexpectedValue(hint.Kind),
            };

        protected static bool MatchesMethodIntent(string methodName, IParameterSymbol parameter)
        {
            // Check for something like `EnableLogging(true)`
            if (TryGetIntent("Enable", methodName, out _) ||
                TryGetIntent("Disable", methodName, out _))
            {
                return parameter.Type.SpecialType == SpecialType.System_Boolean;
            }

            // More names can be added here if we find other patterns like this.
            if (TryGetIntent("Set", methodName, out var methodIntent) ||
                TryGetIntent("From", methodName, out methodIntent))
            {
                return IntentNameMatchesParameterName(methodIntent.Value, parameter.Name);
            }

            return false;

            static bool TryGetIntent(string prefix, string nameValue, [NotNullWhen(true)] out ReadOnlyMemory<char>? result)
            {
                if (nameValue.Length > prefix.Length &&
                    nameValue.StartsWith(prefix) &&
                    char.IsUpper(nameValue[prefix.Length]))
                {
                    result = nameValue.AsMemory().Slice(prefix.Length);
                    return true;
                }

                result = null;
                return false;
            }

            static bool IntentNameMatchesParameterName(ReadOnlyMemory<char> intent, string parameterName)
            {
                // Method's name will be something like 'FromResult', so 'intent' will be 'Result' and parameterName
                // will be 'result'.  So we check if the first letters differ on case and the rest of the method
                // matches.
                return char.ToLower(intent.Span[0]) == parameterName[0] &&
                       intent.Span.Slice(1).Equals(parameterName.AsSpan().Slice(1), StringComparison.Ordinal);
            }
        }
    }
}
