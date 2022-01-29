// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportSignatureHelpProvider("ConstructorInitializerSignatureHelpProvider", LanguageNames.CSharp), Shared]
    internal partial class ConstructorInitializerSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConstructorInitializerSignatureHelpProvider()
        {
        }

        public override bool IsTriggerCharacter(char ch)
            => ch is '(' or ',';

        public override bool IsRetriggerCharacter(char ch)
            => ch == ')';

        private bool TryGetConstructorInitializer(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, SignatureHelpTriggerReason triggerReason, CancellationToken cancellationToken, out ConstructorInitializerSyntax expression)
        {
            if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
            {
                return false;
            }

            return expression.ArgumentList != null;
        }

        private bool IsTriggerToken(SyntaxToken token)
            => SignatureHelpUtilities.IsTriggerParenOrComma<ConstructorInitializerSyntax>(token, IsTriggerCharacter);

        private static bool IsArgumentListToken(ConstructorInitializerSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList != null &&
                expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }

        protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!TryGetConstructorInitializer(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var constructorInitializer))
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within == null)
            {
                return null;
            }

            if (within.TypeKind is not TypeKind.Struct and not TypeKind.Class)
            {
                return null;
            }

            var type = constructorInitializer.Kind() == SyntaxKind.BaseConstructorInitializer
                ? within.BaseType
                : within;

            if (type == null)
            {
                return null;
            }

            // get the candidate methods
            var currentConstructor = semanticModel.GetDeclaredSymbol(constructorInitializer.Parent!, cancellationToken);

            var constructors = type.InstanceConstructors
                             .WhereAsArray(c => c.IsAccessibleWithin(within) && !c.Equals(currentConstructor))
                             .WhereAsArray(c => c.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
                             .Sort(semanticModel, constructorInitializer.SpanStart);

            if (!constructors.Any())
            {
                return null;
            }

            // guess the best candidate if needed and determine parameter index
            var arguments = constructorInitializer.ArgumentList.Arguments;
            var candidates = semanticModel.GetSymbolInfo(constructorInitializer, cancellationToken).Symbol is IMethodSymbol exactSymbol
                ? ImmutableArray.Create(exactSymbol)
                : constructors;
            LightweightOverloadResolution.RefineOverloadAndPickParameter(document, position, semanticModel, candidates, arguments, out var currentSymbol, out var parameterIndex);

            // present items and select
            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(constructorInitializer.ArgumentList);
            var structuralTypeDisplayService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
            var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

            var items = constructors.SelectAsArray(m => Convert(m, constructorInitializer.ArgumentList.OpenParenToken, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService));
            var selectedItem = TryGetSelectedIndex(constructors, currentSymbol);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return CreateSignatureHelpItems(items, textSpan, GetCurrentArgumentState(root, position, parameterIndex, syntaxFacts, textSpan, cancellationToken), selectedItem);
        }

        private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, int parameterIndex, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            if (TryGetConstructorInitializer(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
                currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
            {
                return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position, parameterIndex);
            }

            return null;
        }

        private static SignatureHelpItem Convert(
            IMethodSymbol constructor,
            SyntaxToken openToken,
            SemanticModel semanticModel,
            IStructuralTypeDisplayService structuralTypeDisplayService,
            IDocumentationCommentFormattingService documentationCommentFormattingService)
        {
            var position = openToken.SpanStart;
            var item = CreateItem(
                constructor, semanticModel, position,
                structuralTypeDisplayService,
                constructor.IsParams(),
                constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
                GetPreambleParts(constructor, semanticModel, position),
                GetSeparatorParts(),
                GetPostambleParts(),
                constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
            return item;
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
}
