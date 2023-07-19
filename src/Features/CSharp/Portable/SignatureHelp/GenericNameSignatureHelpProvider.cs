// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("GenericNameSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal partial class GenericNameSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GenericNameSignatureHelpProvider()
        {
        }

        public override bool IsTriggerCharacter(char ch)
            => ch is '<' or ',';

        public override bool IsRetriggerCharacter(char ch)
            => ch == '>';

        protected virtual bool TryGetGenericIdentifier(
            SyntaxNode root, int position,
            ISyntaxFactsService syntaxFacts,
            SignatureHelpTriggerReason triggerReason,
            CancellationToken cancellationToken,
            out SyntaxToken genericIdentifier,
            out SyntaxToken lessThanToken)
        {
            if (CommonSignatureHelpUtilities.TryGetSyntax(
                    root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out GenericNameSyntax? name))
            {
                genericIdentifier = name.Identifier;
                lessThanToken = name.TypeArgumentList.LessThanToken;
                return true;
            }

            genericIdentifier = default;
            lessThanToken = default;
            return false;
        }

        private bool IsTriggerToken(SyntaxToken token)
        {
            return !token.IsKind(SyntaxKind.None) &&
                token.ValueText.Length == 1 &&
                IsTriggerCharacter(token.ValueText[0]) &&
                token.Parent is TypeArgumentListSyntax &&
                token.Parent.Parent is GenericNameSyntax;
        }

        private bool IsArgumentListToken(GenericNameSyntax node, SyntaxToken token)
        {
            return node.TypeArgumentList != null &&
                node.TypeArgumentList.Span.Contains(token.SpanStart) &&
                token != node.TypeArgumentList.GreaterThanToken;
        }

        protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
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

            var accessibleSymbols =
                symbols.WhereAsArray(s => s.GetArity() > 0)
                       .WhereAsArray(s => s is INamedTypeSymbol or IMethodSymbol)
                       .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation)
                       .Sort(semanticModel, genericIdentifier.SpanStart);

            if (!accessibleSymbols.Any())
            {
                return null;
            }

            var structuralTypeDisplayService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
            var textSpan = GetTextSpan(genericIdentifier, lessThanToken);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return CreateSignatureHelpItems(accessibleSymbols.Select(s =>
                Convert(s, lessThanToken, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
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

        protected virtual TextSpan GetTextSpan(SyntaxToken genericIdentifier, SyntaxToken lessThanToken)
        {
            Contract.ThrowIfFalse(lessThanToken.Parent is TypeArgumentListSyntax && lessThanToken.Parent.Parent is GenericNameSyntax);
            return SignatureHelpUtilities.GetSignatureHelpSpan(((GenericNameSyntax)lessThanToken.Parent.Parent).TypeArgumentList);
        }

        private static SignatureHelpItem Convert(
            ISymbol symbol,
            SyntaxToken lessThanToken,
            SemanticModel semanticModel,
            IStructuralTypeDisplayService structuralTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService)
        {
            var position = lessThanToken.SpanStart;

            SignatureHelpItem item;
            if (symbol is INamedTypeSymbol namedType)
            {
                item = CreateItem(
                    symbol, semanticModel, position,
                    structuralTypeDisplayService,
                    false,
                    symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    GetPreambleParts(namedType, semanticModel, position),
                    GetSeparatorParts(),
                    GetPostambleParts(),
                    namedType.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
            }
            else
            {
                var method = (IMethodSymbol)symbol;
                item = CreateItem(
                    symbol, semanticModel, position,
                    structuralTypeDisplayService,
                    false,
                    c => symbol.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c),
                    GetPreambleParts(method, semanticModel, position),
                    GetSeparatorParts(),
                    GetPostambleParts(method, semanticModel, position),
                    method.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
            }

            return item;
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
                }
            }

            return parts;
        }

        private static bool TypeParameterHasConstraints(ITypeParameterSymbol typeParam)
        {
            return !typeParam.ConstraintTypes.IsDefaultOrEmpty || typeParam.HasConstructorConstraint ||
                typeParam.HasReferenceTypeConstraint || typeParam.HasValueTypeConstraint;
        }
    }
}
