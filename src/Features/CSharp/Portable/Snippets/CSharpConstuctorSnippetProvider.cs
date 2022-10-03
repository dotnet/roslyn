// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpConstructorSnippetProvider : AbstractConstructorSnippetProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConstructorSnippetProvider()
        {
        }

        private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.NewKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.AbstractKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword
            };

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            return
                syntaxContext.IsGlobalStatementContext ||
                syntaxContext.IsTypeDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassStructRecordTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }

        protected override async Task<SyntaxNode> GenerateSnippetSyntaxAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            
            return generator.ConstructorDeclaration(
                containingTypeName: ,
                accessibility: Accessibility.Public);
        }
    }
}
