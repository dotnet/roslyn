// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseObjectInitializer;

internal abstract class AbstractUseObjectInitializerCodeFixProvider<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TAssignmentStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer>
    : ForkingSyntaxEditorBasedCodeFixProvider<TObjectCreationExpressionSyntax>
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TAssignmentStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseNamedMemberInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>, new()
{
    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
        => (AnalyzersResources.Object_initialization_can_be_simplified, nameof(AnalyzersResources.Object_initialization_can_be_simplified));

    protected abstract TAnalyzer GetAnalyzer();

    protected abstract ISyntaxKinds SyntaxKinds { get; }
    protected abstract ISyntaxFormatting SyntaxFormatting { get; }

    protected abstract SyntaxTrivia Whitespace(string text);

    protected abstract TStatementSyntax GetNewStatement(
        TStatementSyntax statement, TObjectCreationExpressionSyntax objectCreation, SyntaxFormattingOptions options,
        ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches);

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseObjectInitializerDiagnosticId];

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        TObjectCreationExpressionSyntax objectCreation,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var currentRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var analyzer = GetAnalyzer();

        var matches = analyzer.Analyze(semanticModel, syntaxFacts, objectCreation, cancellationToken);
        if (matches.IsDefaultOrEmpty)
            return;

        var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
        Contract.ThrowIfNull(statement);

        var firstToken = objectCreation.GetFirstToken();
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var newStatement = GetNewStatement(statement, objectCreation, formattingOptions, matches).WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(statement, newStatement);
        foreach (var match in matches)
            editor.RemoveNode(match.Statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
    }

    protected TExpressionSyntax Indent(TExpressionSyntax expression, SyntaxFormattingOptions options)
    {
        var endOfLineKind = this.SyntaxKinds.EndOfLineTrivia;
        var whitespaceTriviaKind = this.SyntaxKinds.WhitespaceTrivia;
        return expression.ReplaceTokens(
            expression.DescendantTokens(),
            (currentToken, _) =>
            {
                if (currentToken.LeadingTrivia is [.., var whitespace1] &&
                    whitespace1.RawKind == whitespaceTriviaKind)
                {
                    // This is a token on its own line.  With whitespace at the start of the line.
                    var leadingTrivia = currentToken.LeadingTrivia.Replace(
                        whitespace1,
                        IncreaseIndent(whitespace1, options));

                    currentToken = currentToken.WithLeadingTrivia(leadingTrivia);
                }

                if (currentToken.TrailingTrivia is [.., var endOfLine, var whitespace2] &&
                    endOfLine.RawKind == endOfLineKind &&
                    whitespace2.RawKind == whitespaceTriviaKind)
                {
                    // This is a VB line continuation case (`_`), with indentation before the next token
                    var trailingTrivia = currentToken.TrailingTrivia.Replace(
                        whitespace2,
                        IncreaseIndent(whitespace2, options));

                    currentToken = currentToken.WithTrailingTrivia(trailingTrivia);
                }

                return currentToken;
            });
    }

    private SyntaxTrivia IncreaseIndent(SyntaxTrivia whitespaceTrivia, SyntaxFormattingOptions options)
    {
        // Convert the existing whitespace to determine which column it corresponds to in spaces.  
        var existingWhitespace = whitespaceTrivia.ToString();
        var spaceCount = existingWhitespace.ConvertTabToSpace(
            options.TabSize,
            initialColumn: 0,
            endPosition: existingWhitespace.Length);

        // Then add the desired indentation spaces to it.
        var desiredSpaceCount = spaceCount + options.IndentationSize;

        // Now convert back to a string with the appropriate tab/space configuration.
        var desiredWhitespace = desiredSpaceCount.CreateIndentationString(options.UseTabs, options.TabSize);
        return Whitespace(desiredWhitespace);
    }
}
