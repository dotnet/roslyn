// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    internal sealed class CSharpConvertForEachToLinqQueryProvider : AbstractConvertForEachToLinqQueryProvider<ForEachStatementSyntax, StatementSyntax>
    {
        // TODO what if comments are not in foreach but in related statements we're removing or modifying, e.g. return or list = new List<int>()
        protected override ForEachStatementSyntax FindNodeToRefactor(SyntaxNode root, TextSpan span)
            => root.FindNode(span) as ForEachStatementSyntax;

        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

        protected override string Title => CSharpFeaturesResources.Convert_to_query;

        protected override IConverter CreateDefaultConverter(
            ForEachStatementSyntax forEachStatement,
            ImmutableArray<SyntaxNode> convertingNodes,
            ImmutableArray<SyntaxToken> identifiers,
            ImmutableArray<StatementSyntax> statements)
            => new DefaultConverter(forEachStatement, convertingNodes, identifiers, statements);

        // Do not try to refactor queries with comments or conditional compilation in them.
        // We can consider supporting queries with comments in the future.
        protected override bool Validate(StatementSyntax statement)
         => !(statement.ContainsDirectives ||
            statement.DescendantTrivia().Any(trivia => trivia.MatchesKind(
             SyntaxKind.SingleLineCommentTrivia,
             SyntaxKind.MultiLineCommentTrivia,
             SyntaxKind.MultiLineDocumentationCommentTrivia)));

        protected override (ImmutableArray<SyntaxNode> ConvertingNodes, ImmutableArray<SyntaxToken> Identifiers, ImmutableArray<StatementSyntax> Statements) CreateForEachInfo(ForEachStatementSyntax forEachStatement)
        {
            var identifiers = new List<SyntaxToken>();
            identifiers.Add(forEachStatement.Identifier);
            var convertingNodes = new List<SyntaxNode>();
            var current = forEachStatement.Statement;
            IEnumerable<StatementSyntax> statementsCannotBeConverted = null;

            void ProcessLocalDeclarationStatement(LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                foreach (var variable in localDeclarationStatement.Declaration.Variables)
                {
                    convertingNodes.Add(variable);
                    identifiers.Add(variable.Identifier);
                }
            }

            // Setting statementsCannotBeConverted to anything means that we stop processing.
            while (statementsCannotBeConverted == null)
            {
                switch (current.Kind())
                {
                    case SyntaxKind.Block:
                        var block = (BlockSyntax)current;
                        var array = block.Statements.ToArray();
                        if (array.Any())
                        {
                            // Process all statements except the last one.
                            for (int i = 0; i < array.Length - 1; i++)
                            {
                                var statement = array[i];
                                if (statement is LocalDeclarationStatementSyntax localDeclarationStatement)
                                {
                                    ProcessLocalDeclarationStatement(localDeclarationStatement);
                                }
                                else
                                {
                                    statementsCannotBeConverted = array.Skip(i);
                                    break;
                                }
                            }

                            // Process the last statement separately.
                            current = array.Last();
                        }
                        else
                        {
                            statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        }
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
                            statementsCannotBeConverted = new[] { current };
                            break;
                        }

                    case SyntaxKind.LocalDeclarationStatement:
                        // This is a situation with "var a = something;" s the most internal statements inside the loop.
                        ProcessLocalDeclarationStatement((LocalDeclarationStatementSyntax)current);
                        statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        break;

                    case SyntaxKind.EmptyStatement:
                        statementsCannotBeConverted = Enumerable.Empty<StatementSyntax>();
                        break;

                    default:
                        statementsCannotBeConverted = new[] { current };
                        break;
                }
            }

            return (convertingNodes.ToImmutableArray(), identifiers.ToImmutableArray(), statementsCannotBeConverted.ToImmutableArray());
        }

        protected override bool TryBuildSpecificConverter(
            ForEachStatementSyntax forEachStatement,
            SemanticModel semanticModel,
            ImmutableArray<SyntaxNode> convertingNodes,
            StatementSyntax statementCannotBeConverted,
            CancellationToken cancellationToken,
            out IConverter converter)
        {
            switch (statementCannotBeConverted.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                    var expression = ((ExpressionStatementSyntax)statementCannotBeConverted).Expression;
                    switch (expression.Kind())
                    {
                        case SyntaxKind.PostIncrementExpression:
                            // No matter what can be used as the last select statement for the case of Count. We use SyntaxFactory.IdentifierName(forEachStatement.Identifier).
                            converter = new ConvertToCountSyntaxEditor(
                                forEachStatement,
                                convertingNodes,
                                SyntaxFactory.IdentifierName(forEachStatement.Identifier),
                                ((PostfixUnaryExpressionSyntax)expression).Operand);
                            return true;

                        case SyntaxKind.InvocationExpression:
                            var invocationExpression = (InvocationExpressionSyntax)expression;
                            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                IsList(methodSymbol.ContainingType, semanticModel) &&
                                methodSymbol.Name.Equals(nameof(IList.Add)) &&
                                methodSymbol.Parameters.Length == 1)
                            {
                                converter = new ConvertToListSyntaxEditor(
                                    forEachStatement,
                                    convertingNodes,
                                    invocationExpression.ArgumentList.Arguments.Single().Expression,
                                    memberAccessExpression.Expression);
                                return true;
                            }

                            break;
                    }

                    break;

                case SyntaxKind.YieldReturnStatement:
                    var memberDeclaration = semanticModel.GetEnclosingSymbol(forEachStatement.SpanStart, cancellationToken).DeclaringSyntaxReferences.Single().GetSyntax();
                    var yieldStatements = memberDeclaration.DescendantNodes().OfType<YieldStatementSyntax>();

                    if (forEachStatement.IsParentKind(SyntaxKind.Block) && forEachStatement.Parent.Parent == memberDeclaration)
                    {
                        var statementsOnBlockWithForEach = ((BlockSyntax)forEachStatement.Parent).Statements;
                        var lastStatement = statementsOnBlockWithForEach.Last();
                        if (yieldStatements.Count() == 1 && lastStatement == forEachStatement)
                        {
                            converter = new YieldReturnConverter(forEachStatement, convertingNodes, (YieldStatementSyntax)statementCannotBeConverted);
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
                            // This removes the yield break.
                            converter = new YieldReturnConverter(forEachStatement, convertingNodes, (YieldStatementSyntax)statementCannotBeConverted, nodeToDelete: lastStatement);
                            return true;
                        }
                    }

                    break;
            }

            converter = default;
            return false;
        }

        protected override async Task AddLinqUsing(Document document, SyntaxEditor syntaxEditor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            const string linqNamespaceName = "System.Linq";
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is CompilationUnitSyntax compilationUnit)
            {
                var existingUsings = compilationUnit.Usings;
                // TODO anything better?
                if (!existingUsings.Any(existingUsing => existingUsing.Name.ToString().Equals(linqNamespaceName)))
                {
                    var linqName = SyntaxFactory.ParseName(linqNamespaceName);
                    // TODO what if no usings at all?
                    syntaxEditor.InsertAfter(compilationUnit.Usings.Last(), SyntaxFactory.UsingDirective(linqName));
                }
            }
        }

        private static bool IsList(ITypeSymbol typeSymbol, SemanticModel semanticModel)
            => Equals(typeSymbol.OriginalDefinition, semanticModel.Compilation.GetTypeByMetadataName(typeof(List<>).FullName));

        private abstract class AbstractConverter
        {
            protected static QueryExpressionSyntax CreateQueryExpression(ForEachStatementSyntax forEachStatement, ImmutableArray<SyntaxNode> convertingNodes, ExpressionSyntax selectExpression)
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
            private ImmutableArray<SyntaxNode> _convertingNodes;
            private YieldStatementSyntax _yieldStatement;
            private SyntaxNode _nodeToDelete;

            public YieldReturnConverter(
                ForEachStatementSyntax forEachStatement,
                ImmutableArray<SyntaxNode> convertingNodes,
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
                // Delete the yield break just after the loop.
                if (_nodeToDelete != null)
                {
                    editor.RemoveNode(_nodeToDelete);
                }
            }
        }

        private sealed class DefaultConverter : AbstractConverter, IConverter
        {
            private ForEachStatementSyntax _forEachStatement;
            private ImmutableArray<SyntaxNode> _convertingNodes;
            private ImmutableArray<SyntaxToken> _identifiers;
            private ImmutableArray<StatementSyntax> _statements;

            public DefaultConverter(
                ForEachStatementSyntax forEachStatement,
                ImmutableArray<SyntaxNode> convertingNodes,
                ImmutableArray<SyntaxToken> identifiers,
                ImmutableArray<StatementSyntax> statements)
            {
                _forEachStatement = forEachStatement;
                _convertingNodes = convertingNodes;
                _identifiers = identifiers;
                _statements = statements;
            }

            public void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                // Filter out identifiers which are not used in statements.
                var symbolNames = new HashSet<string>(_statements.SelectMany(statement => semanticModel.AnalyzeDataFlow(statement).ReadInside).Select(symbol => symbol.Name));
                var identifiersUsedInStatements = _identifiers.Where(identifier => symbolNames.Contains(identifier.ValueText));

                // Wrap statements with a block.
                var block = WrapWithBlockIfNecessary(_statements.Select(statement => statement.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine(Environment.NewLine))));

                editor.ReplaceNode(_forEachStatement, CreateDefaultReplacementStatement(_forEachStatement, _convertingNodes, identifiersUsedInStatements, block).WithAdditionalAnnotations(Formatter.Annotation));
            }

            private static StatementSyntax CreateDefaultReplacementStatement(
                ForEachStatementSyntax forEachStatement,
                ImmutableArray<SyntaxNode> convertingNodes,
                IEnumerable<SyntaxToken> identifiers,
                BlockSyntax block)
            {
                var identifiersCount = identifiers.Count();
                if (identifiersCount == 0)
                {
                    // Generate foreach(var _ ... select new {})
                    return SyntaxFactory.ForEachStatement(VarNameIdentifier, SyntaxFactory.Identifier("_"), CreateQueryExpression(forEachStatement, convertingNodes, SyntaxFactory.AnonymousObjectCreationExpression()), block);
                }
                else if (identifiersCount == 1)
                {
                    // Generate foreach(var singleIdentifier from ... select singleIdentifier)
                    return SyntaxFactory.ForEachStatement(VarNameIdentifier, identifiers.Single(), CreateQueryExpression(forEachStatement, convertingNodes, SyntaxFactory.IdentifierName(identifiers.Single())), block);
                }
                else
                {
                    var tupleForSelectExpression = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(identifier)))));
                    var declaration = SyntaxFactory.DeclarationExpression(
                        VarNameIdentifier,
                        SyntaxFactory.ParenthesizedVariableDesignation(
                            SyntaxFactory.SeparatedList<VariableDesignationSyntax>(identifiers.Select(identifier => SyntaxFactory.SingleVariableDesignation(identifier)))));

                    // Generate foreach(var (a,b) ... select (a, b))
                    return SyntaxFactory.ForEachVariableStatement(declaration, CreateQueryExpression(forEachStatement, convertingNodes, tupleForSelectExpression), block);
                }
            }

            private static BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
                => (statements.Count() == 1 && statements.Single() is BlockSyntax block) ? block : SyntaxFactory.Block(statements);
        }

        private abstract class AbstractConvertToMethodSyntaxEditor : AbstractConverter, IConverter
        {
            private ForEachStatementSyntax _forEachStatement;
            private ImmutableArray<SyntaxNode> _convertingNodes;

            // It is "item" for for "list.Add(item)"
            // It can be anything for "counter++". It will be ingored in the case.
            private ExpressionSyntax _selectExpression;

            // It is "list" for "list.Add(item)"
            // It is "counter" for "counter++"
            private ExpressionSyntax _modifyingExpression;

            public AbstractConvertToMethodSyntaxEditor(
                ForEachStatementSyntax forEachStatement,
                ImmutableArray<SyntaxNode> convertingNodes,
                ExpressionSyntax selectExpression,
                ExpressionSyntax modifyingExpression)
            {
                _forEachStatement = forEachStatement;
                _convertingNodes = convertingNodes;
                _selectExpression = selectExpression;
                _modifyingExpression = modifyingExpression;
            }

            protected abstract string MethodName { get; }

            protected abstract bool CanReplaceInitialization(ExpressionSyntax expressionSyntax, SemanticModel semanticModel, CancellationToken cancellationToken);

            protected abstract StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression);

            public void Convert(SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var queryExpression = CreateQueryExpression(_forEachStatement, _convertingNodes, _selectExpression);

                void Convert(ExpressionSyntax replacingExpression, SyntaxNode nodeToRemoveIfFollowedByReturn)
                {
                    // Check if expressionAssigning is followed by a return statement.
                    var expresisonSymbol = semanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol;
                    if (expresisonSymbol is ILocalSymbol &&
                        FindNextStatementInBlock(_forEachStatement) is ReturnStatementSyntax returnStatement &&
                        SymbolEquivalenceComparer.Instance.Equals(expresisonSymbol, semanticModel.GetSymbolInfo(returnStatement.Expression, cancellationToken).Symbol))
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
                        editor.RemoveNode(nodeToRemoveIfFollowedByReturn);
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
                        // Check if 
                        // var ...., list = new List<T>(); or var ..., counter = 0;
                        // is just before the foreach.
                        // If so, join the declaration with the query.
                        if (_modifyingExpression is IdentifierNameSyntax identifierName &&
                            lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) &&
                            CanReplaceInitialization(lastDeclaration.Initializer.Value, semanticModel, cancellationToken))
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
                                semanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).Symbol,
                                semanticModel.GetSymbolInfo(_modifyingExpression, cancellationToken).Symbol) &&
                            CanReplaceInitialization(assignmentExpression.Right, semanticModel, cancellationToken))
                        {
                            Convert(assignmentExpression.Right, previous);
                            return;
                        }

                        break;
                }

                // At least, we already can convert to 
                // list.AddRange(query) or counter += query.Count();
                editor.ReplaceNode(_forEachStatement, CreateDefaultStatement(queryExpression, _modifyingExpression).WithAdditionalAnnotations(Formatter.Annotation));
            }

            // query => query.Method()
            // like query.Count() or query.ToList()
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
                ImmutableArray<SyntaxNode> convertingNodes,
                ExpressionSyntax selectExpression,
                ExpressionSyntax modifyingExpression) : base(forEachStatement, convertingNodes, selectExpression, modifyingExpression) { }

            protected override string MethodName => nameof(Enumerable.ToList);

            /// Checks that the expression is "new List();"
            /// Exclude "new List(a);" and new List() { 1, 2, 3}
            protected override bool CanReplaceInitialization(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
                => expression is ObjectCreationExpressionSyntax objectCreationExpression &&
                semanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken).Symbol is ITypeSymbol typeSymbol &&
                IsList(typeSymbol, semanticModel) &&
                !objectCreationExpression.ArgumentList.Arguments.Any() &&
                objectCreationExpression.Initializer == null;

            /// Input:
            /// foreach(...)
            /// {
            ///     ...
            ///     ...
            ///     list.Add(item);
            ///  }
            ///  
            ///  Output:
            ///  list.AddRange(queryGenerated);
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
                ImmutableArray<SyntaxNode> convertingNodes,
                ExpressionSyntax selectExpression,
                ExpressionSyntax modifyingExpression)
                : base(forEachStatement, convertingNodes, selectExpression, modifyingExpression) { }

            protected override string MethodName => nameof(Enumerable.Count);

            // Checks that the expression is "0".
            protected override bool CanReplaceInitialization(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
                => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

            /// Input:
            /// foreach(...)
            /// {
            ///     ...
            ///     ...
            ///     counter++;
            ///  }
            ///  
            ///  Output:
            ///  counter += queryGenerated.Count();
            protected override StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression)
                => SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.AddAssignmentExpression,
                        expression,
                        CreateInvocationExpression(queryExpression)));
        }
    }
}
