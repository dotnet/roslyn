// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    internal abstract class AbstractConstructorSnippetProvider : AbstractSingleChangeSnippetProvider
    {
        public override string Identifier => "ctor";

        public override string Description => FeaturesResources.constructor;
        public override ImmutableArray<string> AdditionalFilterTexts { get; } = ImmutableArray.Create("constructor");

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts) => syntaxFacts.IsConstructorDeclaration;

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
            => ImmutableArray<SnippetPlaceholder>.Empty;

        protected override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeAtPosition = root.FindNode(TextSpan.FromBounds(position, position));
            var containingType = nodeAtPosition.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration);
            Contract.ThrowIfNull(containingType);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var containingTypeSymbol = semanticModel.GetDeclaredSymbol(containingType, cancellationToken);
            Contract.ThrowIfNull(containingTypeSymbol);
            var constructorDeclaration = generator.ConstructorDeclaration(
                containingTypeName: syntaxFacts.GetIdentifierOfTypeDeclaration(containingType).ToString(),
                accessibility: containingTypeSymbol.IsAbstract ? Accessibility.Protected : Accessibility.Public);
            return new TextChange(TextSpan.FromBounds(position, position), constructorDeclaration.NormalizeWhitespace().ToFullString());
        }
    }
}
