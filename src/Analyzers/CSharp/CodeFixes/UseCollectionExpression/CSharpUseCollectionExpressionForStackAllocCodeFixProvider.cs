// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionExpression;
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
        RegisterCodeFix(context, CSharpCodeFixesResources.Use_collection_expression, IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId);
        return Task.CompletedTask;
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        // Fix-All for this feature is somewhat complicated.  As Collection-Initializers 
        // could be arbitrarily nested, we have to make sure that any edits we make
        // to one Collection-Initializer are seen by any higher ones.  In order to do this
        // we actually process each object-creation-node, one at a time, rewriting
        // the tree for each node.  In order to do this effectively, we use the '.TrackNodes'
        // feature to keep track of all the object creation nodes as we make edits to
        // the tree.  If we didn't do this, then we wouldn't be able to find the 
        // second object-creation-node after we make the edit for the first one.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var originalRoot = editor.OriginalRoot;

        var stackallocExpressions = new Stack<ExpressionSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var expression = (ExpressionSyntax)originalRoot.FindNode(
                diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            stackallocExpressions.Push(expression);
        }

        // We're going to be continually editing this tree.  Track all the nodes we
        // care about so we can find them across each edit.
        var semanticDocument = await SemanticDocument.CreateAsync(
            document.WithSyntaxRoot(originalRoot.TrackNodes(stackallocExpressions)),
            cancellationToken).ConfigureAwait(false);

        while (stackallocExpressions.Count > 0)
        {
            var originalStackAllocExpression = stackallocExpressions.Pop();

            var stackAllocExpression = semanticDocument.Root.GetCurrentNodes(originalStackAllocExpression).Single();
            if (stackAllocExpression is not StackAllocArrayCreationExpressionSyntax and not ImplicitStackAllocArrayCreationExpressionSyntax)
                continue;

            var matches = GetMatches(semanticDocument, stackAllocExpression);
            if (matches.IsDefault)
                continue;

            var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
                semanticDocument.Document,
                fallbackOptions,
                stackAllocExpression,
                matches,
                static e => e switch
                {
                    StackAllocArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
                    ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.Initializer,
                    _ => throw ExceptionUtilities.Unreachable(),
                },
                static (e, i) => e switch
                {
                    StackAllocArrayCreationExpressionSyntax arrayCreation => arrayCreation.WithInitializer(i),
                    ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.WithInitializer(i),
                    _ => throw ExceptionUtilities.Unreachable(),
                },
                cancellationToken).ConfigureAwait(false);

            var newRoot = semanticDocument.Root.ReplaceNode(stackAllocExpression, collectionExpression);
            semanticDocument = await semanticDocument.WithSyntaxRootAsync(newRoot, cancellationToken).ConfigureAwait(false);
        }

        editor.ReplaceNode(originalRoot, semanticDocument.Root);
        return;

        ImmutableArray<CollectionExpressionMatch> GetMatches(SemanticDocument document, ExpressionSyntax stackAllocExpression)
        {
            switch (stackAllocExpression)
            {
                case ImplicitStackAllocArrayCreationExpressionSyntax:
                    // if we have `stackalloc[] { ... }` we have no subsequent matches to add to the collection. All values come
                    // from within the initializer.
                    return ImmutableArray<CollectionExpressionMatch>.Empty;

                case StackAllocArrayCreationExpressionSyntax arrayCreation:
                    // If we have `stackalloc T[] { ... }`, then all collection elements from the initializer only.
                    if (arrayCreation.Initializer is null)
                        return ImmutableArray<CollectionExpressionMatch>.Empty;

                    // we have `stackalloc T[...];` Have to find the elements based on what follows the creation
                    // statement.
                    return CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer.TryGetMatches(
                        document.SemanticModel, arrayCreation, cancellationToken);

                default:
                    // We validated this is unreachable in the caller.
                    throw ExceptionUtilities.Unreachable();
            }
        }
    }
}

//        return;

