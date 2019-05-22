// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.LanguageServices;
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
        public GenericNameSignatureHelpProvider()
        {
        }

        public override bool IsTriggerCharacter(char ch)
        {
            return ch == '<' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == '>';
        }

        protected virtual bool TryGetGenericIdentifier(
            SyntaxNode root, int position,
            ISyntaxFactsService syntaxFacts,
            SignatureHelpTriggerReason triggerReason,
            CancellationToken cancellationToken,
            out SyntaxToken genericIdentifier, out SyntaxToken lessThanToken)
        {
            if (CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out GenericNameSyntax name))
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

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetGenericIdentifier(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken,
                    out var genericIdentifier, out var lessThanToken))
            {
                return null;
            }

            var simpleName = genericIdentifier.Parent as SimpleNameSyntax;
            if (simpleName == null)
            {
                return null;
            }

            var beforeDotExpression = simpleName.IsRightSideOfDot() ? simpleName.GetLeftSideOfDot() : null;

            var semanticModel = await document.GetSemanticModelForNodeAsync(simpleName, cancellationToken).ConfigureAwait(false);

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

            var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
            var accessibleSymbols =
                symbols.WhereAsArray(s => s.GetArity() > 0)
                       .WhereAsArray(s => s is INamedTypeSymbol || s is IMethodSymbol)
                       .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation)
                       .Sort(symbolDisplayService, semanticModel, genericIdentifier.SpanStart);

            if (!accessibleSymbols.Any())
            {
                return null;
            }

            var anonymousTypeDisplayService = document.GetLanguageService<IAnonymousTypeDisplayService>();
            var documentationCommentFormattingService = document.GetLanguageService<IDocumentationCommentFormattingService>();
            var textSpan = GetTextSpan(genericIdentifier, lessThanToken);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            return CreateSignatureHelpItems(accessibleSymbols.Select(s =>
                Convert(s, lessThanToken, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormattingService, cancellationToken)).ToList(),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem: null);
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (!TryGetGenericIdentifier(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken,
                    out var genericIdentifier, out var lessThanToken))
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

        private SignatureHelpItem Convert(
            ISymbol symbol,
            SyntaxToken lessThanToken,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService,
            CancellationToken cancellationToken)
        {
            var position = lessThanToken.SpanStart;

            SignatureHelpItem item;
            if (symbol is INamedTypeSymbol namedType)
            {
                item = CreateItem(
                    symbol, semanticModel, position,
                    symbolDisplayService, anonymousTypeDisplayService,
                    false,
                    symbol.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                    GetPreambleParts(namedType, semanticModel, position),
                    GetSeparatorParts(),
                    GetPostambleParts(namedType),
                    namedType.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)).ToList());
            }
            else
            {
                var method = (IMethodSymbol)symbol;
                item = CreateItem(
                    symbol, semanticModel, position,
                    symbolDisplayService, anonymousTypeDisplayService,
                    false,
                    c => symbol.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c).Concat(GetAwaitableUsage(method, semanticModel, position)),
                    GetPreambleParts(method, semanticModel, position),
                    GetSeparatorParts(),
                    GetPostambleParts(method, semanticModel, position),
                    method.TypeParameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService, cancellationToken)).ToList());
            }

            return item;
        }

        private static readonly SymbolDisplayFormat s_minimallyQualifiedFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions | SymbolDisplayGenericsOptions.IncludeVariance);

        private SignatureHelpSymbolParameter Convert(
            ITypeParameterSymbol parameter,
            SemanticModel semanticModel,
            int position,
            IDocumentationCommentFormattingService formatter,
            CancellationToken cancellationToken)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                isOptional: false,
                documentationFactory: parameter.GetDocumentationPartsFactory(semanticModel, position, formatter),
                displayParts: parameter.ToMinimalDisplayParts(semanticModel, position, s_minimallyQualifiedFormat),
                selectedDisplayParts: GetSelectedDisplayParts(parameter, semanticModel, position, cancellationToken));
        }

        private IList<SymbolDisplayPart> GetSelectedDisplayParts(
            ITypeParameterSymbol typeParam,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken)
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
