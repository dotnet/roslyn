// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportSignatureHelpProvider("TupleSignatureHelpProvider", LanguageNames.CSharp), Shared]
internal class TupleConstructionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
{
    private static readonly Func<TupleExpressionSyntax, SyntaxToken> s_getOpenToken = e => e.OpenParenToken;
    private static readonly Func<TupleExpressionSyntax, SyntaxToken> s_getCloseToken = e => e.CloseParenToken;
    private static readonly Func<TupleExpressionSyntax, SyntaxNodeOrTokenList> s_getArgumentsWithSeparators = e => e.Arguments.GetWithSeparators();
    private static readonly Func<TupleExpressionSyntax, IEnumerable<string>> s_getArgumentNames = e => e.Arguments.Select(a => a.NameColon?.Name.Identifier.ValueText ?? string.Empty);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TupleConstructionSignatureHelpProvider()
    {
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (GetOuterMostTupleExpressionInSpan(root, position, syntaxFacts, currentSpan, cancellationToken, out var expression))
        {
            return CommonSignatureHelpUtilities.GetSignatureHelpState(expression, position,
               getOpenToken: s_getOpenToken,
               getCloseToken: s_getCloseToken,
               getArgumentsWithSeparators: s_getArgumentsWithSeparators,
               getArgumentNames: s_getArgumentNames);
        }

        if (GetOuterMostParenthesizedExpressionInSpan(root, position, syntaxFacts, currentSpan, cancellationToken, out var parenthesizedExpression))
        {
            if (currentSpan.Start == parenthesizedExpression.SpanStart)
            {
                return new SignatureHelpState(
                    SemanticParameterIndex: 0,
                    SyntacticArgumentCount: 0,
                    ArgumentName: string.Empty,
                    ArgumentNames: default);
            }
        }

        return null;
    }

    private bool GetOuterMostTupleExpressionInSpan(SyntaxNode root, int position,
        ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken, [NotNullWhen(true)] out TupleExpressionSyntax? result)
    {
        result = null;
        while (TryGetTupleExpression(SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
            root, position, syntaxFacts, cancellationToken, out var expression))
        {
            if (!currentSpan.Contains(expression.Span))
            {
                break;
            }

            result = expression;
            position = expression.SpanStart;
        }

        return result != null;
    }

    private bool GetOuterMostParenthesizedExpressionInSpan(SyntaxNode root, int position,
     ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken, [NotNullWhen(true)] out ParenthesizedExpressionSyntax? result)
    {
        result = null;
        while (TryGetParenthesizedExpression(SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
            root, position, syntaxFacts, cancellationToken, out var expression))
        {
            if (!currentSpan.Contains(expression.Span))
            {
                break;
            }

            result = expression;
            position = expression.SpanStart;
        }

        return result != null;
    }

    public override Boolean IsRetriggerCharacter(Char ch)
        => ch == ')';

    public override Boolean IsTriggerCharacter(Char ch)
        => ch is '(' or ',';

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var typeInferrer = document.GetRequiredLanguageService<ITypeInferenceService>();
        var inferredTypes = FindNearestTupleConstructionWithInferrableType(root, semanticModel, position, triggerInfo,
            typeInferrer, syntaxFacts, cancellationToken, out var targetExpression);

        if (inferredTypes == null || !inferredTypes.Any())
        {
            return null;
        }

