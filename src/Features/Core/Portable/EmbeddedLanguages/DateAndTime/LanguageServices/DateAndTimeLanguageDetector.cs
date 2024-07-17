// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime.LanguageServices;

/// <summary>
/// Helper class to detect <see cref="DateTime"/> and <see cref="DateTimeOffset"/> format strings in a document efficiently.
/// </summary>
internal sealed class DateAndTimeLanguageDetector(
    EmbeddedLanguageInfo info,
    INamedTypeSymbol? dateTimeType,
    INamedTypeSymbol? dateTimeOffsetType)
    : AbstractLanguageDetector<DateAndTimeOptions, DateTimeTree, DateAndTimeLanguageDetector, DateAndTimeLanguageDetector.DateAndTimeInfo>(
        info, LanguageIdentifiers, CommentDetector)
{
    internal readonly struct DateAndTimeInfo : ILanguageDetectorInfo<DateAndTimeLanguageDetector>
    {
        public ImmutableArray<string> LanguageIdentifiers => ["Date", "Time", "DateTime", "DateTimeFormat"];

        public DateAndTimeLanguageDetector Create(Compilation compilation, EmbeddedLanguageInfo info)
        {
            var dateTimeType = compilation.GetTypeByMetadataName(typeof(DateTime).FullName!);
            var dateTimeOffsetType = compilation.GetTypeByMetadataName(typeof(DateTimeOffset).FullName!);

            return new DateAndTimeLanguageDetector(info, dateTimeType, dateTimeOffsetType);
        }
    }

    private const string FormatName = "format";

    private readonly INamedTypeSymbol? _dateTimeType = dateTimeType;
    private readonly INamedTypeSymbol? _dateTimeOffsetType = dateTimeOffsetType;

    protected override bool TryGetOptions(SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out DateAndTimeOptions options)
    {
        // DateTime never has any options.  So just return empty and 'true' so we stop processing immediately.
        options = default;
        return true;
    }

    protected override DateTimeTree? TryParse(VirtualCharSequence chars, DateAndTimeOptions options)
    {
        // Once we've determined something is a DateTime string, then parsing is a no-op.  We just return a dummy
        // instance to satisfy the detector requirements.
        return DateTimeTree.Instance;
    }

    protected override bool IsEmbeddedLanguageInterpolatedStringTextToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var syntaxFacts = Info.SyntaxFacts;
        var interpolationFormatClause = token.Parent;
        var interpolation = interpolationFormatClause?.Parent;
        if (interpolation?.RawKind != syntaxFacts.SyntaxKinds.Interpolation)
            return false;

        var expression = syntaxFacts.GetExpressionOfInterpolation(interpolation);
        var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        return IsDateTimeType(type);
    }

    protected override bool IsArgumentToWellKnownAPI(
        SyntaxToken token,
        SyntaxNode argumentNode,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out DateAndTimeOptions options)
    {
        options = default;

        var argumentList = argumentNode.Parent;
        var invocationOrCreation = argumentList?.Parent;

        var syntaxFacts = Info.SyntaxFacts;
        if (!syntaxFacts.IsInvocationExpression(invocationOrCreation))
            return false;

        var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(invocationOrCreation);
        var name = GetNameOfInvokedExpression(syntaxFacts, invokedExpression);
        if (name is not nameof(ToString) and not nameof(DateTime.ParseExact) and not nameof(DateTime.TryParseExact))
            return false;

        // We have a string literal passed to a method called ToString/ParseExact/TryParseExact.
        // Have to do a more expensive semantic check now.

        // if we couldn't determine the arg name or arg index, can't proceed.
        var (argName, argIndex) = GetArgumentNameOrIndex(argumentNode);
        if (argName == null && argIndex == null)
            return false;

        // If we had a specified arg name and it isn't 'format', then it's not a DateTime
        // 'format' param we care about.
        if (argName is not null and not FormatName)
            return false;

        var symbolInfo = semanticModel.GetSymbolInfo(invocationOrCreation, cancellationToken);
        var method = symbolInfo.Symbol;
        if (TryAnalyzeInvocation(method, argName, argIndex))
            return true;

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (TryAnalyzeInvocation(candidate, argName, argIndex))
                return true;
        }

        return false;
    }

    private static string? GetNameOfInvokedExpression(ISyntaxFacts syntaxFacts, SyntaxNode invokedExpression)
    {
        if (syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
            return syntaxFacts.GetIdentifierOfSimpleName(syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;

        if (syntaxFacts.IsMemberBindingExpression(invokedExpression))
            invokedExpression = syntaxFacts.GetNameOfMemberBindingExpression(invokedExpression);

        if (syntaxFacts.IsIdentifierName(invokedExpression))
            return syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;

        return null;
    }

    private static bool IsMethodArgument(SyntaxToken token, ISyntaxFacts syntaxFacts)
        => syntaxFacts.IsLiteralExpression(token.Parent) &&
           syntaxFacts.IsArgument(token.Parent!.Parent);

    private (string? name, int? index) GetArgumentNameOrIndex(SyntaxNode argument)
    {
        var syntaxFacts = Info.SyntaxFacts;

        var argName = syntaxFacts.GetNameForArgument(argument);
        if (argName != "")
            return (argName, null);

        var arguments = syntaxFacts.GetArgumentsOfArgumentList(argument.GetRequiredParent());
        var index = arguments.IndexOf(argument);
        if (index >= 0)
            return (null, index);

        return default;
    }

    private bool TryAnalyzeInvocation(ISymbol? symbol, string? argName, int? argIndex)
        => symbol is IMethodSymbol method &&
           method.DeclaredAccessibility == Accessibility.Public &&
           method.MethodKind == MethodKind.Ordinary &&
           IsDateTimeType(method.ContainingType) &&
           AnalyzeStringLiteral(method, argName, argIndex);

    private bool IsDateTimeType(ITypeSymbol? type)
        => type != null && (type.Equals(_dateTimeType) || type.Equals(_dateTimeOffsetType));

    private static bool AnalyzeStringLiteral(IMethodSymbol method, string? argName, int? argIndex)
    {
        Debug.Assert(argName != null || argIndex != null);

        var parameters = method.Parameters;
        if (argName != null)
            return parameters.Any(predicate: static (p, argName) => p.Name == argName, arg: argName);

        var parameter = argIndex < parameters.Length ? parameters[argIndex.Value] : null;
        return parameter?.Name == FormatName;
    }
}
