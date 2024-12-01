// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.InlineHints;

/// <summary>
/// The service to locate the positions in which the adornments should appear
/// as well as associate the adornments back to the parameter name
/// </summary>
[ExportLanguageService(typeof(IInlineParameterNameHintsService), LanguageNames.CSharp), Shared]
internal class CSharpInlineParameterNameHintsService : AbstractInlineParameterNameHintsService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpInlineParameterNameHintsService()
    {
    }

    protected override void AddAllParameterNameHintLocations(
         SemanticModel semanticModel,
         ISyntaxFactsService syntaxFacts,
         SyntaxNode node,
         ArrayBuilder<(int position, string? identifierArgument, IParameterSymbol? parameter, HintKind kind)> buffer,
         CancellationToken cancellationToken)
    {
        if (node is BaseArgumentListSyntax argumentList)
        {
            AddArguments(semanticModel, syntaxFacts, buffer, argumentList, cancellationToken);
        }
        else if (node is AttributeArgumentListSyntax attributeArgumentList)
        {
            AddArguments(semanticModel, syntaxFacts, buffer, attributeArgumentList, cancellationToken);
        }
    }

    private static void AddArguments(
        SemanticModel semanticModel,
        ISyntaxFactsService syntaxFacts,
        ArrayBuilder<(int position, string? identifierArgument, IParameterSymbol? parameter, HintKind kind)> buffer,
        AttributeArgumentListSyntax argumentList,
        CancellationToken cancellationToken)
    {
        foreach (var argument in argumentList.Arguments)
        {
            if (argument.NameEquals != null || argument.NameColon != null)
                continue;

            var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
            var identifierArgument = GetIdentifierNameFromArgument(argument, syntaxFacts);
            buffer.Add((argument.Span.Start, identifierArgument, parameter, GetKind(argument.Expression)));
        }
    }

    private static void AddArguments(
        SemanticModel semanticModel,
        ISyntaxFactsService syntaxFacts,
        ArrayBuilder<(int position, string? identifierArgument, IParameterSymbol? parameter, HintKind kind)> buffer,
        BaseArgumentListSyntax argumentList,
        CancellationToken cancellationToken)
    {
        foreach (var argument in argumentList.Arguments)
        {
            if (argument.NameColon != null)
                continue;

            var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
            var identifierArgument = GetIdentifierNameFromArgument(argument, syntaxFacts);
            buffer.Add((argument.Span.Start, identifierArgument, parameter, GetKind(argument.Expression)));
        }
    }

    private static HintKind GetKind(ExpressionSyntax arg)
        => arg switch
        {
            LiteralExpressionSyntax or InterpolatedStringExpressionSyntax => HintKind.Literal,
            ObjectCreationExpressionSyntax => HintKind.ObjectCreation,
            CastExpressionSyntax cast => GetKind(cast.Expression),
            PrefixUnaryExpressionSyntax prefix => GetKind(prefix.Operand),
            // Treat `expr!` the same as `expr` (i.e. treat `!` as if it's just trivia).
            PostfixUnaryExpressionSyntax(SyntaxKind.SuppressNullableWarningExpression) postfix => GetKind(postfix.Operand),
            _ => HintKind.Other,
        };

    protected override bool IsIndexer(SyntaxNode node, IParameterSymbol parameter)
    {
        return node is BracketedArgumentListSyntax;
    }

    protected override string GetReplacementText(string parameterName)
    {
        var keywordKind = SyntaxFacts.GetKeywordKind(parameterName);
        var isReservedKeyword = SyntaxFacts.IsReservedKeyword(keywordKind);
        return (isReservedKeyword ? "@" : string.Empty) + parameterName + ": ";
    }
}
