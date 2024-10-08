// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpDoWhileLoopSnippetProvider()
    : AbstractConditionalBlockSnippetProvider<DoStatementSyntax, ExpressionSyntax>
{
    public override string Identifier => CSharpSnippetIdentifiers.Do;

    public override string Description => CSharpFeaturesResources.do_while_loop;

    protected override bool CanInsertStatementAfterToken(SyntaxToken token)
        => token.IsBeginningOfStatementContext() || token.IsBeginningOfGlobalStatementContext();

    protected override DoStatementSyntax GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, InlineExpressionInfo? inlineExpressionInfo)
    {
        return SyntaxFactory.DoStatement(
            SyntaxFactory.Block(),
            (ExpressionSyntax)(inlineExpressionInfo?.Node.WithoutLeadingTrivia() ?? generator.TrueLiteralExpression()));
    }

    protected override ExpressionSyntax GetCondition(DoStatementSyntax node)
        => node.Condition;

    protected override int GetTargetCaretPosition(DoStatementSyntax doStatement, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            doStatement,
            static s => (BlockSyntax)s.Statement,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, DoStatementSyntax doStatement, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            doStatement,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);
}
