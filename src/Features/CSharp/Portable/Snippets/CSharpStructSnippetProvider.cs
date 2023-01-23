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
    internal sealed class CSharpStructSnippetProvider : CSharpTypeSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpStructSnippetProvider()
        {
        }
        public override string Identifier => "struct";

        public override string Description => FeaturesResources.struct_;

        protected override async Task<SyntaxNode> GenerateTypeDeclarationAsync(Document document, int position, bool useAccessibility, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var name = NameGenerator.GenerateUniqueName("MyStruct", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);
            var classDeclaration = useAccessibility is true
                ? generator.StructDeclaration(name, accessibility: Accessibility.Public)
                : generator.StructDeclaration(name);

            return classDeclaration;
        }

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        {
            return syntaxFacts.IsStructDeclaration;
        }
    }
}
