// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUnsafeSnippetProvider() : AbstractStatementSnippetProvider<UnsafeStatementSyntax>
{
    public override string Identifier => CSharpSnippetIdentifiers.Unsafe;

    public override string Description => CSharpFeaturesResources.unsafe_block;

    protected override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        => Task.FromResult(new TextChange(TextSpan.FromBounds(position, position), SyntaxFactory.UnsafeStatement().ToFullString()));

    protected override int GetTargetCaretPosition(UnsafeStatementSyntax unsafeStatement, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            unsafeStatement,
            static s => s.Block,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, UnsafeStatementSyntax unsafeStatement, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            unsafeStatement,
            static s => s.Block,
            cancellationToken);
}
