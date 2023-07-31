// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionInitializer), Shared]
    internal class CSharpUseCollectionInitializerCodeFixProvider :
        AbstractUseCollectionInitializerCodeFixProvider<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            BaseObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            ForEachStatementSyntax,
            IfStatementSyntax,
            VariableDeclaratorSyntax,
            CSharpUseCollectionInitializerAnalyzer>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseCollectionInitializerCodeFixProvider()
        {
        }

        protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
            => CSharpUseCollectionInitializerAnalyzer.Allocate();

        protected override async Task<StatementSyntax> GetNewStatementAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            StatementSyntax statement,
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            var newObjectCreation = await GetNewObjectCreationAsync(
                document, fallbackOptions, objectCreation, useCollectionExpression, matches, cancellationToken).ConfigureAwait(false);
            return statement.ReplaceNode(objectCreation, newObjectCreation);
        }

        private static async Task<ExpressionSyntax> GetNewObjectCreationAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            bool useCollectionExpression,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            return useCollectionExpression
                ? await CreateCollectionExpressionAsync(document, fallbackOptions, objectCreation, matches, cancellationToken).ConfigureAwait(false)
                : CreateObjectInitializerExpression(objectCreation, matches);
        }

        private static BaseObjectCreationExpressionSyntax CreateObjectInitializerExpression(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches)
        {
            var expressions = CreateElements(objectCreation, matches, static (_, e) => e);
            var withLineBreaks = AddLineBreaks(expressions, includeFinalLineBreak: true);
            var newCreation = UseInitializerHelpers.GetNewObjectCreation(objectCreation, withLineBreaks);
            return newCreation.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static async Task<CollectionExpressionSyntax> CreateCollectionExpressionAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            CancellationToken cancellationToken)
        {
            var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var elements = CreateElements(objectCreation, matches, CreateCollectionElement);

#if CODE_STYLE
            var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
#else
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(
                fallbackOptions, cancellationToken).ConfigureAwait(false);
#endif

            var indentationOptions = new IndentationOptions(formattingOptions);

            // the option is currently not an editorconfig option, so not available in code style layer
            var wrappingLength =
#if !CODE_STYLE
                fallbackOptions.GetOptions(document.Project.Services)?.CollectionExpressionWrappingLength ??
#endif
                CodeActionOptions.DefaultCollectionExpressionWrappingLength;

            //if (MakeMultiLine(sourceText, objectCreation, matches, wrappingLength))
            //    elements = AddLineBreaks(elements, includeFinalLineBreak: false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var makeMultiLine = MakeMultiLine(sourceText, objectCreation, matches, wrappingLength);

            var initializer = objectCreation.Initializer;
            return initializer == null || initializer.Expressions.Count == 0
                ? CreateCollectionExpressionWithoutExistingElements()
                : CreateCollectionExpressionWithExistingElements();

            CollectionExpressionSyntax CreateCollectionExpressionWithoutExistingElements()
            {
                // Didn't have an existing initializer (or it was empty).  For both cases, just create an entirely
                // fresh collection expression, and replace the object entirely.
                if (makeMultiLine)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // All the elements would work on a single line.  This is a trivial case.  We can just make the
                    // fresh collection expression, and do a wholesale replacement of the original object creation
                    // expression with it.
                    var collectionExpression = CollectionExpression(
                        SeparatedList(matches.Select(m => CreateElement(m, CreateCollectionElement).element)));
                    return collectionExpression.WithTriviaFrom(objectCreation);
                }
            }

            CollectionExpressionSyntax CreateCollectionExpressionWithExistingElements()
            {
                // If the object creation expression had an initializer (with at least one element in it).  Attempt to
                // preserve the formatting of the original initializer and the new collection expression.
                var initializer = objectCreation.Initializer;
                Contract.ThrowIfNull(initializer);

                var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
                var indentationOptions = new IndentationOptions(formattingOptions);

                if (!sourceText.AreOnSameLine(initializer.GetFirstToken(), initializer.GetLastToken()))
                {
                    // initializer itself was on multiple lines.  We'll want to create a collection expression whose
                    // braces (and initial elements) match whatever the initializer correct looks like.

                    var initialConversion = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: false);

                    if (!makeMultiLine &&
                        sourceText.AreOnSameLine(initializer.Expressions.First().GetFirstToken(), initializer.Expressions.Last().GetLastToken()))
                    {
                        // New elements were all single line, and existing elements were on a single line.  e.g.
                        //
                        //  {
                        //      1, 2, 3
                        //  }
                        //
                        // Just add the new elements to this.

                        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
                        nodesAndTokens.AddRange(initialConversion.Elements.GetWithSeparators());

                        var trailingComma = default(SyntaxToken);
                        var trailingTrivia = default(SyntaxTriviaList);
                        if (nodesAndTokens[^1].IsToken)
                        {
                            trailingComma = nodesAndTokens[^1].AsToken();
                            nodesAndTokens.RemoveLast();
                        }
                        else
                        {
                            trailingTrivia = nodesAndTokens[^1].GetTrailingTrivia();
                            nodesAndTokens[^1] = nodesAndTokens[^1].WithTrailingTrivia();
                        }

                        foreach (var element in matches.Select(m => CreateElement(m, CreateCollectionElement).element))
                        {
                            nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithoutLeadingTrivia().WithTrailingTrivia(Space));
                            nodesAndTokens.Add(element.WithoutTrivia());
                        }

                        // If we ended with a comma before, continue ending with a comma.
                        if (trailingComma != default)
                        {
                            nodesAndTokens.Add(trailingComma);
                        }
                        else
                        {
                            nodesAndTokens[^1] = nodesAndTokens[^1].WithTrailingTrivia(trailingTrivia);
                        }

                        var finalCollection = initialConversion.WithElements(SeparatedList<CollectionElementSyntax>(nodesAndTokens));

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection);
                    }
                    else
                    {
                        // We want the new items to be multiline *or* existing items were on different lines already.
                        // Figure out what the preferred indentation is, and prepend each new item with it.
                        var preferredIndentation = initializer.Expressions.First().GetFirstToken().GetPreferredIndentation(
                            parsedDocument, indentationOptions, cancellationToken);

                        using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);
                        nodesAndTokens.AddRange(initialConversion.Elements.GetWithSeparators());

                        var trailingComma = default(SyntaxToken);
                        var trailingTrivia = default(SyntaxTriviaList);
                        if (nodesAndTokens[^1].IsToken)
                        {
                            trailingComma = nodesAndTokens[^1].AsToken();
                            nodesAndTokens.RemoveLast();
                        }
                        else
                        {
                            trailingTrivia = nodesAndTokens[^1].GetTrailingTrivia();
                            nodesAndTokens[^1] = nodesAndTokens[^1].WithTrailingTrivia();
                        }

                        foreach (var (element, anchor) in matches.Select(m => CreateElement(m, CreateCollectionElement)))
                        {
                            nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithoutTrivia().WithTrailingTrivia(EndOfLine(formattingOptions.NewLine)));
                            nodesAndTokens.Add(ShiftNode(element.WithoutTrivia(), anchor, preferredIndentation));
                        }

                        // If we ended with a comma before, continue ending with a comma.
                        if (trailingComma != default)
                        {
                            nodesAndTokens.Add(trailingComma);
                        }
                        else
                        {
                            nodesAndTokens[^1] = nodesAndTokens[^1].WithTrailingTrivia(trailingTrivia);
                        }

                        var finalCollection = initialConversion.WithElements(SeparatedList<CollectionElementSyntax>(nodesAndTokens));

                        return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                            sourceText, initializer, finalCollection);
                    }

                    if (!makeMultiLine)
                    {
                        // combined length of the current expressions and new expressions is ok for a single line. And
                        // none of the new expressions are on multiple lines.

                    }

                    if (!makeMultiLine &&
                        (initializer.Expressions.Count == 0 ||
                         sourceText.AreOnSameLine(initializer.Expressions.First().GetFirstToken(), initializer.Expressions.Last().GetLastToken())))
                    {
                        // If the existing elements themselves were on the same line *and* the new elements are all single
                        // line, then keep things on a single line.
                    }
                    else
                    {
                        // otherwise, the existing expressions were on multiple lines, or the new expressions need
                        // multiple lines.  Place each expression on a new line, indented by the right amount.
                    }

                    // Now do the actual replacement.  This will ensure the location of the collection expression
                    // properly corresponds to the equivalent pieces of the collection initializer.
                    //return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                    //    sourceText, objectCreation.Initializer, totalConversion);
                }
                else if (makeMultiLine)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    // Both the initializer and the new elements all would work on a single line.

                    // First, convert the existing initializer (and its expressions) into a corresponding collection
                    // expression.  This will fixup the braces properly for the collection expression.
                    var initialConversion = UseCollectionExpressionHelpers.ConvertInitializerToCollectionExpression(
                        initializer, wasOnSingleLine: true);

                    // now, add all the matches in after the existing elements.
                    var totalConversion = initialConversion.AddElements(
                        matches.Select(m => CreateElement(m, CreateCollectionElement).element).ToArray());

                    // Now do the actual replacement.  This will ensure the location of the collection expression
                    // properly corresponds to the equivalent pieces of the collection initializer.
                    return UseCollectionExpressionHelpers.ReplaceWithCollectionExpression(
                        sourceText, initializer, totalConversion);
                }
            }

            static TNode ShiftNode<TNode>(
                TNode node,
                SyntaxNode anchor,
                string preferredIndentation)
            {
                // first 
            }
        }

        private static CollectionElementSyntax CreateCollectionElement(
            Match<StatementSyntax>? match,
            ExpressionSyntax expression)
        {
            return match?.UseSpread is true ? SpreadElement(expression) : ExpressionElement(expression);
        }

        private static ExpressionSyntax ConvertExpression(ExpressionSyntax expression)
            => expression switch
            {
                InvocationExpressionSyntax invocation => ConvertInvocation(invocation),
                AssignmentExpressionSyntax assignment => ConvertAssignment(assignment),
                _ => throw new InvalidOperationException(),
            };

        private static AssignmentExpressionSyntax ConvertAssignment(AssignmentExpressionSyntax assignment)
        {
            var elementAccess = (ElementAccessExpressionSyntax)assignment.Left;
            return assignment.WithLeft(
                ImplicitElementAccess(elementAccess.ArgumentList));
        }

        private static ExpressionSyntax ConvertInvocation(InvocationExpressionSyntax invocation)
        {
            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count == 1)
            {
                // Assignment expressions in a collection initializer will cause the compiler to 
                // report an error.  This is because { a = b } is the form for an object initializer,
                // and the two forms are not allowed to mix/match.  Parenthesize the assignment to
                // avoid the ambiguity.
                var expression = arguments[0].Expression;
                return SyntaxFacts.IsAssignmentExpression(expression.Kind())
                    ? ParenthesizedExpression(expression)
                    : expression;
            }

            return InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                Token(SyntaxKind.OpenBraceToken).WithoutTrivia(),
                SeparatedList(
                    arguments.Select(a => a.Expression),
                    arguments.GetSeparators()),
                Token(SyntaxKind.CloseBraceToken).WithoutTrivia());
        }

        private static bool MakeMultiLine(
            SourceText sourceText,
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            int wrappingLength)
        {
            var totalLength = 0;
            if (objectCreation.Initializer != null)
            {
                foreach (var expression in objectCreation.Initializer.Expressions)
                    totalLength += expression.Span.Length;
            }

            foreach (var (statement, _) in matches)
            {
                // if the statement we're replacing has any comments on it, then we need to be multiline to give them an
                // appropriate place to go.
                if (statement.GetLeadingTrivia().Any(static t => t.IsSingleOrMultiLineComment()) ||
                    statement.GetTrailingTrivia().Any(static t => t.IsSingleOrMultiLineComment()))
                {
                    return true;
                }

                foreach (var component in GetElementComponents(statement))
                {
                    // if any of the expressions we're adding are multiline, then make things multiline.
                    if (!sourceText.AreOnSameLine(component.GetFirstToken(), component.GetLastToken()))
                        return true;

                    totalLength += component.Span.Length;
                }
            }

            return totalLength > wrappingLength;

            static IEnumerable<SyntaxNode> GetElementComponents(StatementSyntax statement)
            {
                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    yield return expressionStatement.Expression;
                }
                else if (statement is ForEachStatementSyntax foreachStatement)
                {
                    yield return foreachStatement.Expression;
                }
                else if (statement is IfStatementSyntax ifStatement)
                {
                    yield return ifStatement.Condition;
                    yield return UnwrapEmbeddedStatement(ifStatement.Statement);
                    if (ifStatement.Else != null)
                        yield return UnwrapEmbeddedStatement(ifStatement.Else.Statement);
                }
            }
        }

        private static StatementSyntax UnwrapEmbeddedStatement(StatementSyntax statement)
            => statement is BlockSyntax { Statements: [var innerStatement] } ? innerStatement : statement;

        public static SeparatedSyntaxList<TNode> AddLineBreaks<TNode>(
            SeparatedSyntaxList<TNode> nodes, bool includeFinalLineBreak)
            where TNode : SyntaxNode
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            var nodeOrTokenList = nodes.GetWithSeparators();
            foreach (var item in nodeOrTokenList)
            {
                var addLineBreak = item.IsToken || (includeFinalLineBreak && item == nodeOrTokenList.Last());
                if (addLineBreak && item.GetTrailingTrivia() is not [.., (kind: SyntaxKind.EndOfLineTrivia)])
                {
                    nodesAndTokens.Add(item.WithAppendedTrailingTrivia(ElasticCarriageReturnLineFeed));
                }
                else
                {
                    nodesAndTokens.Add(item);
                }
            }

            return SeparatedList<TNode>(nodesAndTokens);
        }

        private static (TElementSyntax element, ExpressionSyntax anchorExpression) CreateElement<TElementSyntax>(
            Match<StatementSyntax> match,
            Func<Match<StatementSyntax>?, ExpressionSyntax, TElementSyntax> createElement)
            where TElementSyntax : SyntaxNode
        {
            var statement = match.Statement;

            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                return (createElement(match, ConvertExpression(expressionStatement.Expression).WithoutTrivia()), expressionStatement.Expression);
            }
            else if (statement is ForEachStatementSyntax foreachStatement)
            {
                return (createElement(match, foreachStatement.Expression.WithoutTrivia()), foreachStatement.Expression);
            }
            else if (statement is IfStatementSyntax ifStatement)
            {
                var anchorExpression = ifStatement.Condition;
                var trueStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Statement);

                if (ifStatement.Else is null)
                {
                    // Create: x ? [y] : []
                    var expression = ConditionalExpression(
                        ifStatement.Condition.Parenthesize(),
                        CollectionExpression(SingletonSeparatedList<CollectionElementSyntax>(ExpressionElement(ConvertExpression(trueStatement.Expression)))),
                        CollectionExpression());
                    return (createElement(match, expression), anchorExpression);
                }
                else
                {
                    // Create: x ? y : z
                    var falseStatement = (ExpressionStatementSyntax)UnwrapEmbeddedStatement(ifStatement.Else.Statement);
                    var expression = ConditionalExpression(ifStatement.Condition.Parenthesize(), ConvertExpression(trueStatement.Expression), ConvertExpression(falseStatement.Expression));
                    return (createElement(match, expression), anchorExpression);
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static SeparatedSyntaxList<TElement> CreateElements<TElement>(
            BaseObjectCreationExpressionSyntax objectCreation,
            ImmutableArray<Match<StatementSyntax>> matches,
            Func<Match<StatementSyntax>?, ExpressionSyntax, TElement> createElement)
            where TElement : SyntaxNode
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            UseInitializerHelpers.AddExistingItems(
                objectCreation, nodesAndTokens, addTrailingComma: matches.Length > 0, createElement);

            for (var i = 0; i < matches.Length; i++)
            {
                var match = matches[i];
                var statement = match.Statement;

                var trivia = statement.GetLeadingTrivia();
                var leadingTrivia = i == 0 ? trivia.WithoutLeadingBlankLines() : trivia;

                var semicolon = statement is ExpressionStatementSyntax expressionStatement ? expressionStatement.SemicolonToken : default;
                var trailingTrivia = semicolon.TrailingTrivia.Contains(static t => t.IsSingleOrMultiLineComment())
                    ? semicolon.TrailingTrivia
                    : default;

                var element = CreateElement(match, createElement).element.WithLeadingTrivia(leadingTrivia);

                if (i < matches.Length - 1)
                {
                    nodesAndTokens.Add(element);
                    nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithTrailingTrivia(trailingTrivia));
                }
                else
                {
                    nodesAndTokens.Add(element.WithTrailingTrivia(trailingTrivia));
                }
            }

            return SeparatedList<TElement>(nodesAndTokens);
        }
    }
}
