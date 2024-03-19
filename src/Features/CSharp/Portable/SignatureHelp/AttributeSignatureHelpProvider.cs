// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportSignatureHelpProvider("AttributeSignatureHelpProvider", LanguageNames.CSharp), Shared]
internal partial class AttributeSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AttributeSignatureHelpProvider()
    {
    }

    public override bool IsTriggerCharacter(char ch)
        => ch is '(' or ',';

    public override bool IsRetriggerCharacter(char ch)
        => ch == ')';

    private bool TryGetAttributeExpression(
        SyntaxNode root,
        int position,
        ISyntaxFactsService syntaxFacts,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out AttributeSyntax? attribute)
    {
        if (!CommonSignatureHelpUtilities.TryGetSyntax(
                root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out attribute))
        {
            return false;
        }

        return attribute.ArgumentList != null;
    }

    private bool IsTriggerToken(SyntaxToken token)
    {
        return !token.IsKind(SyntaxKind.None) &&
            token.ValueText.Length == 1 &&
            IsTriggerCharacter(token.ValueText[0]) &&
            token.Parent is AttributeArgumentListSyntax &&
            token.Parent.Parent is AttributeSyntax;
    }

    private static bool IsArgumentListToken(AttributeSyntax expression, SyntaxToken token)
    {
        return expression.ArgumentList != null &&
            expression.ArgumentList.Span.Contains(token.SpanStart) &&
            token != expression.ArgumentList.CloseParenToken;
    }

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetAttributeExpression(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var attribute))
        {
            return null;
        }

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(attribute, cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetTypeInfo(attribute, cancellationToken).Type is not INamedTypeSymbol attributeType)
        {
            return null;
        }

        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var accessibleConstructors = attributeType.InstanceConstructors
                                                  .WhereAsArray(c => c.IsAccessibleWithin(within))
                                                  .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                                                  .Sort(semanticModel, attribute.SpanStart);

        if (!accessibleConstructors.Any())
        {
            return null;
        }

        var structuralTypeDisplayService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormatter = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(attribute.ArgumentList!);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var symbolInfo = semanticModel.GetSymbolInfo(attribute, cancellationToken);
        var selectedItem = TryGetSelectedIndex(accessibleConstructors, symbolInfo.Symbol);

        return CreateSignatureHelpItems(accessibleConstructors.Select(c =>
            Convert(c, within, attribute, semanticModel, structuralTypeDisplayService, documentationCommentFormatter, cancellationToken)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem, parameterIndexOverride: -1);
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetAttributeExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList!).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList!, position);
        }

        return null;
    }

    private static SignatureHelpItem Convert(
        IMethodSymbol constructor,
        ISymbol within,
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        IDocumentationCommentFormattingService documentationCommentFormatter,
        CancellationToken cancellationToken)
    {
        var position = attribute.SpanStart;
        var namedParameters = constructor.ContainingType.GetAttributeNamedParameters(semanticModel.Compilation, within)
            .OrderBy(s => s.Name)
            .ToList();

        var isVariadic =
            constructor.Parameters is [.., { IsParams: true }] && namedParameters.Count == 0;

        var item = CreateItem(
            constructor, semanticModel, position,
            structuralTypeDisplayService,
            isVariadic,
            constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormatter),
            GetPreambleParts(constructor, semanticModel, position),
            GetSeparatorParts(),
            GetPostambleParts(),
            GetParameters(constructor, semanticModel, position, namedParameters, documentationCommentFormatter, cancellationToken));
        return item;
    }

    private static IList<SignatureHelpSymbolParameter> GetParameters(
        IMethodSymbol constructor,
        SemanticModel semanticModel,
        int position,
        IList<ISymbol> namedParameters,
        IDocumentationCommentFormattingService documentationCommentFormatter,
        CancellationToken cancellationToken)
    {
        var result = new List<SignatureHelpSymbolParameter>();
        foreach (var parameter in constructor.Parameters)
        {
            result.Add(Convert(parameter, semanticModel, position, documentationCommentFormatter));
        }

        for (var i = 0; i < namedParameters.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var namedParameter = namedParameters[i];

            var type = namedParameter is IFieldSymbol ? ((IFieldSymbol)namedParameter).Type : ((IPropertySymbol)namedParameter).Type;

            var displayParts = new List<SymbolDisplayPart>
            {
                new SymbolDisplayPart(
                namedParameter is IFieldSymbol ? SymbolDisplayPartKind.FieldName : SymbolDisplayPartKind.PropertyName,
                namedParameter, namedParameter.Name.ToIdentifierToken().ToString()),
                Space(),
                Punctuation(SyntaxKind.EqualsToken),
                Space()
            };
            displayParts.AddRange(type.ToMinimalDisplayParts(semanticModel, position));

            result.Add(new SignatureHelpSymbolParameter(
                namedParameter.Name,
                isOptional: true,
                documentationFactory: namedParameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormatter),
                displayParts: displayParts,
                prefixDisplayParts: GetParameterPrefixDisplayParts(i)));
        }

        return result;
    }

    private static List<SymbolDisplayPart>? GetParameterPrefixDisplayParts(int i)
    {
        if (i == 0)
        {
            return
            [
                new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, CSharpFeaturesResources.Properties),
                Punctuation(SyntaxKind.ColonToken),
                Space()
            ];
        }

        return null;
    }

    private static IList<SymbolDisplayPart> GetPreambleParts(
        IMethodSymbol method,
        SemanticModel semanticModel,
        int position)
    {
        var result = new List<SymbolDisplayPart>();

        result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));

        return result;
    }

    private static IList<SymbolDisplayPart> GetPostambleParts()
    {
        return SpecializedCollections.SingletonList(
            Punctuation(SyntaxKind.CloseParenToken));
    }
}
