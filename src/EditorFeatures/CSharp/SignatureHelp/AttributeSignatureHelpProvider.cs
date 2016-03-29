// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.SignatureHelp;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("AttributeSignatureHelpProvider", LanguageNames.CSharp)]
    internal partial class AttributeSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        public override bool IsTriggerCharacter(char ch)
        {
            return ch == '(' || ch == ',';
        }

        public override bool IsRetriggerCharacter(char ch)
        {
            return ch == ')';
        }

        private bool TryGetAttributeExpression(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out attribute))
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

        protected override async Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            AttributeSyntax attribute;
            if (!TryGetAttributeExpression(root, position, document.GetLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out attribute))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(attribute, cancellationToken).ConfigureAwait(false);
            var attributeType = semanticModel.GetTypeInfo(attribute, cancellationToken).Type as INamedTypeSymbol;
            if (attributeType == null)
            {
                return null;
            }

            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            var symbolDisplayService = document.Project.LanguageServices.GetService<ISymbolDisplayService>();
            var accessibleConstructors = attributeType.InstanceConstructors
                                                      .Where(c => c.IsAccessibleWithin(within))
                                                      .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation)
                                                      .Sort(symbolDisplayService, semanticModel, attribute.SpanStart);

            if (!accessibleConstructors.Any())
            {
                return null;
            }

            var anonymousTypeDisplayService = document.Project.LanguageServices.GetService<IAnonymousTypeDisplayService>();
            var documentationCommentFormatter = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(attribute.ArgumentList);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return CreateSignatureHelpItems(accessibleConstructors.Select(c =>
                Convert(c, within, attribute, semanticModel, symbolDisplayService, anonymousTypeDisplayService, documentationCommentFormatter, cancellationToken)),
                textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
        }

        public override SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            AttributeSyntax expression;
            if (TryGetAttributeExpression(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
            }

            return null;
        }

        private SignatureHelpItem Convert(
            IMethodSymbol constructor,
            ISymbol within,
            AttributeSyntax attribute,
            SemanticModel semanticModel,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormatter,
            CancellationToken cancellationToken)
        {
            var position = attribute.SpanStart;
            var namedParameters = constructor.ContainingType.GetAttributeNamedParameters(semanticModel.Compilation, within)
                .OrderBy(s => s.Name)
                .ToList();

            var isVariadic =
                constructor.Parameters.Length > 0 && constructor.Parameters.Last().IsParams && namedParameters.Count == 0;

            var item = CreateItem(
                constructor, semanticModel, position,
                symbolDisplayService, anonymousTypeDisplayService,
                isVariadic,
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormatter),
                GetPreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(constructor),
                GetParameters(constructor, semanticModel, position, namedParameters, documentationCommentFormatter, cancellationToken));
            return item;
        }

        private IEnumerable<SignatureHelpParameter> GetParameters(
            IMethodSymbol constructor,
            SemanticModel semanticModel,
            int position,
            IList<ISymbol> namedParameters,
            IDocumentationCommentFormattingService documentationCommentFormatter,
            CancellationToken cancellationToken)
        {
            foreach (var parameter in constructor.Parameters)
            {
                yield return Convert(parameter, semanticModel, position, documentationCommentFormatter, cancellationToken);
            }

            for (int i = 0; i < namedParameters.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var namedParameter = namedParameters[i];

                var type = namedParameter is IFieldSymbol ? ((IFieldSymbol)namedParameter).Type : ((IPropertySymbol)namedParameter).Type;

                var displayParts = new List<SymbolDisplayPart>();

                displayParts.Add(new SymbolDisplayPart(
                    namedParameter is IFieldSymbol ? SymbolDisplayPartKind.FieldName : SymbolDisplayPartKind.PropertyName,
                    namedParameter, namedParameter.Name.ToIdentifierToken().ToString()));
                displayParts.Add(Space());
                displayParts.Add(Punctuation(SyntaxKind.EqualsToken));
                displayParts.Add(Space());
                displayParts.AddRange(type.ToMinimalDisplayParts(semanticModel, position));

                yield return new SignatureHelpParameter(
                    namedParameter.Name,
                    isOptional: true,
                    documentationFactory: namedParameter.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormatter),
                    displayParts: displayParts,
                    prefixDisplayParts: GetParameterPrefixDisplayParts(i));
            }
        }

        private static List<SymbolDisplayPart> GetParameterPrefixDisplayParts(int i)
        {
            if (i == 0)
            {
                return new List<SymbolDisplayPart>
                {
                    new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, CSharpEditorResources.Properties),
                    Punctuation(SyntaxKind.ColonToken),
                    Space()
                };
            }

            return null;
        }

        private IEnumerable<SymbolDisplayPart> GetPreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        private IEnumerable<SymbolDisplayPart> GetPostambleParts(IMethodSymbol method)
        {
            yield return Punctuation(SyntaxKind.CloseParenToken);
        }
    }
}
