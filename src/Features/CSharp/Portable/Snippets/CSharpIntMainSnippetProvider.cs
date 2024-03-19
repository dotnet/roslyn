// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
internal sealed class CSharpIntMainSnippetProvider : AbstractCSharpMainMethodSnippetProvider
{
    public override string Identifier => "sim";

    public override string Description => CSharpFeaturesResources.static_int_Main;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpIntMainSnippetProvider()
    {
    }

    protected override SyntaxNode GenerateReturnType(SyntaxGenerator generator)
        => generator.TypeExpression(SpecialType.System_Int32);

    protected override IEnumerable<SyntaxNode> GenerateInnerStatements(SyntaxGenerator generator)
    {
        var returnStatement = generator.ReturnStatement(generator.LiteralExpression(0));
        return SpecializedCollections.SingletonEnumerable(returnStatement);
    }

    protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
    {
        var methodDeclaration = (MethodDeclarationSyntax)caretTarget;
        var body = methodDeclaration.Body!;
        var returnStatement = body.Statements.First();

        var triviaSpan = returnStatement.GetLeadingTrivia().Span;
        var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
        // Getting the location at the end of the line before the newline.
        return line.Span.End;
    }

    protected override async Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var snippetNode = root.GetAnnotatedNodes(FindSnippetAnnotation).FirstOrDefault();

        if (snippetNode is not MethodDeclarationSyntax methodDeclaration)
            return document;

        var body = methodDeclaration.Body!;
        var returnStatement = body.Statements.First();

        var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
        var indentationString = CSharpSnippetHelpers.GetBlockLikeIndentationString(document, body.OpenBraceToken.SpanStart, syntaxFormattingOptions, cancellationToken);

        var updatedReturnStatement = returnStatement.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString));
        var updatedRoot = root.ReplaceNode(returnStatement, updatedReturnStatement);

        return document.WithSyntaxRoot(updatedRoot);
    }
}
