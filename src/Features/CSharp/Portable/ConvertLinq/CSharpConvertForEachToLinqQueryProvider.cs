// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertForEachToLinqQueryProvider)), Shared]
    internal sealed class CSharpConvertForEachToLinqQueryProvider : AbstractConvertForEachToLinqQueryProvider<ForEachStatementSyntax, QueryExpressionSyntax>
    {
        // TODO actually can consider conversion for ForEachVariableStatement, need to add as a separate requirement
        // TODO what if no using System.Linq; is it required?
        // TODO what if comments are not in foreach but in related statements we're removing or modifying, e.g. return or list = new List<int>()
        // TODO test: foreach(var q in largequeryexpression1) { foreach(var q1 in largequeryexpression2) {....}}
        protected override ForEachStatementSyntax FindNodeToRefactor(SyntaxNode root, TextSpan span)
            => root.FindNode(span) as ForEachStatementSyntax; // TODO depending on performance, consider using FirstAncestorOrSelf

        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

        protected override string Title => CSharpFeaturesResources.Convert_to_query;

        protected override bool TryConvert(
            ForEachStatementSyntax forEachStatement,
            Workspace workspace,
            SemanticModel semanticModel,
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken,
            out SyntaxEditor editor)
                => new Converter(semanticModel, semanticFacts, forEachStatement, workspace, cancellationToken).TryConvert(out editor);

        private class Converter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ISemanticFactsService _semanticFacts;
            private readonly CancellationToken _cancellationToken;
            private readonly Workspace _workspace;
            private readonly ForEachStatementSyntax _forEachStatement;

            // TODO adjust parameters order in this method and the method in the parent class
            public Converter(SemanticModel semanticModel, ISemanticFactsService semanticFacts, ForEachStatementSyntax forEachStatement, Workspace workspace, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _semanticFacts = semanticFacts;
                _forEachStatement = forEachStatement;
                _workspace = workspace;
                _cancellationToken = cancellationToken;
            }

            public bool TryConvert(out SyntaxEditor editor)
            {
                // Do not try refactoring queries with comments or conditional compilation in them.
                // We can consider supporting queries with comments in the future.
                if (_forEachStatement.DescendantTrivia().Any(trivia => trivia.MatchesKind(
                        SyntaxKind.SingleLineCommentTrivia,
                        SyntaxKind.MultiLineCommentTrivia,
                        SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                    _forEachStatement.ContainsDirectives))
                {
                    editor = default;
                    return false;
                }

                var (queryClauses, identifiers, statements) = CreateForEachInfo(_forEachStatement);

                if (statements.Length == 1 && TryConvertToSpecificCase(statements.Single(), queryClauses, out editor))
                {
                    return true;
                }

                // No sense to convert a single foreach to foreach over the same collection
                if (queryClauses.Length >= 1)
                {
                    var symbolNames = new HashSet<string>(statements.SelectMany(statement => _semanticModel.AnalyzeDataFlow(statement).ReadInside).Select(symbol => symbol.Name));
                    var identifiersUsedInBody = identifiers.Where(identifier => symbolNames.Contains(identifier.ValueText));
                    editor = CreateDefaultEditor();
                    editor.ReplaceNode(_forEachStatement, ConvertToDefault(_forEachStatement, queryClauses, identifiersUsedInBody, statements));
                    return true;
                }

                editor = default;
                return false;
            }

            private static (ImmutableArray<QueryClauseSyntax> QueryClauses, ImmutableArray<SyntaxToken> Identifiers, ImmutableArray<StatementSyntax> Statements) CreateForEachInfo(ForEachStatementSyntax forEachStatement)
            {
                var identifiers = new List<SyntaxToken>();
                identifiers.Add(forEachStatement.Identifier);
                var queryClauses = new List<QueryClauseSyntax>();
                var current = forEachStatement.Statement;
                StatementSyntax[] statements = null;

                var flag = true;
                while (flag)
                {
                    switch (current.Kind())
                    {
                        case SyntaxKind.Block:
                            var block = (BlockSyntax)current;
                            var array = block.Statements.ToArray();
                            // will process the last statement later on
                            for (int i = 0; i < array.Length - 1; i++)
                            {
                                var statement = array[i];
                                if (statement.Kind() == SyntaxKind.LocalDeclarationStatement)
                                {
                                    foreach (var variable in ((LocalDeclarationStatementSyntax)statement).Declaration.Variables)
                                    {
                                        queryClauses.Add(SyntaxFactory.LetClause(variable.Identifier, variable.Initializer.Value));
                                        identifiers.Add(variable.Identifier);
                                    }
                                }
                                else
                                {
                                    statements = array.Skip(i).ToArray();
                                    flag = false;
                                    break;
                                }
                            }

                            // process the last statement separately
                            current = array.Last();
                            break;

                        case SyntaxKind.ForEachStatement:
                            var currentForEachStatement = (ForEachStatementSyntax)current;
                            identifiers.Add(currentForEachStatement.Identifier);
                            queryClauses.Add(CreateFromClause(currentForEachStatement));
                            current = forEachStatement.Statement;
                            break;

                        case SyntaxKind.IfStatement:
                            var ifStatement = (IfStatementSyntax)current;
                            if (ifStatement.Else == null)
                            {
                                queryClauses.Add(SyntaxFactory.WhereClause(ifStatement.Condition));
                                current = ifStatement.Statement;
                                break;
                            }
                            else
                            {
                                statements = new[] { current };
                                flag = false;
                                break;
                            }

                        default:
                            statements = new[] { current };
                            flag = false;
                            break;
                    }
                }

                return (queryClauses.ToImmutableArray(), identifiers.ToImmutableArray(), statements.ToImmutableArray());
            }

            private SyntaxEditor CreateDefaultEditor() => new SyntaxEditor(_semanticModel.SyntaxTree.GetRoot(_cancellationToken), _workspace);

            private bool TryConvertToSpecificCase(StatementSyntax residuaryStatement, IEnumerable<QueryClauseSyntax> queryClauses, out SyntaxEditor editor)
            {
                switch (residuaryStatement.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        var experessionStatement = (ExpressionStatementSyntax)residuaryStatement;
                        switch (experessionStatement.Expression.Kind())
                        {
                            // TODO should we consider a = a + 1;?
                            case SyntaxKind.PostIncrementExpression:
                                editor = CreateDefaultEditor();
                                // No matter what can be used as the last select statement for the case of Count. We use SyntaxFactory.IdentifierName(_forEachStatement.Identifier).
                                new ConvertToCountSyntaxEditor(_semanticModel, _forEachStatement, _cancellationToken).
                                    Convert(experessionStatement, SyntaxFactory.IdentifierName(_forEachStatement.Identifier), queryClauses, ((PostfixUnaryExpressionSyntax)experessionStatement.Expression).Operand, editor);
                                return true;

                            case SyntaxKind.InvocationExpression:
                                var invocationExpression = (InvocationExpressionSyntax)experessionStatement.Expression;
                                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                    _semanticModel.GetSymbolInfo(memberAccessExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                    IsList(methodSymbol.ContainingType) &&
                                    methodSymbol.Name.Equals(nameof(IList.Add)) &&
                                    methodSymbol.Parameters.Length == 1)
                                {
                                    editor = CreateDefaultEditor();
                                    new ConvertToListSyntaxEditor(_semanticModel, _forEachStatement, _cancellationToken).
                                        Convert(experessionStatement, invocationExpression.ArgumentList.Arguments.Single().Expression, queryClauses, memberAccessExpression.Expression, editor);
                                    return true;
                                }

                                break;
                        }

                        break;

                    case SyntaxKind.YieldReturnStatement:
                        var memberDeclaration = FindParentMemberDeclarationNode(_forEachStatement, out _);
                        var yieldStatements = memberDeclaration.DescendantNodes().OfType<YieldStatementSyntax>();

                        if (_forEachStatement.IsParentKind(SyntaxKind.Block) &&
                            _forEachStatement.Parent.Parent == memberDeclaration)
                        {
                            var statementsOnBlockWithForEach = ((BlockSyntax)_forEachStatement.Parent).Statements;
                            var lastStatement = statementsOnBlockWithForEach.Last();
                            if (yieldStatements.Count() == 1 && lastStatement == _forEachStatement)
                            {
                                editor = CreateReplaceYieldReturn(residuaryStatement, queryClauses);
                                return true;
                            }

                            // foreach()
                            // {
                            //   yield return ...;
                            // }
                            // yield break;
                            // end of member
                            if (yieldStatements.Count() == 2 &&
                                lastStatement.Kind() == SyntaxKind.YieldBreakStatement &&
                                statementsOnBlockWithForEach.ElementAt(statementsOnBlockWithForEach.Count - 2) == _forEachStatement)
                            {
                                editor = CreateReplaceYieldReturn(residuaryStatement, queryClauses);
                                // remove yield break
                                editor.RemoveNode(lastStatement);
                                return true;
                            }
                        }

                        break;
                }

                editor = default;
                return false;
            }

            private SyntaxEditor CreateReplaceYieldReturn(StatementSyntax residuaryStatement, IEnumerable<QueryClauseSyntax> queryClauses)
            {
                var editor = CreateDefaultEditor();
                var queryExpression = CreateQueryExpression(_forEachStatement, queryClauses, ((YieldStatementSyntax)residuaryStatement).Expression);
                editor.ReplaceNode(_forEachStatement, SyntaxFactory.ReturnStatement(queryExpression).WithAdditionalAnnotations(Formatter.Annotation));
                return editor;
            }

            private static StatementSyntax ConvertToDefault(
                ForEachStatementSyntax forEachStatement, 
                IEnumerable<QueryClauseSyntax> queryClauses, 
                IEnumerable<SyntaxToken> identifiers, 
                IEnumerable<StatementSyntax> lastStatements)
            {
                var identifiersCount = identifiers.Count();
                var block = WrapWithBlockIfNecessary(lastStatements.Select(statement => statement.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine(Environment.NewLine))));

                if (identifiersCount == 0)
                {
                    // Generate foreach(var _ ... select (a,b))
                    var queryExpression1 = CreateQueryExpression(forEachStatement, queryClauses, SyntaxFactory.AnonymousObjectCreationExpression());
                    return SyntaxFactory.ForEachStatement(VarNameIdentifier, SyntaxFactory.Identifier("_"), queryExpression1, block);
                }
                else if (identifiersCount == 1)
                {
                    return
                        SyntaxFactory.ForEachStatement(
                            VarNameIdentifier,
                            identifiers.Single(),
                            CreateQueryExpression(forEachStatement, queryClauses, SyntaxFactory.IdentifierName(identifiers.Single())),
                            block);
                }
                else
                {
                    var selectExpression = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier)))));
                    var queryExpression = CreateQueryExpression(forEachStatement, queryClauses, selectExpression);
                    var declaration = SyntaxFactory.DeclarationExpression(VarNameIdentifier, SyntaxFactory.ParenthesizedVariableDesignation(
                        SyntaxFactory.SeparatedList<VariableDesignationSyntax>(identifiers.Select(identifier => SyntaxFactory.SingleVariableDesignation(identifier)))));

                    return SyntaxFactory.ForEachVariableStatement(declaration, queryExpression, block);
                }
            }

            private static QueryExpressionSyntax CreateQueryExpression(ForEachStatementSyntax forEachStatement, IEnumerable<QueryClauseSyntax> queryClauses, ExpressionSyntax selectExpression)
                => SyntaxFactory.QueryExpression(
                    CreateFromClause(forEachStatement),
                    SyntaxFactory.QueryBody(
                        SyntaxFactory.List(queryClauses.Select(qc => qc.WithoutTrivia())),
                        // The current coverage of foreach statements to support does not need to use query continuations.
                        SyntaxFactory.SelectClause(selectExpression).WithoutTrivia(), continuation: null).WithoutTrivia()).WithAdditionalAnnotations(Formatter.Annotation);

            private static FromClauseSyntax CreateFromClause(ForEachStatementSyntax forEachStatement)
                => SyntaxFactory.FromClause(
                    forEachStatement.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "var" ? null : forEachStatement.Type,
                    forEachStatement.Identifier,
                    forEachStatement.Expression);

            private static BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
                => (statements.Count() == 1 && statements.Single() is BlockSyntax block) ? block : SyntaxFactory.Block(statements);

            // Borrowed from another provider. Share?
            // We may assume that the query is defined within a method, field, property and so on and it is declare just once.
            private SyntaxNode FindParentMemberDeclarationNode(SyntaxNode node, out ISymbol declaredSymbol)
            {
                declaredSymbol = _semanticModel.GetEnclosingSymbol(node.SpanStart, _cancellationToken);
                return declaredSymbol.DeclaringSyntaxReferences.Single().GetSyntax();
            }

            // Borrowed from another provider. Share?
            private bool IsList(ITypeSymbol typeSymbol)
                => Equals(typeSymbol.OriginalDefinition, _semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));

            // TODO maybe it should be converter and no other converter class is needed
            // TODO maybe move it out of C#
            private abstract class AbstractConvertToMethodSyntaxEditor
            {
                protected readonly SemanticModel _semanticModel;
                protected readonly CancellationToken _cancellationToken;
                protected readonly ForEachStatementSyntax _forEachStatement;

                public AbstractConvertToMethodSyntaxEditor(SemanticModel semanticModel, ForEachStatementSyntax forEachStatement, CancellationToken cancellationToken)
                {
                    _semanticModel = semanticModel;
                    _forEachStatement = forEachStatement;
                    _cancellationToken = cancellationToken;
                }

                protected abstract string MethodName { get; }

                protected abstract bool CanReplaceInitializion(ExpressionSyntax expressionSyntax);

                protected abstract void ConvertToDefault(QueryExpressionSyntax queryExpression, ExpressionSyntax expression, SyntaxEditor editor);

                public void Convert(
                    ExpressionStatementSyntax expressionStatement,
                    ExpressionSyntax selectExpression,
                    IEnumerable<QueryClauseSyntax> queryClauses,
                    //                    QueryExpressionSyntax queryExpression,
                    ExpressionSyntax expression,
                    SyntaxEditor editor)
                {
                    var queryExpression = CreateQueryExpression(_forEachStatement, queryClauses, selectExpression);
                    var previous = FindPreviousStatementInBlock(_forEachStatement);

                    switch (previous?.Kind())
                    {
                        case SyntaxKind.LocalDeclarationStatement:
                            var variables = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables;
                            var lastDeclaration = variables.Last();
                            if (expression is IdentifierNameSyntax identifierName &&
                                lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) &&
                                CanReplaceInitializion(lastDeclaration.Initializer.Value))
                            {
                                if (IsFollowedByReturnOfLocal(expression, out var returnStatement))
                                {
                                    ConvertToMethodAndReturn(queryExpression, returnStatement, editor);

                                    if (variables.Count == 1)
                                    {
                                        editor.RemoveNode(previous);
                                    }
                                    else
                                    {
                                        editor.RemoveNode(lastDeclaration);
                                    }

                                    return;
                                }

                                ConvertToMemberAccess(lastDeclaration.Initializer.Value, queryExpression, editor);
                                return;
                            }

                            break;

                        case SyntaxKind.ExpressionStatement:
                            if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                                SymbolEquivalenceComparer.Instance.Equals(_semanticModel.GetSymbolInfo(assignmentExpression.Left, _cancellationToken).Symbol, _semanticModel.GetSymbolInfo(expression, _cancellationToken).Symbol) &&
                                CanReplaceInitializion(assignmentExpression.Right))
                            {
                                var nextStatement = FindNextStatementInBlock(_forEachStatement);
                                if (IsFollowedByReturnOfLocal(expression, out var returnStatement))
                                {
                                    ConvertToMethodAndReturn(queryExpression, returnStatement, editor);
                                    editor.RemoveNode(previous);
                                    return;
                                }

                                ConvertToMemberAccess(assignmentExpression.Right, queryExpression, editor);
                                return;
                            }

                            break;
                    }

                    ConvertToDefault(queryExpression, expression, editor);
                }

                private void ConvertToMemberAccess(ExpressionSyntax expressionToReplace, QueryExpressionSyntax queryExpression, SyntaxEditor editor)
                {
                    editor.ReplaceNode(
                        expressionToReplace,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParenthesizedExpression(queryExpression),
                                SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation));
                    editor.RemoveNode(_forEachStatement);
                }

                protected void ConvertToMethodAndReturn(QueryExpressionSyntax queryExpression, ReturnStatementSyntax returnStatement, SyntaxEditor editor)
                {
                    editor.RemoveNode(_forEachStatement);
                    editor.ReplaceNode(returnStatement.Expression,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParenthesizedExpression(queryExpression),
                                SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation));
                }

                private bool IsFollowedByReturnOfLocal(ExpressionSyntax expression, out ReturnStatementSyntax returnStatement)
                {
                    var expresisonSymbol = _semanticModel.GetSymbolInfo(expression, _cancellationToken).Symbol;
                    if (expresisonSymbol is ILocalSymbol)
                    {
                        var nextStatement = FindNextStatementInBlock(_forEachStatement);
                        if (nextStatement is ReturnStatementSyntax returnStatementCandidate &&
                            SymbolEquivalenceComparer.Instance.Equals(expresisonSymbol, _semanticModel.GetSymbolInfo(returnStatementCandidate.Expression, _cancellationToken).Symbol))
                        {
                            returnStatement = returnStatementCandidate;
                            return true;
                        }
                    }

                    returnStatement = default;
                    return false;
                }

                private static StatementSyntax FindPreviousStatementInBlock(StatementSyntax statement)
                {
                    if (statement.Parent is BlockSyntax block)
                    {
                        var index = block.Statements.IndexOf(statement);
                        if (index > 0)
                        {
                            return block.Statements[index - 1];
                        }
                    }

                    return null;
                }

                private static StatementSyntax FindNextStatementInBlock(StatementSyntax statement)
                {
                    if (statement.Parent is BlockSyntax block)
                    {
                        var index = block.Statements.IndexOf(statement);
                        if (index >= 0 && index < block.Statements.Count - 1)
                        {
                            return block.Statements[index + 1];
                        }
                    }

                    return null;
                }
            }

            private sealed class ConvertToListSyntaxEditor : AbstractConvertToMethodSyntaxEditor
            {
                public ConvertToListSyntaxEditor(SemanticModel semanticModel, ForEachStatementSyntax forEachStatement, CancellationToken cancellationToken)
                    : base(semanticModel, forEachStatement, cancellationToken) { }

                protected override string MethodName => nameof(Enumerable.ToList);

                protected override bool CanReplaceInitializion(ExpressionSyntax expression)
                    => expression is ObjectCreationExpressionSyntax objectCreationExpression &&
                    _semanticModel.GetSymbolInfo(objectCreationExpression.Type, _cancellationToken).Symbol is ITypeSymbol typeSymbol &&
                    IsList(typeSymbol);

                protected override void ConvertToDefault(QueryExpressionSyntax queryExpression, ExpressionSyntax expression, SyntaxEditor editor)
                {
                    editor.ReplaceNode(
                        _forEachStatement,
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    expression,
                                    SyntaxFactory.IdentifierName(nameof(List<object>.AddRange))),
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(queryExpression)))))
                                .WithAdditionalAnnotations(Formatter.Annotation));
                }

                // Borrowed from another provider. Share?
                private bool IsList(ITypeSymbol typeSymbol)
                    => Equals(typeSymbol.OriginalDefinition, _semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));
            }

            private sealed class ConvertToCountSyntaxEditor : AbstractConvertToMethodSyntaxEditor
            {
                public ConvertToCountSyntaxEditor(SemanticModel semanticModel, ForEachStatementSyntax forEachStatement, CancellationToken cancellationToken)
                    : base(semanticModel, forEachStatement, cancellationToken) { }

                protected override string MethodName => nameof(Enumerable.Count);

                protected override bool CanReplaceInitializion(ExpressionSyntax expression)
                    => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

                protected override void ConvertToDefault(QueryExpressionSyntax queryExpression, ExpressionSyntax expression, SyntaxEditor editor)
                {
                    editor.ReplaceNode(
                        _forEachStatement,
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.AddAssignmentExpression,
                                expression,
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ParenthesizedExpression(queryExpression),
                                        SyntaxFactory.IdentifierName(MethodName)))))
                                .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
        }
    }
}
