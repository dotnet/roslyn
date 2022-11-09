// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpClassSnippetProvider : CSharpTypeSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpClassSnippetProvider()
        {
        }
        public override string Identifier => "class";

        public override string Description => FeaturesResources.class_;

        protected override async Task<SyntaxNode> GenerateTypeDeclarationAsync(Document document, int position, bool useAccessibility, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var name = NameGenerator.GenerateUniqueName("MyClass", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);
            var classDeclaration = useAccessibility is true
                ? generator.ClassDeclaration(name, accessibility: Accessibility.Public)
                : generator.ClassDeclaration(name);

            return classDeclaration;
        }

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        {
            return syntaxFacts.IsClassDeclaration;
        }
    }
}
