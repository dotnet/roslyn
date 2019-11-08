// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using SyntaxNodeOrTokenExtensions = Microsoft.CodeAnalysis.Shared.Extensions.SyntaxNodeOrTokenExtensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    /// <summary>
    /// Provides a conversion to query.Method() like query.ToList(), query.Count().
    /// </summary>
    internal abstract class AbstractToMethodConverter : AbstractConverter
    {
        // It is "item" for for "list.Add(item)"
        // It can be anything for "counter++". It will be ingored in the case.
        private readonly ExpressionSyntax _selectExpression;

        // It is "list" for "list.Add(item)"
        // It is "counter" for "counter++"
        private readonly ExpressionSyntax _modifyingExpression;

        // Trivia around "counter++;" or "list.Add(item);". Required to keep comments.
        private readonly SyntaxTrivia[] _trivia;

        public AbstractToMethodConverter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            ExpressionSyntax selectExpression,
            ExpressionSyntax modifyingExpression,
            SyntaxTrivia[] trivia) : base(forEachInfo)
        {
            _selectExpression = selectExpression;
            _modifyingExpression = modifyingExpression;
            _trivia = trivia;
        }

        protected abstract string MethodName { get; }

        protected abstract bool CanReplaceInitialization(ExpressionSyntax expressionSyntax, CancellationToken cancellationToken);

        protected abstract StatementSyntax CreateDefaultStatement(ExpressionSyntax queryOrLinqInvocationExpression, ExpressionSyntax expression);

        public override void Convert(SyntaxEditor editor, bool convertToQuery, CancellationToken cancellationToken)
        {
            var queryOrLinqInvocationExpression = CreateQueryExpressionOrLinqInvocation(
                _selectExpression, Enumerable.Empty<SyntaxToken>(), Enumerable.Empty<SyntaxToken>(), convertToQuery);

            var previous = ForEachInfo.ForEachStatement.GetPreviousStatement();

            if (previous is { ContainsDirectives: false })
            {
                switch (previous.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:
                        var variables = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables;
                        var lastDeclaration = variables.Last();
                        // Check if 
                        // var ...., list = new List<T>(); or var ..., counter = 0;
                        // is just before the foreach.
                        // If so, join the declaration with the query.
                        if (_modifyingExpression is IdentifierNameSyntax identifierName &&
                            lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) &&
                            CanReplaceInitialization(lastDeclaration.Initializer.Value, cancellationToken))
                        {
                            Convert(lastDeclaration.Initializer.Value, variables.Count == 1 ? (SyntaxNode)previous : lastDeclaration);
                            return;
                        }

                        break;

                    case SyntaxKind.ExpressionStatement:
                        // Check if 
                        // list = new List<T>(); or counter = 0;
                        // is just before the foreach.
                        // If so, join the assignment with the query.
                        if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                            SymbolEquivalenceComparer.Instance.Equals(
                                ForEachInfo.SemanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).Symbol,
                                ForEachInfo.SemanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol) &&
                            CanReplaceInitialization(assignmentExpression.Right, cancellationToken))
                        {
                            Convert(assignmentExpression.Right, previous);
                            return;
                        }

                        break;
                }
            }

            // At least, we already can convert to 
            // list.AddRange(query) or counter += query.Count();
            editor.ReplaceNode(
                ForEachInfo.ForEachStatement,
                CreateDefaultStatement(queryOrLinqInvocationExpression, _modifyingExpression).WithAdditionalAnnotations(Formatter.Annotation));

            return;

            void Convert(ExpressionSyntax replacingExpression, SyntaxNode nodeToRemoveIfFollowedByReturn)
            {
                SyntaxTrivia[] leadingTrivia;

                // Check if expressionAssigning is followed by a return statement.
                var expresisonSymbol = ForEachInfo.SemanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol;
                if (expresisonSymbol is ILocalSymbol &&
                    ForEachInfo.ForEachStatement.GetNextStatement() is ReturnStatementSyntax returnStatement &&
                    !returnStatement.ContainsDirectives &&
                    SymbolEquivalenceComparer.Instance.Equals(
                        expresisonSymbol, ForEachInfo.SemanticModel.GetSymbolInfo(returnStatement.Expression, cancellationToken).Symbol))
                {
                    // Input:
                    // var list = new List<T>(); or var counter = 0;
                    // foreach(...)
                    // {
                    //     ...
                    //     ...
                    //     list.Add(item); or counter++;
                    //  }
                    //  return list; or return counter;
                    //
                    //  Output:
                    //  return queryGenerated.ToList(); or return queryGenerated.Count();
                    replacingExpression = returnStatement.Expression;
                    leadingTrivia = GetTriviaFromNode(nodeToRemoveIfFollowedByReturn)
                        .Concat(SyntaxNodeOrTokenExtensions.GetTrivia(replacingExpression)).ToArray();
                    editor.RemoveNode(nodeToRemoveIfFollowedByReturn);
                }
                else
                {
                    leadingTrivia = SyntaxNodeOrTokenExtensions.GetTrivia(replacingExpression);
                }

                editor.ReplaceNode(
                    replacingExpression,
                    CreateInvocationExpression(queryOrLinqInvocationExpression)
                        .WithCommentsFrom(leadingTrivia, _trivia).KeepCommentsAndAddElasticMarkers());
                editor.RemoveNode(ForEachInfo.ForEachStatement);
            }

            static SyntaxTrivia[] GetTriviaFromVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
                => SyntaxNodeOrTokenExtensions.GetTrivia(variableDeclarator.Identifier, variableDeclarator.Initializer.EqualsToken, variableDeclarator.Initializer.Value);
            static SyntaxTrivia[] GetTriviaFromNode(SyntaxNode node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:

                        var localDeclaration = (LocalDeclarationStatementSyntax)node;
                        if (localDeclaration.Declaration.Variables.Count != 1)
                        {
                            throw ExceptionUtilities.Unreachable;
                        }

                        return new IEnumerable<SyntaxTrivia>[] {
                            SyntaxNodeOrTokenExtensions.GetTrivia(localDeclaration.Declaration.Type),
                            GetTriviaFromVariableDeclarator(localDeclaration.Declaration.Variables[0]),
                            SyntaxNodeOrTokenExtensions.GetTrivia(localDeclaration.SemicolonToken)}.Flatten().ToArray();

                    case SyntaxKind.VariableDeclarator:
                        return GetTriviaFromVariableDeclarator((VariableDeclaratorSyntax)node);

                    case SyntaxKind.ExpressionStatement:
                        if (((ExpressionStatementSyntax)node).Expression is AssignmentExpressionSyntax assignmentExpression)
                        {
                            return SyntaxNodeOrTokenExtensions.GetTrivia(
                                assignmentExpression.Left, assignmentExpression.OperatorToken, assignmentExpression.Right);
                        }

                        break;
                }

                throw ExceptionUtilities.Unreachable;
            }
        }

        // query => query.Method()
        // like query.Count() or query.ToList()
        protected InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax queryOrLinqInvocationExpression)
            => SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParenthesizedExpression(queryOrLinqInvocationExpression),
                        SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation);
    }
}
