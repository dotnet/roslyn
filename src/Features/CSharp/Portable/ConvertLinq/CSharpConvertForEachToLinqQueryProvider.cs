// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
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

                var queryClauses = CreateQueryClauses(out var identifiers, out var lastStatements);

                if (lastStatements.Count() == 1 && TryConvertToSpecificCase(lastStatements.Single(), queryClauses, out editor))
                {
                    return true;
                }

                // No sense to convert a single foreach to foreach over the same collection
                if (queryClauses.Count >= 1)
                {
                    editor = ConvertToDefault(queryClauses, identifiers, lastStatements);
                    return true;
                }

                editor = default;
                return false;
            }

            private SyntaxEditor CreateDefaultEditor() => new SyntaxEditor(_semanticModel.SyntaxTree.GetRoot(_cancellationToken), _workspace);

            // TODO signature is not ideal: we always return queryClauses but setting lastStatements to something various
            private List<QueryClauseSyntax> CreateQueryClauses(out List<SyntaxToken> identifiers, out StatementSyntax[] lastStatements)
            {
                identifiers = new List<SyntaxToken>();
                identifiers.Add(_forEachStatement.Identifier);
                var queryClauses = new List<QueryClauseSyntax>();
                var current = _forEachStatement.Statement;

                while (true)
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
                                    lastStatements = array.Skip(i).ToArray();
                                    return queryClauses;
                                }
                            }

                            // process the last statement separately
                            current = array.Last();
                            break;

                        case SyntaxKind.ForEachStatement:
                            var forEachStatement = (ForEachStatementSyntax)current;
                            identifiers.Add(forEachStatement.Identifier);
                            queryClauses.Add(CreateFromClause(forEachStatement));
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
                                lastStatements = new[] { current };
                                return queryClauses;
                            }

                        default:
                            lastStatements = new[] { current };
                            return queryClauses;
                    }
                }
            }

            private bool TryConvertToSpecificCase(StatementSyntax residuaryStatement, List<QueryClauseSyntax> queryClauses, out SyntaxEditor editor)
            {
                switch (residuaryStatement.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        var experessionStatement = (ExpressionStatementSyntax)residuaryStatement;
                        switch (experessionStatement.Expression.Kind())
                        {
                            // TODO should we consider a = a + 1;?
                            case SyntaxKind.PostIncrementExpression:
                                // No matter what can be used as the last select statement for the case of Count. We use SyntaxFactory.IdentifierName(_forEachStatement.Identifier).
                                var queryExpression1 = CreateQueryExpression(queryClauses, SyntaxFactory.IdentifierName(_forEachStatement.Identifier));
                                editor = new ConvertToCountSyntaxEditor(_semanticModel, _semanticFacts, _forEachStatement, _workspace, _cancellationToken).
                                    Convert(experessionStatement, queryExpression1, ((PostfixUnaryExpressionSyntax)experessionStatement.Expression).Operand);
                                return true;

                            case SyntaxKind.InvocationExpression:
                                var invocationExpression = (InvocationExpressionSyntax)experessionStatement.Expression;
                                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                    _semanticModel.GetSymbolInfo(memberAccessExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                    IsList(methodSymbol.ContainingType) &&
                                    methodSymbol.Name.Equals(nameof(IList.Add)) &&
                                    methodSymbol.Parameters.Length == 1)
                                {
                                    var queryExpression2 = CreateQueryExpression(queryClauses, invocationExpression.ArgumentList.Arguments.Single().Expression);
                                    editor = new ConvertToListSyntaxEditor(_semanticModel, _semanticFacts, _forEachStatement, _workspace, _cancellationToken).
                                        Convert(experessionStatement, queryExpression2, memberAccessExpression.Expression);
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

            private SyntaxEditor CreateReplaceYieldReturn(StatementSyntax residuaryStatement, List<QueryClauseSyntax> queryClauses)
            {
                var editor = CreateDefaultEditor();
                var queryExpression = CreateQueryExpression(queryClauses, ((YieldStatementSyntax)residuaryStatement).Expression);
                editor.ReplaceNode(_forEachStatement, SyntaxFactory.ReturnStatement(queryExpression).WithAdditionalAnnotations(Formatter.Annotation));
                return editor;
            }

            private SyntaxEditor ConvertToDefault(List<QueryClauseSyntax> queryClauses, List<SyntaxToken> identifiers, IEnumerable<StatementSyntax> lastStatements)
            {
                var symbolNames = new HashSet<string>(lastStatements.SelectMany(statement => _semanticModel.AnalyzeDataFlow(statement).ReadInside).Select(symbol => symbol.Name));
                identifiers = identifiers.Where(identifier => symbolNames.Contains(identifier.ValueText)).ToList();

                var editor = CreateDefaultEditor();
                if (identifiers.Count() == 1)
                {
                    editor.ReplaceNode(
                        _forEachStatement,
                        SyntaxFactory.ForEachStatement(
                            VarNameIdentifier,
                            identifiers.Single(),
                            CreateQueryExpression(queryClauses, SyntaxFactory.IdentifierName(identifiers.Single())),
                            WrapWithBlockIfNecessary(lastStatements)));
                }
                else
                {
                    // TODO check that 0 expressions is processed properly
                    if (identifiers.Count >= 2 && CanReplaceExpressions(lastStatements, identifiers, out var replacingExpressions))
                    {
                        var queryExpression1 = CreateQueryExpression(queryClauses, replacingExpressions.First());
                        // TODO naming
                        var anonymousTypeName1 = "anonymous";
                        var freeName = GetFreeSymbolName(anonymousTypeName1);
                        var block1 = WrapWithBlockIfNecessary(lastStatements.Select(statement => statement.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine(Environment.NewLine))));
                        editor.ReplaceNode(_forEachStatement, SyntaxFactory.ForEachStatement(VarNameIdentifier, freeName, queryExpression1, block1));
                        foreach(var replacingExpression in replacingExpressions)
                        {
                            // TODO reuse
                            // TODO unit test
                            editor.ReplaceNode(replacingExpression, SyntaxFactory.IdentifierName(freeName));
                        }
                    }
                    else
                    {
                        // This supports the case with no variables used.
                        // In this case, it generates:
                        // foreach(var anonymous ... select new {})
                        var selectExpression = SyntaxFactory.AnonymousObjectCreationExpression(
                                SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.IdentifierName(identifier)))));
                        var queryExpression = CreateQueryExpression(queryClauses, selectExpression);

                        var anonymousTypeName = "anonymous";
                        var freeName = GetFreeSymbolName(anonymousTypeName);
                        var block = WrapWithBlockIfNecessary(lastStatements.Select(statement => statement.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine(Environment.NewLine))));

                        // Reversing identifiers because adding declarations from bottom to top.
                        identifiers.Reverse();
                        foreach (var identifier in identifiers)
                        {
                            var localDeclaration =
                                SyntaxFactory.LocalDeclarationStatement(
                                    SyntaxFactory.VariableDeclaration(
                                        VarNameIdentifier,
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.VariableDeclarator(
                                                identifier,
                                                argumentList: null,
                                                SyntaxFactory.EqualsValueClause(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(freeName),
                                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                                        SyntaxFactory.IdentifierName(identifier)))))));
                            block = AddToBlockTop(localDeclaration, block);
                        }

                        editor.ReplaceNode(_forEachStatement, SyntaxFactory.ForEachStatement(VarNameIdentifier, freeName, queryExpression, block));
                    }
                }

                return editor;
            }

            // TODO unit tests
            private bool CanReplaceExpressions(IEnumerable<StatementSyntax> statements, IEnumerable<SyntaxToken> identifiers, out IEnumerable<ExpressionSyntax> expressions)
            {
                // TODO there is a problem: Console.WriteLine is also an expression
                // instead of this, we should find all identifiers and find the smallest expression covering all identifiers.
                // then, need to copmare all such expressions
                // this could be essential for the perofrmance
                var allExpressions = statements.SelectMany(statement => statement.DescendantNodes(node => !(node is ExpressionSyntax))).OfType<ExpressionSyntax>();
                var identifierNamesHashset = new HashSet<string>(identifiers.Select(identifier => identifier.ValueText));
                var list = new List<ExpressionSyntax>();

                foreach (var expression in allExpressions)
                {
                    var variablesRead = _semanticModel.AnalyzeDataFlow(expression).ReadInside;

                    if (!variablesRead.Any(variable => identifierNamesHashset.Contains(variable.Name)))
                    {
                        continue;
                    }

                    var descendantNodes = expression.DescendantNodes();
                    if (descendantNodes.OfType<InvocationExpressionSyntax>().Any() || descendantNodes.OfType<MemberAccessExpressionSyntax>().Any())
                    {
                        expressions = default;
                        return false;
                    }

                    list.Add(expression);
                }

                // TODO 0, 1 or many expressions
                if (list.Count > 1)
                {
                    var firstExpression = list.First();
                    var firstExpressionSymbol = _semanticModel.GetSymbolInfo(firstExpression, _cancellationToken).Symbol;

                    foreach (var expression in list)
                    {
                        var symbol = _semanticModel.GetSymbolInfo(expression, _cancellationToken).Symbol;
                        if (!(SymbolEquivalenceComparer.Instance.Equals(firstExpressionSymbol, symbol)))
                        {
                            expressions = default;
                            return false;
                        }
                    }
                }

                expressions = list;
                return true;
            }

            private QueryExpressionSyntax CreateQueryExpression(
                IEnumerable<QueryClauseSyntax> queryClauses,
                ExpressionSyntax selectExpression)
            {
                return SyntaxFactory.QueryExpression(
                    CreateFromClause(_forEachStatement),
                    SyntaxFactory.QueryBody(
                        SyntaxFactory.List(queryClauses.Select(qc => qc.WithoutTrivia())),
                        // The current coverage of foreach statements to support does not need to use query continuations.
                        SyntaxFactory.SelectClause(selectExpression).WithoutTrivia(), continuation: null).WithoutTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
            }

            private static FromClauseSyntax CreateFromClause(ForEachStatementSyntax forEachStatement)
                => SyntaxFactory.FromClause(
                    forEachStatement.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "var" ? null : forEachStatement.Type,
                    forEachStatement.Identifier,
                    forEachStatement.Expression);

            // TODO copied from CSharpConvertLinqQueryToForEachProvider. Share?
            private static BlockSyntax AddToBlockTop(StatementSyntax newStatement, StatementSyntax statement)
            {
                if (statement is BlockSyntax block)
                {
                    return block.WithStatements(block.Statements.Insert(0, newStatement));
                }
                else
                {
                    return SyntaxFactory.Block(newStatement, statement);
                }
            }

            private static BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
                => (statements.Count() == 1 && statements.Single() is BlockSyntax block) ? block : SyntaxFactory.Block(statements);

            private SyntaxToken GetFreeSymbolName(string prefix)
                => _semanticFacts.GenerateUniqueName(_semanticModel, _forEachStatement, containerOpt: null, baseName: prefix, Enumerable.Empty<string>(), _cancellationToken);

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
                protected readonly ISemanticFactsService _semanticFacts;
                protected readonly CancellationToken _cancellationToken;
                protected readonly Workspace _workspace;
                protected readonly ForEachStatementSyntax _forEachStatement;

                public AbstractConvertToMethodSyntaxEditor(SemanticModel semanticModel, ISemanticFactsService semanticFacts, ForEachStatementSyntax forEachStatement, Workspace workspace, CancellationToken cancellationToken)
                {

                    _semanticModel = semanticModel;
                    _semanticFacts = semanticFacts;
                    _forEachStatement = forEachStatement;
                    _workspace = workspace;
                    _cancellationToken = cancellationToken;
                }

                protected abstract string MethodName { get; }

                protected abstract bool CanReplaceInitializion(ExpressionSyntax expressionSyntax);

                protected abstract SyntaxEditor ConvertToDefault(QueryExpressionSyntax queryExpression, ExpressionSyntax expression);

                public SyntaxEditor Convert(
                    ExpressionStatementSyntax expressionStatement,
                    QueryExpressionSyntax queryExpression,
                    ExpressionSyntax expression)
                {
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
                                    var editor1 = ConvertToMethodAndReturn(queryExpression, returnStatement);

                                    if (variables.Count == 1)
                                    {
                                        editor1.RemoveNode(previous);
                                    }
                                    else
                                    {
                                        editor1.RemoveNode(lastDeclaration);
                                    }

                                    return editor1;
                                }

                                return ConvertToMemberAccess(lastDeclaration.Initializer.Value, queryExpression);
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
                                    var editor2 = ConvertToMethodAndReturn(queryExpression, returnStatement);
                                    editor2.RemoveNode(previous);
                                    return editor2;
                                }

                                return ConvertToMemberAccess(assignmentExpression.Right, queryExpression);
                            }

                            break;
                    }

                    return ConvertToDefault(queryExpression, expression);
                }

                private SyntaxEditor ConvertToMemberAccess(ExpressionSyntax expressionToReplace, QueryExpressionSyntax queryExpression)
                {
                    var editor = CreateDefaultEditor();
                    editor.ReplaceNode(
                        expressionToReplace,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParenthesizedExpression(queryExpression),
                                SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation));
                    editor.RemoveNode(_forEachStatement);
                    return editor;
                }

                protected SyntaxEditor ConvertToMethodAndReturn(QueryExpressionSyntax queryExpression, ReturnStatementSyntax returnStatement)
                {
                    var editor = CreateDefaultEditor();
                    editor.RemoveNode(_forEachStatement);
                    editor.ReplaceNode(returnStatement.Expression,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParenthesizedExpression(queryExpression),
                                SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation));
                    return editor;
                }

                // TODO duplicated from the parent class
                protected SyntaxEditor CreateDefaultEditor() => new SyntaxEditor(_semanticModel.SyntaxTree.GetRoot(_cancellationToken), _workspace);

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
                public ConvertToListSyntaxEditor(SemanticModel semanticModel, ISemanticFactsService semanticFacts, ForEachStatementSyntax forEachStatement, Workspace workspace, CancellationToken cancellationToken)
                    : base(semanticModel, semanticFacts, forEachStatement, workspace, cancellationToken) { }

                protected override string MethodName => nameof(Enumerable.ToList);

                protected override bool CanReplaceInitializion(ExpressionSyntax expression)
                    => expression is ObjectCreationExpressionSyntax objectCreationExpression &&
                    _semanticModel.GetSymbolInfo(objectCreationExpression.Type, _cancellationToken).Symbol is ITypeSymbol typeSymbol &&
                    IsList(typeSymbol);

                protected override SyntaxEditor ConvertToDefault(QueryExpressionSyntax queryExpression, ExpressionSyntax expression)
                {
                    var editor = CreateDefaultEditor();
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
                    return editor;
                }

                // Borrowed from another provider. Share?
                private bool IsList(ITypeSymbol typeSymbol)
                    => Equals(typeSymbol.OriginalDefinition, _semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));
            }

            private sealed class ConvertToCountSyntaxEditor : AbstractConvertToMethodSyntaxEditor
            {
                public ConvertToCountSyntaxEditor(SemanticModel semanticModel, ISemanticFactsService semanticFacts, ForEachStatementSyntax forEachStatement, Workspace workspace, CancellationToken cancellationToken)
                    : base(semanticModel, semanticFacts, forEachStatement, workspace, cancellationToken) { }

                protected override string MethodName => nameof(Enumerable.Count);

                protected override bool CanReplaceInitializion(ExpressionSyntax expression)
                    => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

                protected override SyntaxEditor ConvertToDefault(QueryExpressionSyntax queryExpression, ExpressionSyntax expression)
                {
                    var editor = CreateDefaultEditor();
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
                    return editor;
                }
            }
        }
    }
}
