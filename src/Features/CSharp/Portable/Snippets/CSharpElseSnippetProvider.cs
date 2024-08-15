// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpElseSnippetProvider() : AbstractElseSnippetProvider<ElseClauseSyntax>
{
    public override string Identifier => CSharpSnippetIdentifiers.Else;

    public override string Description => FeaturesResources.else_statement;

    protected override bool IsValidSnippetLocation(SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxContext = context.SyntaxContext;
        var token = syntaxContext.TargetToken;

        // We have to consider all ancestor if statements of the last token until we find a match for this 'else':
        // while (true)
        //     if (true)
        //         while (true)
        //             if (true)
        //                 Console.WriteLine();
        //             else
        //                 Console.WriteLine();
        //     $$
        var isAfterIfStatement = false;

        foreach (var ifStatement in token.GetAncestors<IfStatementSyntax>())
        {
            // If there's a missing token at the end of the statement, it's incomplete and we do not offer 'else'.
            // context.TargetToken does not include zero width so in that case these will never be equal.
            if (ifStatement.Statement.GetLastToken(includeZeroWidth: true) == token)
            {
                isAfterIfStatement = true;
                break;
            }
        }

        return isAfterIfStatement && base.IsValidSnippetLocation(context, cancellationToken);
    }

    protected override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var elseClause = SyntaxFactory.ElseClause(SyntaxFactory.Block());
        return Task.FromResult(new TextChange(TextSpan.FromBounds(position, position), elseClause.ToFullString()));
    }

    protected override int GetTargetCaretPosition(ElseClauseSyntax elseClause, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            elseClause,
            static c => (BlockSyntax)c.Statement,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, ElseClauseSyntax elseClause, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            elseClause,
            static c => (BlockSyntax)c.Statement,
            cancellationToken);
}
