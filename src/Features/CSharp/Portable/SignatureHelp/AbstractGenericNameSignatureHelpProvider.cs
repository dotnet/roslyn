// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal abstract partial class AbstractGenericNameSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters => ['<', ','];

    public override ImmutableArray<char> RetriggerCharacters => ['>'];

    protected abstract TextSpan GetTextSpan(SyntaxToken genericIdentifier, SyntaxToken lessThanToken);

    protected abstract bool TryGetGenericIdentifier(
        SyntaxNode root, int position,
        ISyntaxFactsService syntaxFacts,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken,
        out SyntaxToken genericIdentifier,
        out SyntaxToken lessThanToken);

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetGenericIdentifier(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken,
                out var genericIdentifier, out var lessThanToken))
        {
            return null;
        }

        if (genericIdentifier.Parent is not SimpleNameSyntax simpleName)
        {
            return null;
        }

        var beforeDotExpression = simpleName.IsRightSideOfDot() ? simpleName.GetLeftSideOfDot() : null;

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(simpleName, cancellationToken).ConfigureAwait(false);

        var leftSymbol = beforeDotExpression == null
            ? null
            : semanticModel.GetSymbolInfo(beforeDotExpression, cancellationToken).GetAnySymbol() as INamespaceOrTypeSymbol;
        var leftType = beforeDotExpression == null
            ? null
            : semanticModel.GetTypeInfo(beforeDotExpression, cancellationToken).Type as INamespaceOrTypeSymbol;

        var leftContainer = leftSymbol ?? leftType;

        var isBaseAccess = beforeDotExpression is BaseExpressionSyntax;
        var namespacesOrTypesOnly = SyntaxFacts.IsInNamespaceOrTypeContext(simpleName);
        var includeExtensions = leftSymbol == null && leftType != null;
        var name = genericIdentifier.ValueText;
        var symbols = isBaseAccess
            ? semanticModel.LookupBaseMembers(position, name)
            : namespacesOrTypesOnly
                ? semanticModel.LookupNamespacesAndTypes(position, leftContainer, name)
                : semanticModel.LookupSymbols(position, leftContainer, name, includeExtensions);

        var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        if (within == null)
        {
            return null;
        }

        var accessibleSymbols = symbols
            .WhereAsArray(s => s.GetArity() > 0)
            .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation, inclusionFilter: static s => true)
            .Sort(semanticModel, genericIdentifier.SpanStart);

        if (!accessibleSymbols.Any())
            return null;

        var structuralTypeDisplayService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = GetTextSpan(genericIdentifier, lessThanToken);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        return CreateSignatureHelpItems([.. accessibleSymbols.Select(s =>
            Convert(s, lessThanToken, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService))],
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, cancellationToken), selectedItemIndex: null, parameterIndexOverride: -1);
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
    {
        if (!TryGetGenericIdentifier(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken,
                out var genericIdentifier, out _))
        {
            return null;
        }

        if (genericIdentifier.TryParseGenericName(cancellationToken, out var genericName))
        {
            // Because we synthesized the generic name, it will have an index starting at 0
            // instead of at the actual position it's at in the text.  Because of this, we need to
            // offset the position we are checking accordingly.
            var offset = genericIdentifier.SpanStart - genericName.SpanStart;
            position -= offset;
            return SignatureHelpUtilities.GetSignatureHelpState(genericName.TypeArgumentList, position);
        }

        return null;
    }

    private static SignatureHelpItem Convert(
        ISymbol symbol,
        SyntaxToken lessThanToken,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = lessThanToken.SpanStart;

        if (symbol is INamedTypeSymbol namedType)
        {
            return CreateItem(
                symbol, semanticModel, position,
                structuralTypeDisplayService,
                isVariadic: false,
                symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(namedType, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(),
                [.. namedType.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService))]);
        }
        else if (symbol is IMethodSymbol method)
        {
            return CreateItem(
                symbol, semanticModel, position,
                structuralTypeDisplayService,
                isVariadic: false,
                symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(method, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(method, semanticModel, position),
                GetTypeArguments(method));
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(symbol);
        }

        IList<SignatureHelpSymbolParameter> GetTypeArguments(IMethodSymbol method)
        {
            var result = new List<SignatureHelpSymbolParameter>();

            // Signature help for generic modern extensions must include the generic type *arguments* for the containing
            // extension as well.  These are fixed given the receiver, and need to be repeated in the method type argument
            // list.
            if (method.ContainingType.IsExtension)
            {
                result.AddRange(method.ContainingType.TypeArguments.Select(t => new SignatureHelpSymbolParameter(
                    name: null, isOptional: false,
                    t.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    t.ToMinimalDisplayParts(semanticModel, position))));
            }

            result.AddRange(method.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)));

            return result;
        }
    }

    private static readonly SymbolDisplayFormat s_minimallyQualifiedFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(
            SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions | SymbolDisplayGenericsOptions.IncludeVariance);

    private static SignatureHelpSymbolParameter Convert(
        ITypeParameterSymbol parameter,
        SemanticModel semanticModel,
        int position,
        IDocumentationCommentFormattingService formatter)
    {
        return new SignatureHelpSymbolParameter(
            parameter.Name,
            isOptional: false,
            documentationFactory: parameter.GetDocumentationPartsFactory(semanticModel, position, formatter),
            displayParts: parameter.ToMinimalDisplayParts(semanticModel, position, s_minimallyQualifiedFormat),
            selectedDisplayParts: GetSelectedDisplayParts(parameter, semanticModel, position));
    }

    private static IList<SymbolDisplayPart> GetSelectedDisplayParts(
        ITypeParameterSymbol typeParam,
        SemanticModel semanticModel,
        int position)
    {
        var parts = new List<SymbolDisplayPart>();

        if (TypeParameterHasConstraints(typeParam))
        {
            parts.Add(Space());
            parts.Add(Keyword(SyntaxKind.WhereKeyword));
            parts.Add(Space());

            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.TypeParameterName, typeParam, typeParam.Name));

            parts.Add(Space());
            parts.Add(Punctuation(SyntaxKind.ColonToken));
            parts.Add(Space());

            var needComma = false;

            // class/struct constraint must be first
            if (typeParam.HasReferenceTypeConstraint)
            {
                parts.Add(Keyword(SyntaxKind.ClassKeyword));
                needComma = true;
            }
            else if (typeParam.HasUnmanagedTypeConstraint)
            {
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "unmanaged"));
                needComma = true;
            }
            else if (typeParam.HasValueTypeConstraint)
            {
                parts.Add(Keyword(SyntaxKind.StructKeyword));
                needComma = true;
            }

            foreach (var baseType in typeParam.ConstraintTypes)
            {
                if (needComma)
                {
                    parts.Add(Punctuation(SyntaxKind.CommaToken));
                    parts.Add(Space());
                }

                parts.AddRange(baseType.ToMinimalDisplayParts(semanticModel, position));
                needComma = true;
            }

            // ctor constraint must be last
            if (typeParam.HasConstructorConstraint)
            {
                if (needComma)
                {
                    parts.Add(Punctuation(SyntaxKind.CommaToken));
                    parts.Add(Space());
                }

                parts.Add(Keyword(SyntaxKind.NewKeyword));
                parts.Add(Punctuation(SyntaxKind.OpenParenToken));
                parts.Add(Punctuation(SyntaxKind.CloseParenToken));
                needComma = true;
            }

            if (typeParam.AllowsRefLikeType)
            {
                if (needComma)
                {
                    parts.Add(Punctuation(SyntaxKind.CommaToken));
                    parts.Add(Space());
                }

                parts.Add(Keyword(SyntaxKind.AllowsKeyword));
                parts.Add(Space());
                parts.Add(Keyword(SyntaxKind.RefKeyword));
                parts.Add(Space());
                parts.Add(Keyword(SyntaxKind.StructKeyword));
            }
        }

        return parts;
    }

    private static bool TypeParameterHasConstraints(ITypeParameterSymbol typeParam)
    {
        return !typeParam.ConstraintTypes.IsDefaultOrEmpty || typeParam.HasConstructorConstraint ||
            typeParam.HasReferenceTypeConstraint || typeParam.HasValueTypeConstraint ||
            typeParam.AllowsRefLikeType;
    }
}
