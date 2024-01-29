// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpVoidMainSnippetProvider : AbstractCSharpMainMethodSnippetProvider
    {
        public override string Identifier => "svm";

        public override string Description => CSharpFeaturesResources.static_void_Main;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVoidMainSnippetProvider()
        {
        }

        protected override SyntaxNode GenerateReturnType(SyntaxGenerator generator)
            => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));

        protected override IEnumerable<SyntaxNode> GenerateInnerStatements(SyntaxGenerator generator)
            => SpecializedCollections.EmptyEnumerable<SyntaxNode>();

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            return CSharpSnippetHelpers.GetTargetCaretPositionInBlock<MethodDeclarationSyntax>(
                caretTarget,
                static d => d.Body!,
                sourceText);
        }

        protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            return CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync<MethodDeclarationSyntax>(
                document,
                FindSnippetAnnotation,
                static m => m.Body!,
                cancellationToken);
        }
    }
}
