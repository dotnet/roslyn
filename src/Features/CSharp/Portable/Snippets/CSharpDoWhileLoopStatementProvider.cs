// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpDoWhileLoopStatementProvider() : AbstractConditionalBlockSnippetProvider
{
    public override string Identifier => CSharpSnippetIdentifiers.Do;

    public override string Description => CSharpFeaturesResources.do_while_loop;

    protected override SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, InlineExpressionInfo? inlineExpressionInfo)
    {
        return SyntaxFactory.DoStatement(
            SyntaxFactory.Block(),
            (ExpressionSyntax)(inlineExpressionInfo?.Node.WithoutLeadingTrivia() ?? generator.TrueLiteralExpression()));
    }

    protected override SyntaxNode GetCondition(SyntaxNode node)
    {
        var doStatement = (DoStatementSyntax)node;
        return doStatement.Condition;
    }

    protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        => static node => node is DoStatementSyntax;

    protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
    {
        return CSharpSnippetHelpers.GetTargetCaretPositionInBlock<DoStatementSyntax>(
            caretTarget,
            static s => (BlockSyntax)s.Statement,
            sourceText);
    }

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        return CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync<DoStatementSyntax>(
            document,
            FindSnippetAnnotation,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);
    }
}
