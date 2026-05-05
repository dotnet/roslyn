// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUsingSnippetProvider() : AbstractUsingSnippetProvider<UsingStatementSyntax>
{
    public override string Identifier => CSharpSnippetIdentifiers.Using;

    public override string Description => CSharpFeaturesResources.using_statement;

    protected override ValueTask<ImmutableArray<SnippetPlaceholder>> GetPlaceHolderLocationsListAsync(
        Document document, UsingStatementSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        var expression = node.Expression!;
        return new([new SnippetPlaceholder(expression.ToString(), expression.SpanStart)]);
    }

    protected override int GetTargetCaretPosition(UsingStatementSyntax usingStatement, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            usingStatement,
            static s => (BlockSyntax)s.Statement,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, UsingStatementSyntax usingStatement, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            usingStatement,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);
}
