// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineHints;

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
        ISyntaxFactsService syntaxFacts,
        SyntaxNode node,
        ArrayBuilder<(int position, SyntaxNode argument, IParameterSymbol? parameter, HintKind kind)> buffer,
        CancellationToken cancellationToken);

    protected abstract bool IsIndexer(SyntaxNode node, IParameterSymbol parameter);
    protected abstract string GetReplacementText(string parameterName);

    public async Task AddInlineHintsAsync(
        Document document,
        TextSpan textSpan,
        InlineParameterHintsOptions options,
        SymbolDescriptionOptions displayOptions,
        bool displayAllOverride,
        ArrayBuilder<InlineHint> result,
        CancellationToken cancellationToken)
    {
        var enabledForParameters = displayAllOverride || options.EnabledForParameters;
        if (!enabledForParameters)
            return;

        var literalParameters = displayAllOverride || options.ForLiteralParameters;
        var objectCreationParameters = displayAllOverride || options.ForObjectCreationParameters;
        var otherParameters = displayAllOverride || options.ForOtherParameters;
        if (!literalParameters && !objectCreationParameters && !otherParameters)
            return;

        var indexerParameters = displayAllOverride || options.ForIndexerParameters;
        var suppressForParametersThatDifferOnlyBySuffix = !displayAllOverride && options.SuppressForParametersThatDifferOnlyBySuffix;
        var suppressForParametersThatMatchMethodIntent = !displayAllOverride && options.SuppressForParametersThatMatchMethodIntent;
        var suppressForParametersThatMatchArgumentName = !displayAllOverride && options.SuppressForParametersThatMatchArgumentName;
        var suppressForParametersThatMatchMemberName = !displayAllOverride && options.SuppressForParametersThatMatchMemberName;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        using var _ = ArrayBuilder<(int position, SyntaxNode argument, IParameterSymbol? parameter, HintKind kind)>.GetInstance(out var buffer);

        foreach (var node in root.DescendantNodes(textSpan, n => n.Span.IntersectsWith(textSpan)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddAllParameterNameHintLocations(semanticModel, syntaxFacts, node, buffer, cancellationToken);

            if (buffer.Count > 0)
            {
                AddHintsIfAppropriate(node);
                buffer.Clear();
            }
        }

        return;

        void AddHintsIfAppropriate(SyntaxNode node)
        {
            if (suppressForParametersThatDifferOnlyBySuffix && ParametersDifferOnlyBySuffix(buffer))
                return;

            foreach (var (position, argument, parameter, kind) in buffer)
            {
                // We get hints on *nodes* that intersect the passed in text span.  However, while the full node may
                // intersect the span, the positions of the all the sub-nodes in it that we make hints for (like the
                // positions of the arguments in an invocation) may not.  So, filter out any hints that aren't actually
                // in the span we care about here.
                if (!textSpan.IntersectsWith(position))
                    continue;

                if (string.IsNullOrEmpty(parameter?.Name))
                    continue;

                if (suppressForParametersThatMatchMethodIntent && MatchesMethodIntent(parameter))
                    continue;

                if (suppressForParametersThatMatchArgumentName && ParameterMatchesArgumentName(argument, parameter, syntaxFacts))
                    continue;

                if (suppressForParametersThatDifferOnlyBySuffix && ParameterDiffersOnlyBySuffix(argument, parameter, syntaxFacts))
                    continue;

                if (suppressForParametersThatMatchMemberName && ParameterMatchesMemberAccessName(argument, parameter, syntaxFacts))
                    continue;

                if (!indexerParameters && IsIndexer(node, parameter))
                    continue;

                if (HintMatches(kind, literalParameters, objectCreationParameters, otherParameters))
                {
                    var textSpan = new TextSpan(position, 0);

                    TextChange? replacementTextChange = parameter.IsParams
                        ? null
                        : new TextChange(textSpan, GetReplacementText(parameter.Name));

                    result.Add(new InlineHint(
                        textSpan,
                        [new TaggedText(TextTags.Text, parameter.Name + ": ")],
                        replacementTextChange,
                        ranking: InlineHintsConstants.ParameterRanking,
                        InlineHintHelpers.GetDescriptionFunction(position, parameter, displayOptions)));
                }
            }
        }
    }

    private static bool ParametersDifferOnlyBySuffix(
        ArrayBuilder<(int position, SyntaxNode argument, IParameterSymbol? parameter, HintKind kind)> parameterHints)
    {
        // Only relevant if we have two or more parameters.
        if (parameterHints.Count <= 1)
            return false;

        return ParametersDifferOnlyByAlphaSuffix(parameterHints) ||
               ParametersDifferOnlyByNumericSuffix(parameterHints);

        static bool ParametersDifferOnlyByAlphaSuffix(
            ArrayBuilder<(int position, SyntaxNode argument, IParameterSymbol? parameter, HintKind kind)> parameterHints)
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
            ArrayBuilder<(int position, SyntaxNode argument, IParameterSymbol? parameter, HintKind kind)> parameterHints)
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
    {
        return kind switch
        {
            HintKind.Literal => literalParameters,
            HintKind.ObjectCreation => objectCreationParameters,
            HintKind.Other => otherParameters,
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    protected static bool MatchesMethodIntent(IParameterSymbol? parameter)
    {
        // Methods like `SetColor(color: "y")` `FromResult(result: "x")` `Enable/DisablePolling(bool)` don't need
        // parameter names to improve clarity.  The parameter is clear from the context of the method name.

        // First, this only applies to methods/local functions (as we're looking at the method name itself) so filter down to those.
        if (parameter is not { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.LocalFunction } method })
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

    private static bool ParameterMatchesArgumentName(SyntaxNode argument, IParameterSymbol parameter, ISyntaxFactsService syntaxFacts)
    {
        var argumentName = GetIdentifierNameFromArgument(argument, syntaxFacts);
        if (string.IsNullOrEmpty(argumentName))
            return false;

        // First, try exact match
        if (syntaxFacts.StringComparer.Compare(parameter.Name, argumentName) == 0)
            return true;

        // Try normalized comparison (case-insensitive and ignoring underscores)
        return NormalizeIdentifier(parameter.Name).Equals(NormalizeIdentifier(argumentName), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ParameterDiffersOnlyBySuffix(SyntaxNode argument, IParameterSymbol parameter, ISyntaxFactsService syntaxFacts)
    {
        var argumentName = GetIdentifierNameFromArgument(argument, syntaxFacts);
        if (string.IsNullOrEmpty(argumentName) || string.IsNullOrEmpty(parameter.Name))
            return false;

        // Check if parameter and argument differ only by a trailing underscore or single character suffix
        // Cases to handle:
        // 1. fooBar vs fooBar_
        // 2. fooBar_ vs fooBar
        // 3. fooBar vs fooBarX (where X is A-Z)
        // 4. fooBarX vs fooBar

        // Check if one is prefix of another with single character difference
        if (argumentName.Length == parameter.Name.Length + 1)
        {
            // argument is longer (e.g., fooBar_ vs fooBar)
            if (argumentName.StartsWith(parameter.Name, StringComparison.Ordinal))
            {
                var suffix = argumentName[^1];
                return suffix == '_' || IsUpperAlpha(suffix) || IsNumeric(suffix);
            }
        }
        else if (parameter.Name.Length == argumentName.Length + 1)
        {
            // parameter is longer (e.g., fooBar vs fooBar_)
            if (parameter.Name.StartsWith(argumentName, StringComparison.Ordinal))
            {
                var suffix = parameter.Name[^1];
                return suffix == '_' || IsUpperAlpha(suffix) || IsNumeric(suffix);
            }
        }

        return false;

        static bool IsUpperAlpha(char c) => char.IsUpper(c) && char.IsLetter(c);
        static bool IsNumeric(char c) => char.IsDigit(c);
    }

    private static bool ParameterMatchesMemberAccessName(SyntaxNode argument, IParameterSymbol parameter, ISyntaxFactsService syntaxFacts)
    {
        var memberName = GetMemberNameFromArgument(argument, syntaxFacts);
        if (string.IsNullOrEmpty(memberName))
            return false;

        // Check if member name matches parameter name (normalized, case-insensitive)
        return NormalizeIdentifier(parameter.Name).Equals(NormalizeIdentifier(memberName), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetMemberNameFromArgument(SyntaxNode argument, ISyntaxFactsService syntaxFacts)
    {
        var expression =
            syntaxFacts.IsArgument(argument) ? syntaxFacts.GetExpressionOfArgument(argument) :
            syntaxFacts.IsAttributeArgument(argument) ? syntaxFacts.GetExpressionOfAttributeArgument(argument) : null;

        // Check if the expression is a member access (e.g., foo.X)
        if (syntaxFacts.IsMemberAccessExpression(expression))
        {
            var memberNameSyntax = syntaxFacts.GetNameOfMemberAccessExpression(expression);
            if (syntaxFacts.IsIdentifierName(memberNameSyntax))
            {
                var identifier = syntaxFacts.GetIdentifierOfIdentifierName(memberNameSyntax);
                return identifier.ValueText;
            }
        }

        return string.Empty;
    }

    private static string NormalizeIdentifier(string identifier)
    {
        // Remove common prefixes and underscores for comparison
        var normalized = identifier.TrimStart('_', '@');
        return normalized.Replace("_", "");
    }

    protected static string GetIdentifierNameFromArgument(SyntaxNode argument, ISyntaxFactsService syntaxFacts)
    {
        var identifierNameSyntax =
            syntaxFacts.IsArgument(argument) ? syntaxFacts.GetExpressionOfArgument(argument) :
            syntaxFacts.IsAttributeArgument(argument) ? syntaxFacts.GetExpressionOfAttributeArgument(argument) : null;

        if (syntaxFacts.IsMemberAccessExpression(identifierNameSyntax))
        {
            identifierNameSyntax = syntaxFacts.GetNameOfMemberAccessExpression(identifierNameSyntax);
        }

        if (!syntaxFacts.IsIdentifierName(identifierNameSyntax))
            return string.Empty;

        var identifier = syntaxFacts.GetIdentifierOfIdentifierName(identifierNameSyntax);

        return identifier.ValueText;
    }
}
