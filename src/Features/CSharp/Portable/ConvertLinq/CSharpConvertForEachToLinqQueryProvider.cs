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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertForEachToLinqQueryProvider)), Shared]
    internal sealed class CSharpConvertForEachToLinqQueryProvider : AbstractConvertForEachToLinqQueryProvider<ForEachStatementSyntax>
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
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out IConverter converter)
                => new ConverterBuilder(semanticModel, cancellationToken).TryConvert(forEachStatement, out converter);

        private class ConverterBuilder
        {
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;

            // TODO adjust parameters order in this method and the method in the parent class
            public ConverterBuilder(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            public bool TryConvert(ForEachStatementSyntax forEachStatement, out IConverter converter)
            {
                // Do not try refactoring queries with comments or conditional compilation in them.
                // We can consider supporting queries with comments in the future.
                if (forEachStatement.DescendantTrivia().Any(trivia => trivia.MatchesKind(
                        SyntaxKind.SingleLineCommentTrivia,
                        SyntaxKind.MultiLineCommentTrivia,
                        SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                    forEachStatement.ContainsDirectives))
                {
                    converter = default;
                    return false;
                }

                var (convertingNodes, identifiers, statements) = CreateForEachInfo(forEachStatement);

                // TODO params order
                if (statements.Length == 1 && TryConvertToSpecificCase(forEachStatement, statements.Single(), convertingNodes, out converter))
                {
                    return true;
                }

                // No sense to convert a single foreach to foreach over the same collection
                if (convertingNodes.Length >= 1)
                {
                    converter = new DefaultConverter(forEachStatement, convertingNodes, statements, identifiers);
                    return true;
                }

                converter = default;
                return false;
            }

            private static (ImmutableArray<SyntaxNode> ConvertingNodes, ImmutableArray<SyntaxToken> Identifiers, ImmutableArray<StatementSyntax> Statements) CreateForEachInfo(ForEachStatementSyntax forEachStatement)
            {
                var identifiers = new List<SyntaxToken>();
                identifiers.Add(forEachStatement.Identifier);
                var convertingNodes = new List<SyntaxNode>();
                var current = forEachStatement.Statement;
                IEnumerable<StatementSyntax> statements = null;

                // Setting statements means that we stop processing.
                while (statements == null)
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
                                if (statement is LocalDeclarationStatementSyntax localDeclarationStatement)
                                {
                                    foreach (var variable in localDeclarationStatement.Declaration.Variables)
                                    {
                                        convertingNodes.Add(variable);
                                        identifiers.Add(variable.Identifier);
                                    }
                                }
                                else
                                {
                                    statements = array.Skip(i);
                                    break;
                                }
                            }

                            // process the last statement separately
                            current = array.Last();
                            break;

                        case SyntaxKind.ForEachStatement:
                            var currentForEachStatement = (ForEachStatementSyntax)current;
                            identifiers.Add(currentForEachStatement.Identifier);
                            convertingNodes.Add(currentForEachStatement);
                            current = currentForEachStatement.Statement;
                            break;

                        case SyntaxKind.IfStatement:
                            var ifStatement = (IfStatementSyntax)current;
                            if (ifStatement.Else == null)
                            {
                                convertingNodes.Add(ifStatement);
                                current = ifStatement.Statement;
                                break;
                            }
                            else
                            {
                                statements = new[] { current };
                                break;
                            }

                        default:
                            statements = new[] { current };
                            break;
                    }
                }

                return (convertingNodes.ToImmutableArray(), identifiers.ToImmutableArray(), statements.ToImmutableArray());
            }

            private bool TryConvertToSpecificCase(ForEachStatementSyntax forEachStatement, StatementSyntax residuaryStatement, IEnumerable<SyntaxNode> convertingNodes, out IConverter converter)
            {
                switch (residuaryStatement.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        var expressionStatement = (ExpressionStatementSyntax)residuaryStatement;
                        switch (expressionStatement.Expression.Kind())
                        {
                            // TODO should we consider a = a + 1;?
                            case SyntaxKind.PostIncrementExpression:
                                // No matter what can be used as the last select statement for the case of Count. We use SyntaxFactory.IdentifierName(forEachStatement.Identifier).
                                converter = new ConvertToCountSyntaxEditor(forEachStatement, convertingNodes, SyntaxFactory.IdentifierName(forEachStatement.Identifier), ((PostfixUnaryExpressionSyntax)expressionStatement.Expression).Operand);
                                return true;

                            case SyntaxKind.InvocationExpression:
                                var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;
                                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                    _semanticModel.GetSymbolInfo(memberAccessExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                    IsList(methodSymbol.ContainingType, _semanticModel) &&
                                    methodSymbol.Name.Equals(nameof(IList.Add)) &&
                                    methodSymbol.Parameters.Length == 1)
                                {
                                    converter = new ConvertToListSyntaxEditor(forEachStatement, convertingNodes, invocationExpression.ArgumentList.Arguments.Single().Expression, memberAccessExpression.Expression);
                                    return true;
                                }

                                break;
                        }

                        break;

                    case SyntaxKind.YieldReturnStatement:
                        var memberDeclaration = _semanticModel.GetEnclosingSymbol(forEachStatement.SpanStart, _cancellationToken).DeclaringSyntaxReferences.Single().GetSyntax();
                        var yieldStatements = memberDeclaration.DescendantNodes().OfType<YieldStatementSyntax>();

                        if (forEachStatement.IsParentKind(SyntaxKind.Block) &&
                            forEachStatement.Parent.Parent == memberDeclaration)
                        {
                            var statementsOnBlockWithForEach = ((BlockSyntax)forEachStatement.Parent).Statements;
                            var lastStatement = statementsOnBlockWithForEach.Last();
                            if (yieldStatements.Count() == 1 && lastStatement == forEachStatement)
                            {
                                converter = new YieldReturnConverter(forEachStatement, convertingNodes, (YieldStatementSyntax)residuaryStatement);
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
                                statementsOnBlockWithForEach.ElementAt(statementsOnBlockWithForEach.Count - 2) == forEachStatement)
                            {
                                // remove yield break
                                converter = new YieldReturnConverter(forEachStatement, convertingNodes, (YieldStatementSyntax)residuaryStatement, nodeToDelete: lastStatement);
                                return true;
                            }
                        }

                        break;
                }

                converter = default;
                return false;
            }

            private static bool IsList(ITypeSymbol typeSymbol, SemanticModel semanticModel)
                => Equals(typeSymbol.OriginalDefinition, semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));

            private abstract class AbstractConverter
            {
                protected static QueryExpressionSyntax CreateQueryExpression(ForEachStatementSyntax forEachStatement, IEnumerable<SyntaxNode> convertingNodes, ExpressionSyntax selectExpression)
                    => SyntaxFactory.QueryExpression(
                        CreateFromClause(forEachStatement),
                        SyntaxFactory.QueryBody(
                            SyntaxFactory.List(convertingNodes.Select(qc => CreateQueryClause(qc).WithoutTrivia())),
                            // The current coverage of foreach statements to support does not need to use query continuations.
                            SyntaxFactory.SelectClause(selectExpression).WithoutTrivia(), continuation: null).WithoutTrivia()).WithAdditionalAnnotations(Formatter.Annotation);

                private static QueryClauseSyntax CreateQueryClause(SyntaxNode node)
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.VariableDeclarator:
                            var variable = (VariableDeclaratorSyntax)node;
                            return SyntaxFactory.LetClause(variable.Identifier, variable.Initializer.Value);
                        case SyntaxKind.ForEachStatement:
                            return CreateFromClause((ForEachStatementSyntax)node);
                        case SyntaxKind.IfStatement:
                            return SyntaxFactory.WhereClause(((IfStatementSyntax)node).Condition);
                    }

                    throw new ArgumentException(node.Kind().ToString());
                }

                private static FromClauseSyntax CreateFromClause(ForEachStatementSyntax forEachStatement)
                    => SyntaxFactory.FromClause(
                        forEachStatement.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "var" ? null : forEachStatement.Type,
                        forEachStatement.Identifier,
                        forEachStatement.Expression);
            }

            private sealed class YieldReturnConverter : AbstractConverter, IConverter
            {
                private ForEachStatementSyntax _forEachStatement;
                private IEnumerable<SyntaxNode> _convertingNodes;
                private YieldStatementSyntax _yieldStatement;
                private SyntaxNode _nodeToDelete;

                public YieldReturnConverter(
                    ForEachStatementSyntax forEachStatement,
                 IEnumerable<SyntaxNode> convertingNodes,
                 YieldStatementSyntax yieldStatement,
                    SyntaxNode nodeToDelete = null)
                {
                    _forEachStatement = forEachStatement;
                    _convertingNodes = convertingNodes;
                    _yieldStatement = yieldStatement;
                    _nodeToDelete = nodeToDelete;
                }

                public void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
                {
                    var queryExpression = CreateQueryExpression(_forEachStatement, _convertingNodes, _yieldStatement.Expression);
                    editor.ReplaceNode(_forEachStatement, SyntaxFactory.ReturnStatement(queryExpression).WithAdditionalAnnotations(Formatter.Annotation));
                    if (_nodeToDelete != null)
                    {
                        editor.RemoveNode(_nodeToDelete);
                    }
                }
            }

            private sealed class DefaultConverter : AbstractConverter, IConverter
            {
                private ForEachStatementSyntax _forEachStatement;
                private IEnumerable<StatementSyntax> _statements;
                private IEnumerable<SyntaxToken> _identifiers;
                private IEnumerable<SyntaxNode> _convertingNodes;

                // TODO order of params
                public DefaultConverter(
                    ForEachStatementSyntax forEachStatement,
                    IEnumerable<SyntaxNode> convertingNodes, IEnumerable<StatementSyntax> statements, IEnumerable<SyntaxToken> identifiers)
                {
                    _forEachStatement = forEachStatement;
                    _convertingNodes = convertingNodes;
                    _statements = statements;
                    _identifiers = identifiers;
                }

                public void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
                {
                    var symbolNames = new HashSet<string>(_statements.SelectMany(statement => semanticModel.AnalyzeDataFlow(statement).ReadInside).Select(symbol => symbol.Name));
                    var identifiersUsedInBody = _identifiers.Where(identifier => symbolNames.Contains(identifier.ValueText));
                    editor.ReplaceNode(_forEachStatement, CreateDefaultReplacementStatement(_forEachStatement, _convertingNodes, identifiersUsedInBody, _statements));
                }

                private static StatementSyntax CreateDefaultReplacementStatement(
                    ForEachStatementSyntax forEachStatement,
                    IEnumerable<SyntaxNode> convertingNodes,
                    IEnumerable<SyntaxToken> identifiers,
                    IEnumerable<StatementSyntax> lastStatements)
                {
                    var block = WrapWithBlockIfNecessary(lastStatements.Select(statement => statement.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine(Environment.NewLine))));

                    var identifiersCount = identifiers.Count();
                    if (identifiersCount == 0)
                    {
                        // Generate foreach(var _ ... select (a,b))
                        return SyntaxFactory.ForEachStatement(VarNameIdentifier, SyntaxFactory.Identifier("_"), CreateQueryExpression(forEachStatement, convertingNodes, SyntaxFactory.AnonymousObjectCreationExpression()), block);
                    }
                    else if (identifiersCount == 1)
                    {
                        return SyntaxFactory.ForEachStatement(VarNameIdentifier, identifiers.Single(), CreateQueryExpression(forEachStatement, convertingNodes, SyntaxFactory.IdentifierName(identifiers.Single())), block);
                    }
                    else
                    {
                        var selectExpression = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier)))));
                        var declaration = SyntaxFactory.DeclarationExpression(VarNameIdentifier, SyntaxFactory.ParenthesizedVariableDesignation(
                            SyntaxFactory.SeparatedList<VariableDesignationSyntax>(identifiers.Select(identifier => SyntaxFactory.SingleVariableDesignation(identifier)))));

                        return SyntaxFactory.ForEachVariableStatement(declaration, CreateQueryExpression(forEachStatement, convertingNodes, selectExpression), block);
                    }
                }

                private static BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
                    => (statements.Count() == 1 && statements.Single() is BlockSyntax block) ? block : SyntaxFactory.Block(statements);
            }

            private abstract class AbstractConvertToMethodSyntaxEditor : AbstractConverter, IConverter
            {
                private ForEachStatementSyntax _forEachStatement;
                private IEnumerable<SyntaxNode> _convertingNodes;
                private ExpressionSyntax _selectExpression;
                // TODO name?
                private ExpressionSyntax _expressionAssigning;

                public AbstractConvertToMethodSyntaxEditor(
                    ForEachStatementSyntax forEachStatement,
                    IEnumerable<SyntaxNode> convertingNodes,
                    ExpressionSyntax selectExpression,
                    ExpressionSyntax expressionAssigning)
                {
                    _forEachStatement = forEachStatement;
                    _convertingNodes = convertingNodes;
                    _selectExpression = selectExpression;
                    _expressionAssigning = expressionAssigning;
                }

                protected abstract string MethodName { get; }

                protected abstract bool CanReplaceInitialization(ExpressionSyntax expressionSyntax, SemanticModel semanticModel, CancellationToken cancellationToken);

                protected abstract StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression);

                public void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
                {
                    var queryExpression = CreateQueryExpression(_forEachStatement, _convertingNodes, _selectExpression);
                    // TODO weird argument names
                    // TODO check function name
                    void LocalConvert(ExpressionSyntax replacingExpression, SyntaxNode declarationOrAssignmentNode)
                    {
                        // Check if expressionAssigning is followed by a return statement.
                        var expresisonSymbol = semanticModel.GetSymbolInfo(_expressionAssigning, cancellationToken).Symbol;
                        if (expresisonSymbol is ILocalSymbol &&
                            FindNextStatementInBlock(_forEachStatement) is ReturnStatementSyntax returnStatement &&
                            SymbolEquivalenceComparer.Instance.Equals(expresisonSymbol, semanticModel.GetSymbolInfo(returnStatement.Expression, cancellationToken).Symbol))
                        {
                            replacingExpression = returnStatement.Expression;
                            editor.RemoveNode(declarationOrAssignmentNode);
                        }

                        editor.ReplaceNode(replacingExpression, CreateInvocationExpression(queryExpression));
                        editor.RemoveNode(_forEachStatement);
                    }

                    var previous = FindPreviousStatementInBlock(_forEachStatement);

                    switch (previous?.Kind())
                    {
                        case SyntaxKind.LocalDeclarationStatement:
                            var variables = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables;
                            var lastDeclaration = variables.Last();
                            if (_expressionAssigning is IdentifierNameSyntax identifierName &&
                                lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) &&
                                CanReplaceInitialization(lastDeclaration.Initializer.Value, semanticModel, cancellationToken))
                            {
                                LocalConvert(lastDeclaration.Initializer.Value, variables.Count == 1 ? (SyntaxNode)previous : lastDeclaration);
                                return;
                            }

                            break;

                        case SyntaxKind.ExpressionStatement:
                            if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                                SymbolEquivalenceComparer.Instance.Equals(
                                    semanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).Symbol,
                                    semanticModel.GetSymbolInfo(_expressionAssigning, cancellationToken).Symbol) &&
                                CanReplaceInitialization(assignmentExpression.Right, semanticModel, cancellationToken))
                            {
                                LocalConvert(assignmentExpression.Right, previous);
                                return;
                            }

                            break;
                    }

                    editor.ReplaceNode(_forEachStatement, CreateDefaultStatement(queryExpression, _expressionAssigning).WithAdditionalAnnotations(Formatter.Annotation));
                }

                protected InvocationExpressionSyntax CreateInvocationExpression(QueryExpressionSyntax queryExpression)
                    => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParenthesizedExpression(queryExpression),
                                SyntaxFactory.IdentifierName(MethodName))).WithAdditionalAnnotations(Formatter.Annotation);

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
                public ConvertToListSyntaxEditor(
                    ForEachStatementSyntax forEachStatement,
                    IEnumerable<SyntaxNode> convertingNodes,
                    ExpressionSyntax selectExpression,
                    ExpressionSyntax expressionAssigning) : base(forEachStatement, convertingNodes, selectExpression, expressionAssigning) { }

                protected override string MethodName => nameof(Enumerable.ToList);

                protected override bool CanReplaceInitialization(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
                    => expression is ObjectCreationExpressionSyntax objectCreationExpression &&
                    semanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken).Symbol is ITypeSymbol typeSymbol &&
                    IsList(typeSymbol, semanticModel);

                protected override StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression)
                    => SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                expression,
                                SyntaxFactory.IdentifierName(nameof(List<object>.AddRange))),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(queryExpression)))));
            }

            private sealed class ConvertToCountSyntaxEditor : AbstractConvertToMethodSyntaxEditor
            {
                public ConvertToCountSyntaxEditor(
                    ForEachStatementSyntax forEachStatement,
                    IEnumerable<SyntaxNode> convertingNodes,
                    ExpressionSyntax selectExpression,
                    ExpressionSyntax expressionAssigning)
                    : base(forEachStatement, convertingNodes, selectExpression, expressionAssigning) { }

                protected override string MethodName => nameof(Enumerable.Count);

                protected override bool CanReplaceInitialization(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
                    => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

                protected override StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression)
                    => SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.AddAssignmentExpression,
                            expression,
                            CreateInvocationExpression(queryExpression)));
            }
        }
    }
}
