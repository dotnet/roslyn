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
        protected enum HintKind
        {
            Literal,
            ObjectCreation,
            Other
        }

        protected abstract void AddAllParameterNameHintLocations(
            SemanticModel semanticModel,
            SyntaxNode node,
            ArrayBuilder<(int position, IParameterSymbol? parameter, HintKind kind)> buffer,
            CancellationToken cancellationToken);

        public async Task<ImmutableArray<InlineHint>> GetInlineHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var displayAllOverride = options.GetOption(InlineHintsOptions.DisplayAllOverride);

            var enabledForParameters = displayAllOverride || options.GetOption(InlineHintsOptions.EnabledForParameters);
            if (!enabledForParameters)
                return ImmutableArray<InlineHint>.Empty;

            var literalParameters = displayAllOverride || options.GetOption(InlineHintsOptions.ForLiteralParameters);
            var objectCreationParameters = displayAllOverride || options.GetOption(InlineHintsOptions.ForObjectCreationParameters);
            var otherParameters = displayAllOverride || options.GetOption(InlineHintsOptions.ForOtherParameters);
            if (!literalParameters && !objectCreationParameters && !otherParameters)
                return ImmutableArray<InlineHint>.Empty;

            var suppressForParametersThatDifferOnlyBySuffix = !displayAllOverride && options.GetOption(InlineHintsOptions.SuppressForParametersThatDifferOnlyBySuffix);
            var suppressForParametersThatMatchMethodIntent = !displayAllOverride && options.GetOption(InlineHintsOptions.SuppressForParametersThatMatchMethodIntent);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            using var _1 = ArrayBuilder<InlineHint>.GetInstance(out var result);
            using var _2 = ArrayBuilder<(int position, IParameterSymbol? parameter, HintKind kind)>.GetInstance(out var buffer);

            foreach (var node in root.DescendantNodes(textSpan, n => n.Span.IntersectsWith(textSpan)))
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

                foreach (var (position, parameter, kind) in buffer)
                {
                    if (string.IsNullOrEmpty(parameter?.Name))
                        continue;

                    if (suppressForParametersThatMatchMethodIntent && MatchesMethodIntent(parameter))
                        continue;

                    if (HintMatches(kind, literalParameters, objectCreationParameters, otherParameters))
                    {
                        result.Add(new InlineHint(
                            new TextSpan(position, 0),
                            ImmutableArray.Create(new TaggedText(TextTags.Text, parameter.Name + ": ")),
                            InlineHintHelpers.GetDescriptionFunction(position, parameter.GetSymbolKey(cancellationToken: cancellationToken))));
                    }
                }
            }
        }

        private static bool ParametersDifferOnlyBySuffix(
            ArrayBuilder<(int position, IParameterSymbol? parameter, HintKind kind)> parameterHints)
        {
            // Only relevant if we have two or more parameters.
            if (parameterHints.Count <= 1)
                return false;

            return ParametersDifferOnlyByAlphaSuffix(parameterHints) ||
                   ParametersDifferOnlyByNumericSuffix(parameterHints);

            static bool ParametersDifferOnlyByAlphaSuffix(
                ArrayBuilder<(int position, IParameterSymbol? parameter, HintKind kind)> parameterHints)
            {
                if (!HasAlphaSuffix(parameterHints[0].parameter, out var firstPrefix))
                    return false;

                for (var i = 1; i < parameterHints.Count; i++)
                {
                    if (!HasAlphaSuffix(parameterHints[i].parameter, out var nextPrefix))
                        return false;

                    if (!firstPrefix.Span.Equals(nextPrefix.Span, StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            static bool ParametersDifferOnlyByNumericSuffix(
                ArrayBuilder<(int position, IParameterSymbol? parameter, HintKind kind)> parameterHints)
            {
                if (!HasNumericSuffix(parameterHints[0].parameter, out var firstPrefix))
                    return false;

                for (var i = 1; i < parameterHints.Count; i++)
                {
                    if (!HasNumericSuffix(parameterHints[i].parameter, out var nextPrefix))
                        return false;

                    if (!firstPrefix.Span.Equals(nextPrefix.Span, StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            static bool HasAlphaSuffix(IParameterSymbol? parameter, out ReadOnlyMemory<char> prefix)
            {
                var name = parameter?.Name;

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

            static bool HasNumericSuffix(IParameterSymbol? parameter, out ReadOnlyMemory<char> prefix)
            {
                var name = parameter?.Name;

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

        private static bool HintMatches(HintKind kind, bool literalParameters, bool objectCreationParameters, bool otherParameters)
            => kind switch
            {
                HintKind.Literal => literalParameters,
                HintKind.ObjectCreation => objectCreationParameters,
                HintKind.Other => otherParameters,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };

        protected static bool MatchesMethodIntent(IParameterSymbol? parameter)
        {
            // Methods like `SetColor(color: "y")` `FromResult(result: "x")` `Enable/DisablePolling(bool)` don't need
            // parameter names to improve clarity.  The parameter is clear from the context of the method name.

            // First, this only applies to methods (as we're looking at the method name itself) so filter down to those.
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
