// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;
using static UseCollectionExpressionHelpers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForStackAlloc), Shared]
internal partial class CSharpUseCollectionExpressionForStackAllocCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForStackAllocCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId);

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpCodeFixesResources.Use_collection_expression, nameof(CSharpCodeFixesResources.Use_collection_expression));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

#if CODE_STYLE
        var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
#else
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(
            fallbackOptions, cancellationToken).ConfigureAwait(false);
#endif

#if CODE_STYLE
        var wrappingLength = CodeActionOptions.DefaultCollectionExpressionWrappingLength;
#else
        var wrappingLength = fallbackOptions.GetOptions(document.Project.Services).CollectionExpressionWrappingLength;
#endif

        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            var expression = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (expression is StackAllocArrayCreationExpressionSyntax arrayCreation)
            {
                RewriteArrayCreationExpression(
                    arrayCreation,
                    diagnostic.AdditionalLocations
                        .Skip(1)
                        .Select(loc =>
                        {
                            var expression = (StatementSyntax)loc.FindNode(getInnermostNodeForTie: true, cancellationToken);
                            var statement = expression.FirstAncestorOrSelf<StatementSyntax>();
                            Contract.ThrowIfNull(statement);
                            return (statement, expression);
                        })
                        .ToImmutableArray());
            }
            else if (expression is ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation)
            {
                RewriteImplicitArrayCreationExpression(implicitArrayCreation);
            }
        }

        return;

        static bool IsOnSingleLine(SourceText sourceText, SyntaxNode node)
            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

        void RewriteArrayCreationExpression(
            StackAllocArrayCreationExpressionSyntax arrayCreation,
            ImmutableArray<(StatementSyntax statement, ExpressionSyntax expression)> matches)
        {
            var makeMultiLine = MakeMultiLineCollectionExpression(matches);

            if (arrayCreation.Initializer != null)
            {
                // If the original stacklloc had an initializer (stackalloc int[] { ... }) then just convert the
                // initializer over.
                Contract.ThrowIfNull(currentArrayCreation.Initializer);

                var isOnSingleLine = IsOnSingleLine(sourceText, arrayCreation.Initializer);
                var collectionExpression = ConvertInitializerToCollectionExpression(
                    currentArrayCreation.Initializer, isOnSingleLine);

                editor.ReplaceNode(
    arrayCreation,


                return ReplaceWithCollectionExpression(
                    sourceText, arrayCreation.Initializer, collectionExpression, isOnSingleLine);
            }
            else
            {
                // otherwise, we had `stackalloc int[X];` and we used the following expressions to initialize
                // the values.
                if (makeMultiLine)
                {
                    editor.ReplaceNode(
    arrayCreation,

                }
                else
                {
                    // All the elements would work on a single line.  This is a trivial case.  We can just make the
                    // fresh collection expression, and do a wholesale replacement of the original object creation
                    // expression with it.
                    using var _1 = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
                    CreateAndAddElements(matches, preferredIndentation: null, nodesAndTokens);

                    var collectionExpression = CollectionExpression(
                        SeparatedList<CollectionElementSyntax>(nodesAndTokens));
                    editor.ReplaceNode(
    arrayCreation,

                    return collectionExpression.WithTriviaFrom(currentArrayCreation);
                }
            }
        }

        // Helper which produces the CollectionElementSyntax nodes and adds to the separated syntax list builder array.
        // Used to we can uniformly add the items correctly with the requested (but optional) indentation.  And so that
        // commas are added properly to the sequence.
        void CreateAndAddElements(
            ImmutableArray<(StatementSyntax statement, ExpressionSyntax expression)> matches
            string? preferredIndentation,
            ArrayBuilder<SyntaxNodeOrToken> nodesAndTokens)
        {
            // If there's no requested indentation, then we want to produce the sequence as: `a, b, c, d`.  So just
            // a space after any comma.  If there is desired indentation for an element, then we always follow a comma
            // with a newline so that the element node comes on the next line indented properly.
            var triviaAfterComma = preferredIndentation is null
                ? TriviaList(Space)
                : TriviaList(EndOfLine(formattingOptions.NewLine));

            foreach (var match in matches)
            {
                var expression = CreateExpression(match);

                // Add a comment before each new element we're adding.  Move any trailing whitespace/comment trivia
                // from the prior node to come after that comma.  e.g. if the prior node was `x // comment` then we
                // end up with: `x, // comment<new-line>`
                if (nodesAndTokens.Count > 0)
                {
                    var lastNode = nodesAndTokens[^1];
                    var trailingWhitespaceAndComments = lastNode.GetTrailingTrivia().Where(static t => t.IsWhitespaceOrSingleOrMultiLineComment());

                    nodesAndTokens[^1] = lastNode.WithTrailingTrivia(lastNode.GetTrailingTrivia().Where(t => !trailingWhitespaceAndComments.Contains(t)));

                    var commaToken = Token(SyntaxKind.CommaToken)
                        .WithoutLeadingTrivia()
                        .WithTrailingTrivia(TriviaList(trailingWhitespaceAndComments).AddRange(triviaAfterComma));
                    nodesAndTokens.Add(commaToken);
                }

                nodesAndTokens.Add(expression);
            }
        }

        bool MakeMultiLineCollectionExpression(
            ImmutableArray<(StatementSyntax statement, ExpressionSyntax expression)> matches)
        {
            var totalLength = 0;
            foreach (var (statement, expression) in matches)
            {
                // this must succeed since the analyzer only passed us expressions in `X[...] = expr;` statements.

                // if the statement we're replacing has any comments on it, then we need to be multiline to give them an
                // appropriate place to go.
                if (statement.GetLeadingTrivia().Any(static t => t.IsSingleOrMultiLineComment()) ||
                    statement.GetTrailingTrivia().Any(static t => t.IsSingleOrMultiLineComment()))
                {
                    return true;
                }

                // if any of the expressions we're adding are multiline, then make things multiline.
                if (!sourceText.AreOnSameLine(expression.GetFirstToken(), expression.GetLastToken()))
                    return true;

                totalLength += expression.Span.Length;
            }

            return totalLength > wrappingLength;
        }

        void RewriteImplicitArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation)
        {
            Contract.ThrowIfNull(implicitArrayCreation.Initializer);

            var isOnSingleLine = IsOnSingleLine(sourceText, implicitArrayCreation);
            var collectionExpression = ConvertInitializerToCollectionExpression(
                implicitArrayCreation.Initializer, isOnSingleLine);

            var finalCollectionExpression = ReplaceWithCollectionExpression(
                sourceText, implicitArrayCreation.Initializer, collectionExpression, isOnSingleLine);

            editor.ReplaceNode(
                implicitArrayCreation,
                finalCollectionExpression);
        }
    }
}
