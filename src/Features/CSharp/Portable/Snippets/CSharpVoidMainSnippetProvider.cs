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
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

using static CSharpSyntaxTokens;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpVoidMainSnippetProvider() : AbstractCSharpMainMethodSnippetProvider
{
    public override string Identifier => CSharpSnippetIdentifiers.StaticVoidMain;

    public override string Description => CSharpFeaturesResources.static_void_Main;

    protected override TypeSyntax GenerateReturnType(SyntaxGenerator generator)
        => SyntaxFactory.PredefinedType(VoidKeyword);

    protected override IEnumerable<StatementSyntax> GenerateInnerStatements(SyntaxGenerator generator)
        => [];

    protected override int GetTargetCaretPosition(MethodDeclarationSyntax methodDeclaration, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            methodDeclaration,
            static d => d.Body!,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            methodDeclaration,
            static m => m.Body!,
            cancellationToken);
}