        return CreateItems(position, root, syntaxFacts, targetExpression!, semanticModel, inferredTypes, cancellationToken);
    }

    private IEnumerable<INamedTypeSymbol>? FindNearestTupleConstructionWithInferrableType(SyntaxNode root, SemanticModel semanticModel, int position, SignatureHelpTriggerInfo triggerInfo,
        ITypeInferenceService typeInferrer, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, out ExpressionSyntax? targetExpression)
    {
        // Walk upward through TupleExpressionSyntax/ParenthsizedExpressionSyntax looking for a 
        // place where we can infer a tuple type. 
        ParenthesizedExpressionSyntax? parenthesizedExpression = null;
        while (TryGetTupleExpression(triggerInfo.TriggerReason, root, position, syntaxFacts, cancellationToken, out var tupleExpression) ||
               TryGetParenthesizedExpression(triggerInfo.TriggerReason, root, position, syntaxFacts, cancellationToken, out parenthesizedExpression))
        {
            targetExpression = (ExpressionSyntax?)tupleExpression ?? parenthesizedExpression;
            var inferredTypes = typeInferrer.InferTypes(semanticModel, targetExpression!.SpanStart, cancellationToken);

            var tupleTypes = inferredTypes.Where(t => t.IsTupleType).OfType<INamedTypeSymbol>().ToList();
            if (tupleTypes.Any())
            {
                return tupleTypes;
            }

            position = targetExpression.GetFirstToken().SpanStart;
        }

        targetExpression = null;
        return null;
    }

    private SignatureHelpItems? CreateItems(int position, SyntaxNode root, ISyntaxFactsService syntaxFacts,
        SyntaxNode targetExpression, SemanticModel semanticModel, IEnumerable<INamedTypeSymbol> tupleTypes, CancellationToken cancellationToken)
    {
        var prefixParts = SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "(")).ToTaggedText();
        var suffixParts = SpecializedCollections.SingletonEnumerable(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")")).ToTaggedText();
        var separatorParts = GetSeparatorParts().ToTaggedText();

        var items = tupleTypes.Select(tupleType => Convert(
            tupleType, prefixParts, suffixParts, separatorParts, semanticModel, position))
            .ToList();

        var state = GetCurrentArgumentState(root, position, syntaxFacts, targetExpression.FullSpan, cancellationToken);
        return CreateSignatureHelpItems(items, targetExpression.Span, state, selectedItemIndex: null, parameterIndexOverride: -1);
    }

    private static SignatureHelpItem Convert(INamedTypeSymbol tupleType, ImmutableArray<TaggedText> prefixParts, ImmutableArray<TaggedText> suffixParts,
        ImmutableArray<TaggedText> separatorParts, SemanticModel semanticModel, int position)
    {
        return new SymbolKeySignatureHelpItem(
                symbol: tupleType,
                isVariadic: false,
                documentationFactory: null,
                prefixParts: prefixParts,
                separatorParts: separatorParts,
                suffixParts: suffixParts,
                parameters: ConvertTupleMembers(tupleType, semanticModel, position),
                descriptionParts: null);
    }

    private static IEnumerable<SignatureHelpParameter> ConvertTupleMembers(INamedTypeSymbol tupleType, SemanticModel semanticModel, int position)
    {
        var spacePart = Space();
        var result = new List<SignatureHelpParameter>();
        foreach (var element in tupleType.TupleElements)
        {
            // The display name for each element. 
            // Empty strings for elements not explicitly declared
            var elementName = element.IsImplicitlyDeclared ? string.Empty : element.Name;

            var typeParts = element.Type.ToMinimalDisplayParts(semanticModel, position).ToList();
            if (!string.IsNullOrEmpty(elementName))
            {
                typeParts.Add(spacePart);
                typeParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, null, elementName));
            }

            result.Add(new SignatureHelpParameter(name: string.Empty, isOptional: false, documentationFactory: null, displayParts: typeParts));
        }

        return result;
    }

    private bool TryGetTupleExpression(SignatureHelpTriggerReason triggerReason, SyntaxNode root, int position,
        ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, [NotNullWhen(true)] out TupleExpressionSyntax? tupleExpression)
    {
        return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTupleExpressionTriggerToken,
            IsTupleArgumentListToken, cancellationToken, out tupleExpression);
    }

    private bool IsTupleExpressionTriggerToken(SyntaxToken token)
        => SignatureHelpUtilities.IsTriggerParenOrComma<TupleExpressionSyntax>(token, IsTriggerCharacter);

    private static bool IsTupleArgumentListToken(TupleExpressionSyntax? tupleExpression, SyntaxToken token)
    {
        return tupleExpression != null &&
            tupleExpression.Arguments.FullSpan.Contains(token.SpanStart) &&
            token != tupleExpression.CloseParenToken;
    }

    private bool TryGetParenthesizedExpression(SignatureHelpTriggerReason triggerReason, SyntaxNode root, int position,
        ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken, [NotNullWhen(true)] out ParenthesizedExpressionSyntax? parenthesizedExpression)
    {
        return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason,
            IsParenthesizedExpressionTriggerToken, IsParenthesizedExpressionToken, cancellationToken, out parenthesizedExpression);
    }

    private bool IsParenthesizedExpressionTriggerToken(SyntaxToken token)
        => token.IsKind(SyntaxKind.OpenParenToken) && token.Parent is ParenthesizedExpressionSyntax;

    private static bool IsParenthesizedExpressionToken(ParenthesizedExpressionSyntax? expr, SyntaxToken token)
    {
        return expr != null &&
            expr.FullSpan.Contains(token.SpanStart) &&
            token != expr.CloseParenToken;
    }
}
