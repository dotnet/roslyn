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
internal sealed class CSharpLockSnippetProvider : AbstractLockSnippetProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpLockSnippetProvider()
    {
    }

    public override string Identifier => "lock";

    public override string Description => CSharpFeaturesResources.lock_statement;

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        var lockStatement = (LockStatementSyntax)node;
        var expression = lockStatement.Expression;
        return [new SnippetPlaceholder(expression.ToString(), expression.SpanStart)];
    }

    protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
    {
        return CSharpSnippetHelpers.GetTargetCaretPositionInBlock<LockStatementSyntax>(
            caretTarget,
            static s => (BlockSyntax)s.Statement,
            sourceText);
    }

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        return CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync<LockStatementSyntax>(
            document,
            FindSnippetAnnotation,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);
    }
}
