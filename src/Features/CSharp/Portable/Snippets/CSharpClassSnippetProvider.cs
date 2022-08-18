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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpClassSnippetProvider : AbstractClassSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpClassSnippetProvider()
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
            var syntaxTree = syntaxContext.SyntaxTree;

            return
                syntaxContext.IsGlobalStatementContext ||
                syntaxContext.IsTypeDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }

        protected override void GetClassDeclaration(SyntaxNode node, out SyntaxToken identifier, out int cursorPosition)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)node;
            identifier = classDeclarationSyntax.Identifier;
            cursorPosition = classDeclarationSyntax.OpenBraceToken.SpanStart + 1;
        }
    }
}