//        if (stackAllocExpression is ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation)
//        {
//            var collectionExpression = RewriteImplicitArrayCreationExpression(implicitArrayCreation);
//            subEditor.ReplaceNode(implicitArrayCreation, collectionExpression);
//        }
//        else if (stackAllocExpression is StackAllocArrayCreationExpressionSyntax explicitArrayCreation)
//        {
//            var collectionExpression = RewriteExplicitArrayCreationExpression(explicitArrayCreation);
//            subEditor.ReplaceNode(explicitArrayCreation, collectionExpression);
//        }

//        var matches = analyzer.Analyze(
//            semanticDocument.SemanticModel, syntaxFacts, objectCreation, useCollectionExpression, cancellationToken);

//        if (matches.IsDefault)
//            continue;

//        var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
//        Contract.ThrowIfNull(statement);

//        var newStatement = await GetNewStatementAsync(
//            semanticDocument.Document, fallbackOptions, statement, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);


//        subEditor.ReplaceNode(statement, newStatement);
//        foreach (var match in matches)
//            subEditor.RemoveNode(match.Statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);

//        semanticDocument = await semanticDocument.WithSyntaxRootAsync(
//            subEditor.GetChangedRoot(), cancellationToken).ConfigureAwait(false);
//    }


//    static bool IsOnSingleLine(SourceText sourceText, SyntaxNode node)
//            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

//        CollectionExpressionSyntax RewriteImplicitArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation)
//        {
//            Contract.ThrowIfNull(implicitArrayCreation.Initializer);

//            var isOnSingleLine = IsOnSingleLine(semanticDocument.Text, implicitArrayCreation);
//            var collectionExpression = ConvertInitializerToCollectionExpression(
//                implicitArrayCreation.Initializer, isOnSingleLine);

//            var finalCollectionExpression = ReplaceWithCollectionExpression(
//                semanticDocument.Text, implicitArrayCreation.Initializer, collectionExpression, isOnSingleLine);

//            return finalCollectionExpression;
//        }

//        CollectionExpressionSyntax RewriteExplicitArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation)
//        {

//        }
//    }


//    protected override async Task FixAllAsync(
//        Document document,
//        ImmutableArray<Diagnostic> diagnostics,
//        SyntaxEditor editor,
//        CodeActionOptionsProvider fallbackOptions,
//        CancellationToken cancellationToken)
//    {
//        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

//        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
//        {
//            var expression = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
//            if (expression is StackAllocArrayCreationExpressionSyntax arrayCreation)
//            {
//                RewriteArrayCreationExpression(
//                    arrayCreation,
//                    diagnostic.AdditionalLocations
//                        .Skip(1)
//                        .Select(loc =>
//                        {
//                            var expression = (StatementSyntax)loc.FindNode(getInnermostNodeForTie: true, cancellationToken);
//                            var statement = expression.FirstAncestorOrSelf<StatementSyntax>();
//                            Contract.ThrowIfNull(statement);
//                            return (statement, expression);
//                        })
//                        .ToImmutableArray());
//            }
//            else if (expression is ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation)
//            {
//                RewriteImplicitArrayCreationExpression(implicitArrayCreation);
//            }
//        }

//        return;

//        void RewriteArrayCreationExpression(
//            StackAllocArrayCreationExpressionSyntax arrayCreation,
//            ImmutableArray<(StatementSyntax statement, ExpressionSyntax expression)> matches)
//        {
//            var makeMultiLine = MakeMultiLineCollectionExpression(matches);

//            if (arrayCreation.Initializer != null)
//            {
//                // If the original stacklloc had an initializer (stackalloc int[] { ... }) then just convert the
//                // initializer over.
//                Contract.ThrowIfNull(currentArrayCreation.Initializer);

//                var isOnSingleLine = IsOnSingleLine(sourceText, arrayCreation.Initializer);
//                var collectionExpression = ConvertInitializerToCollectionExpression(
//                    currentArrayCreation.Initializer, isOnSingleLine);

//                editor.ReplaceNode(
//    arrayCreation,


