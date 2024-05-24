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
internal sealed class CSharpWhileLoopSnippetProvider() : AbstractWhileLoopSnippetProvider<WhileStatementSyntax, ExpressionSyntax>
{
    public override string Identifier => CSharpSnippetIdentifiers.While;

    public override string Description => FeaturesResources.while_loop;

    protected override ExpressionSyntax GetCondition(WhileStatementSyntax node)
        => node.Condition;

    protected override int GetTargetCaretPosition(WhileStatementSyntax whileStatement, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            whileStatement,
            static s => (BlockSyntax)s.Statement,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, WhileStatementSyntax whileStatement, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            whileStatement,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);
}
