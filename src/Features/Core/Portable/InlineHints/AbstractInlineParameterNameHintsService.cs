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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal abstract class AbstractInlineParameterNameHintsService : IInlineParameterNameHintsService
    {
        protected abstract void AddAllParameterNameHintLocations(
            SemanticModel semanticModel,
            SyntaxNode node,
            ArrayBuilder<InlineParameterHint> buffer,
            CancellationToken cancellationToken);

        public async Task<ImmutableArray<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var displayAllOverride = options.GetOption(InlineHintsOptions.DisplayAllOverride);

            var forParameters = displayAllOverride || options.GetOption(InlineHintsOptions.EnabledForParameters);
            if (!forParameters)
                return ImmutableArray<InlineParameterHint>.Empty;

            var literalParameters = displayAllOverride || options.GetOption(InlineHintsOptions.ForLiteralParameters);
            var objectCreationParameters = displayAllOverride || options.GetOption(InlineHintsOptions.ForObjectCreationParameters);
            var otherParameters = displayAllOverride || options.GetOption(InlineHintsOptions.ForOtherParameters);
            if (!literalParameters && !objectCreationParameters && !otherParameters)
                return ImmutableArray<InlineParameterHint>.Empty;

            var suppressForParametersThatDifferOnlyBySuffix = options.GetOption(InlineHintsOptions.SuppressForParametersThatDifferOnlyBySuffix);
            var suppressForParametersThatMatchMethodIntent = options.GetOption(InlineHintsOptions.SuppressForParametersThatMatchMethodIntent);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = ArrayBuilder<InlineParameterHint>.GetInstance(out var result);
            using var _2 = ArrayBuilder<InlineParameterHint>.GetInstance(out var buffer);

            foreach (var node in root.DescendantNodes(textSpan))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddAllParameterNameHintLocations(semanticModel, node, buffer, cancellationToken);

                if (buffer.Count > 0)
                {
                    AddHintsIfAppropriate();
                    buffer.Clear();
                }
            }

            return result.ToImmutable();

            void AddHintsIfAppropriate()
            {
                if (suppressForParametersThatDifferOnlyBySuffix && ParametersDifferOnlyBySuffix(buffer))
                    return;

                foreach (var hint in buffer)
                {
                    if (string.IsNullOrEmpty(hint.Parameter?.Name))
                        continue;

                    if (suppressForParametersThatMatchMethodIntent && MatchesMethodIntent(hint))
                        continue;

                    if (HintMatches(hint, literalParameters, objectCreationParameters, otherParameters))
                        result.Add(hint);
                }
            }
        }

        private static bool ParametersDifferOnlyBySuffix(ArrayBuilder<InlineParameterHint> parameterHints)
        {
            // Only relevant if we have two or more parameters.
            if (parameterHints.Count <= 1)
                return false;

            return ParametersDifferOnlyByAlphaSuffix(parameterHints) ||
                   ParametersDifferOnlyByNumericSuffix(parameterHints);

            static bool ParametersDifferOnlyByAlphaSuffix(ArrayBuilder<InlineParameterHint> parameterHints)
            {
                if (!HasAlphaSuffix(parameterHints[0], out var firstPrefix))
                    return false;

                for (var i = 1; i < parameterHints.Count; i++)
                {
                    if (!HasAlphaSuffix(parameterHints[i], out var nextPrefix))
                        return false;

                    if (!firstPrefix.Span.Equals(nextPrefix.Span, StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            static bool ParametersDifferOnlyByNumericSuffix(ArrayBuilder<InlineParameterHint> parameterHints)
            {
                if (!HasNumericSuffix(parameterHints[0], out var firstPrefix))
                    return false;

                for (var i = 1; i < parameterHints.Count; i++)
                {
                    if (!HasNumericSuffix(parameterHints[i], out var nextPrefix))
                        return false;

                    if (!firstPrefix.Span.Equals(nextPrefix.Span, StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            static bool HasAlphaSuffix(InlineParameterHint hint, out ReadOnlyMemory<char> prefix)
            {
                var name = hint.Parameter?.Name;

                // Has to end with A-Z
                // That A-Z can't be following another A-Z (that's just a capitalized word).
                if (name?.Length >= 2 &&
                    IsUpperAlpha(name[^1]) &&
                    !IsUpperAlpha(name[^2]))
                {
                    prefix = name.AsMemory()[..^1];
                    return true;
                }

                prefix = default;
                return false;
            }

            static bool HasNumericSuffix(InlineParameterHint hint, out ReadOnlyMemory<char> prefix)
            {
                var name = hint.Parameter?.Name;

                // Has to end with 0-9.  only handles single-digit numeric suffix for now for simplicity
                if (name?.Length >= 2 &&
                    IsNumeric(name[^1]))
                {
                    prefix = name.AsMemory()[..^1];
                    return true;
                }

                prefix = default;
                return false;
            }

            static bool IsUpperAlpha(char c)
                => c is >= 'A' and <= 'Z';

            static bool IsNumeric(char c)
                => c is >= '0' and <= '9';
        }

        private static bool HintMatches(InlineParameterHint hint, bool literalParameters, bool objectCreationParameters, bool otherParameters)
            => hint.Kind switch
            {
                InlineParameterHintKind.Literal => literalParameters,
                InlineParameterHintKind.ObjectCreation => objectCreationParameters,
                InlineParameterHintKind.Other => otherParameters,
                _ => throw ExceptionUtilities.UnexpectedValue(hint.Kind),
            };

        protected static bool MatchesMethodIntent(InlineParameterHint hint)
        {
            // Methods like `SetColor(color: "y")` `FromResult(result: "x")` `Enable/DisablePolling(bool)` don't need
            // parameter names to improve clarity.  The parameter is clear from the context of the method name.

            // First, this only applies to methods (as we're looking at the method name itself) so filter down to those.
            var parameter = hint.Parameter;
            if (parameter is not { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Ordinary } method })
                return false;

            // We only care when dealing with the first parameter.  Note: we don't have to worry parameter reordering
            // due to named-parameter use.  That's because this entire feature only works when we don't use
            // named-parameters.  So, by definition, the parameter/arg must be in the right location.
            if (method.Parameters[0] != parameter)
                return false;

            var methodName = method.Name;

            // Check for something like `EnableLogging(true)`
            if (TryGetSuffix("Enable", methodName, out _) ||
                TryGetSuffix("Disable", methodName, out _))
            {
                return parameter.Type.SpecialType == SpecialType.System_Boolean;
            }

            // More names can be added here if we find other patterns like this.
            if (TryGetSuffix("Set", methodName, out var suffix) ||
                TryGetSuffix("From", methodName, out suffix))
            {
                return SuffixMatchesParameterName(suffix, parameter.Name);
            }

            return false;

            static bool TryGetSuffix(string prefix, string nameValue, out ReadOnlyMemory<char> suffix)
            {
                if (nameValue.Length > prefix.Length &&
                    nameValue.StartsWith(prefix) &&
                    char.IsUpper(nameValue[prefix.Length]))
                {
                    suffix = nameValue.AsMemory()[prefix.Length..];
                    return true;
                }

                suffix = default;
                return false;
            }

            static bool SuffixMatchesParameterName(ReadOnlyMemory<char> suffix, string parameterName)
            {
                // Method's name will be something like 'FromResult', so 'suffix' will be 'Result' and parameterName
                // will be 'result'.  So we check if the first letters differ on case and the rest of the method
                // matches.
                return char.ToLower(suffix.Span[0]) == parameterName[0] &&
                       suffix.Span[1..].Equals(parameterName.AsSpan()[1..], StringComparison.Ordinal);
            }
        }
    }
}