//                return ReplaceWithCollectionExpression(
//                    sourceText, arrayCreation.Initializer, collectionExpression, isOnSingleLine);
//            }
//            else
//            {
//                // otherwise, we had `stackalloc int[X];` and we used the following expressions to initialize
//                // the values.
//                if (makeMultiLine)
//                {
//                    editor.ReplaceNode(
//    arrayCreation,

//                }
//                else
//                {
//                    // All the elements would work on a single line.  This is a trivial case.  We can just make the
//                    // fresh collection expression, and do a wholesale replacement of the original object creation
//                    // expression with it.
//                    using var _1 = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
//                    CreateAndAddElements(matches, preferredIndentation: null, nodesAndTokens);

//                    var collectionExpression = CollectionExpression(
//                        SeparatedList<CollectionElementSyntax>(nodesAndTokens));
//                    editor.ReplaceNode(
//    arrayCreation,

//                    return collectionExpression.WithTriviaFrom(currentArrayCreation);
//                }
//            }
//        }

//        // Helper which produces the CollectionElementSyntax nodes and adds to the separated syntax list builder array.
//        // Used to we can uniformly add the items correctly with the requested (but optional) indentation.  And so that
//        // commas are added properly to the sequence.
//        void CreateAndAddElements(
//            ImmutableArray<(StatementSyntax statement, ExpressionSyntax expression)> matches
//            string? preferredIndentation,
//            ArrayBuilder<SyntaxNodeOrToken> nodesAndTokens)
//        {
//            // If there's no requested indentation, then we want to produce the sequence as: `a, b, c, d`.  So just
//            // a space after any comma.  If there is desired indentation for an element, then we always follow a comma
//            // with a newline so that the element node comes on the next line indented properly.
//            var triviaAfterComma = preferredIndentation is null
//                ? TriviaList(Space)
//                : TriviaList(EndOfLine(formattingOptions.NewLine));

//            foreach (var match in matches)
//            {
//                var expression = CreateExpression(match);

//                // Add a comment before each new element we're adding.  Move any trailing whitespace/comment trivia
//                // from the prior node to come after that comma.  e.g. if the prior node was `x // comment` then we
//                // end up with: `x, // comment<new-line>`
//                if (nodesAndTokens.Count > 0)
//                {
//                    var lastNode = nodesAndTokens[^1];
//                    var trailingWhitespaceAndComments = lastNode.GetTrailingTrivia().Where(static t => t.IsWhitespaceOrSingleOrMultiLineComment());

//                    nodesAndTokens[^1] = lastNode.WithTrailingTrivia(lastNode.GetTrailingTrivia().Where(t => !trailingWhitespaceAndComments.Contains(t)));

//                    var commaToken = Token(SyntaxKind.CommaToken)
//                        .WithoutLeadingTrivia()
//                        .WithTrailingTrivia(TriviaList(trailingWhitespaceAndComments).AddRange(triviaAfterComma));
//                    nodesAndTokens.Add(commaToken);
//                }

//                nodesAndTokens.Add(expression);
//            }
//        }

//        bool MakeMultiLineCollectionExpression(
//            ImmutableArray<(StatementSyntax statement, ExpressionSyntax expression)> matches)
//        {
//            var totalLength = 0;
//            foreach (var (statement, expression) in matches)
//            {
//                // this must succeed since the analyzer only passed us expressions in `X[...] = expr;` statements.

//                // if the statement we're replacing has any comments on it, then we need to be multiline to give them an
//                // appropriate place to go.
//                if (statement.GetLeadingTrivia().Any(static t => t.IsSingleOrMultiLineComment()) ||
//                    statement.GetTrailingTrivia().Any(static t => t.IsSingleOrMultiLineComment()))
//                {
//                    return true;
//                }

//                // if any of the expressions we're adding are multiline, then make things multiline.
//                if (!sourceText.AreOnSameLine(expression.GetFirstToken(), expression.GetLastToken()))
//                    return true;

//                totalLength += expression.Span.Length;
//            }

//            return totalLength > wrappingLength;
//        }
//    }
//}
