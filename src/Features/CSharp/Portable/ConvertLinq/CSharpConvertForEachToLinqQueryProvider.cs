// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected override ForEachStatementSyntax FindNodeToRefactor(SyntaxNode root, TextSpan span)
            => root.FindNode(span) as ForEachStatementSyntax; // TODO depending on performance, consider using FirstAncestorOrSelf

        private static readonly TypeSyntax VarNameIdentifier = SyntaxFactory.IdentifierName("var");

        protected override string Title => CSharpFeaturesResources.Convert_to_query;

        protected override bool TryConvert(
            ForEachStatementSyntax forEachStatement,
            Document document,
            SemanticModel semanticModel,
            ISemanticFactsService semanticFacts,
            CancellationToken cancellationToken,
            out SyntaxEditor editor)
                => new Converter(semanticModel, semanticFacts, forEachStatement, document, cancellationToken).TryConvert(out editor);

        private class Converter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ISemanticFactsService _semanticFacts;
            private readonly CancellationToken _cancellationToken;
            private readonly Document _document;
            private readonly ForEachStatementSyntax _forEachStatement;

            // TODO adjust parameters order in this method and the method in the parent class
            public Converter(SemanticModel semanticModel, ISemanticFactsService semanticFacts, ForEachStatementSyntax forEachStatement, Document document, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _semanticFacts = semanticFacts;
                _forEachStatement = forEachStatement;
                _document = document;
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

            private SyntaxEditor CreateDefaultEditor() => new SyntaxEditor(_semanticModel.SyntaxTree.GetRoot(_cancellationToken), _document.Project.Solution.Workspace);

            // TODO signature is not ideal: we always return queryClauses but setting lastStatements to something various
            private List<QueryClauseSyntax> CreateQueryClauses(out List<SyntaxToken> identifiers, out List<StatementSyntax> lastStatements)
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
                                    lastStatements = new List<StatementSyntax>(); lastStatements.AddRange(array.Skip(i));
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
                                lastStatements = new List<StatementSyntax> { current };
                                return queryClauses;
                            }

                        default:
                            lastStatements = new List<StatementSyntax> { current };
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
                            case SyntaxKind.PostIncrementExpression:
                                // No matter what can be used as the last select statement for the case of Count. We use SyntaxFactory.IdentifierName(_forEachStatement.Identifier).
                                var queryExpression1 = CreateQueryExpression(queryClauses, SyntaxFactory.IdentifierName(_forEachStatement.Identifier));
                                editor = ContertToCount(experessionStatement, queryExpression1, ((PostfixUnaryExpressionSyntax)experessionStatement.Expression).Operand);
                                return true;

                            case SyntaxKind.InvocationExpression:
                                var invocationExpression = (InvocationExpressionSyntax)experessionStatement.Expression;
                                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                    _semanticModel.GetSymbolInfo(memberAccessExpression, _cancellationToken).Symbol is IMethodSymbol methodSymbol &&
                                    IsList(methodSymbol.ContainingType) &&
                                    methodSymbol.Name.Equals(nameof(IList.Add)))
                                {
                                    var queryExpression2 = CreateQueryExpression(queryClauses, invocationExpression.ArgumentList.Arguments.Single().Expression);// TODO check for single
                                    editor = ConvertToToList(experessionStatement, queryExpression2, memberAccessExpression.Expression);
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

                        // TODO add a test
                        if (_forEachStatement.Parent == memberDeclaration)
                        {
                            editor = CreateReplaceYieldReturn(residuaryStatement, queryClauses);
                            return true;
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

            private SyntaxEditor ContertToCount(
                ExpressionStatementSyntax expressionStatement,
                QueryExpressionSyntax queryExpression,
                ExpressionSyntax expressionToIncrement)
            {
                // TODO consider cases:
                // int a = 0
                // int a = 5
                // var a = 0
                // var a = 5
                // var a = 4, b = 5
                // var b = 5, a = 0
                // a = 0
                // a = 5
                // double a = 0
                var previous = FindPreviousStatement(_forEachStatement);

                switch (previous?.Kind())
                {
                    // TODO also need to check the variable name
                    // a.b.Add(...)
                    case SyntaxKind.LocalDeclarationStatement:
                        var lastDeclaration = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables.Last();
                        if (expressionToIncrement is IdentifierNameSyntax identifierName &&
                            lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) && // TODO better comparison
                            (lastDeclaration.Initializer != null &&  // ignoring the case: int a; foreach(...); although it is not valid
                            (lastDeclaration.Initializer.Value is LiteralExpressionSyntax literalExpression1 && literalExpression1.Token.ValueText == "0")))// TODO better comparison
                        {
                            return ConvertToToCount(lastDeclaration.Initializer.Value, queryExpression);
                        }

                        break;

                    case SyntaxKind.ExpressionStatement:
                        if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                            SymbolEquivalenceComparer.Instance.Equals(_semanticModel.GetSymbolInfo(assignmentExpression.Left, _cancellationToken).Symbol, _semanticModel.GetSymbolInfo(expressionToIncrement, _cancellationToken).Symbol) &&
                            assignmentExpression.Right is LiteralExpressionSyntax literalExpression2 && literalExpression2.Token.ValueText == "0")
                        {
                            return ConvertToToCount(literalExpression2, queryExpression);
                        }

                        break;
                }

                var editor = CreateDefaultEditor();
                editor.ReplaceNode(
                    _forEachStatement,
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.AddAssignmentExpression,
                            expressionToIncrement,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.ParenthesizedExpression(queryExpression),
                                    SyntaxFactory.IdentifierName(nameof(IList.Count))))))
                            .WithAdditionalAnnotations(Formatter.Annotation));
                return editor;
            }

            private SyntaxEditor ConvertToToCount(
                 ExpressionSyntax expressionToReplace,
                 QueryExpressionSyntax queryExpression)
            {
                var editor = CreateDefaultEditor();
                editor.ReplaceNode(
                    expressionToReplace,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParenthesizedExpression(queryExpression),
                            SyntaxFactory.IdentifierName(nameof(Enumerable.Count)))).WithAdditionalAnnotations(Formatter.Annotation));
                editor.RemoveNode(_forEachStatement);
                return editor;
            }

            private SyntaxEditor ConvertToToList(ExpressionStatementSyntax expressionStatement, QueryExpressionSyntax queryExpression, ExpressionSyntax listExpression)
            {
                var previous = FindPreviousStatement(_forEachStatement);

                switch (previous?.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:
                        var lastDeclaration = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables.Last();
                        if (listExpression is IdentifierNameSyntax identifierName &&
                            lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) &&
                            lastDeclaration.Initializer.Value is ObjectCreationExpressionSyntax objectCreationExpression1 &&
                            _semanticModel.GetSymbolInfo(objectCreationExpression1.Type, _cancellationToken).Symbol is ITypeSymbol typeSymbol1 &&
                            IsList(typeSymbol1))
                        {
                            return ConvertToToList(objectCreationExpression1, queryExpression);
                        }

                        break;

                    case SyntaxKind.ExpressionStatement:
                        if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                            SymbolEquivalenceComparer.Instance.Equals(_semanticModel.GetSymbolInfo(assignmentExpression.Left, _cancellationToken).Symbol, _semanticModel.GetSymbolInfo(listExpression, _cancellationToken).Symbol) &&
                            assignmentExpression.Right is ObjectCreationExpressionSyntax objectCreationExpression2 &&
                            _semanticModel.GetSymbolInfo(objectCreationExpression2.Type, _cancellationToken).Symbol is ITypeSymbol typeSymbol2 &&
                            IsList(typeSymbol2))
                        {
                            return ConvertToToList(objectCreationExpression2, queryExpression);
                        }

                        break;
                }

                var editor = CreateDefaultEditor();
                editor.ReplaceNode(
                    _forEachStatement,
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                listExpression,
                                SyntaxFactory.IdentifierName(nameof(List<object>.AddRange))),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(queryExpression)))))
                            .WithAdditionalAnnotations(Formatter.Annotation)); // TODO <object>?
                return editor;
            }

            private SyntaxEditor ConvertToToList(ExpressionSyntax expressionToReplace, QueryExpressionSyntax queryExpression)
            {
                var editor = CreateDefaultEditor();
                editor.ReplaceNode(
                    expressionToReplace,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParenthesizedExpression(queryExpression),
                            SyntaxFactory.IdentifierName(nameof(Enumerable.ToList)))).WithAdditionalAnnotations(Formatter.Annotation));
                editor.RemoveNode(_forEachStatement);
                return editor;
            }

            private SyntaxEditor ConvertToDefault(List<QueryClauseSyntax> queryClauses, List<SyntaxToken> identifiers, IEnumerable<StatementSyntax> lastStatements)
            {
                var symbols = new HashSet<string>(lastStatements.SelectMany(statement => _semanticModel.AnalyzeDataFlow(statement).ReadInside).Select(symbol => symbol.Name));
                identifiers =  identifiers.Where(identifier => symbols.Contains(identifier.ValueText)).ToList();

                var editor = CreateDefaultEditor();
                // TODO can there be identifiers.Count == 0; ??
                if (identifiers.Count() == 1)
                {
                    // TODO should there be var identifier
                    // TODO do we need to add a block? what if there is one statement only?

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
                    identifiers.Reverse();
                    var selectExpression = SyntaxFactory.AnonymousObjectCreationExpression(
                            SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.IdentifierName(identifier)))));
                    var queryExpression = CreateQueryExpression(queryClauses, selectExpression);

                    var anonymousTypeName = "anonymous";
                    var freeName = GetFreeSymbolNameAndMarkUsed(anonymousTypeName);
                    var block = WrapWithBlockIfNecessary(lastStatements);

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

                return editor;
            }

            private QueryExpressionSyntax CreateQueryExpression(
                IEnumerable<QueryClauseSyntax> queryClauses,
                ExpressionSyntax selectExpression)
            {
                return SyntaxFactory.QueryExpression(
                    CreateFromClause(_forEachStatement),
                    SyntaxFactory.QueryBody(
                        SyntaxFactory.List(queryClauses),
                        // The current coverage of foreach statements to support does not need to use query continuations.
                        SyntaxFactory.SelectClause(selectExpression), continuation: null)) 
                        .WithAdditionalAnnotations(Formatter.Annotation);
            }

            private static FromClauseSyntax CreateFromClause(ForEachStatementSyntax forEachStatement)
                => SyntaxFactory.FromClause(
                    forEachStatement.Type is IdentifierNameSyntax identifierName && identifierName.Identifier.ValueText == "var" ? null : forEachStatement.Type, // TODO test for var vs non-var
                    forEachStatement.Identifier,
                    forEachStatement.Expression);

            private static StatementSyntax FindPreviousStatement(StatementSyntax statement)
            {
                if (statement.Parent is BlockSyntax block)
                {
                    StatementSyntax previous = null;
                    foreach (var current in block.Statements)
                    {
                        if (current == statement)
                        {
                            return previous;
                        }

                        previous = current;
                    }
                }

                return null;
            }

            // TODO copeid from CSharpConvertLinqQueryToForEachProvider. Share?
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

            private SyntaxToken GetFreeSymbolNameAndMarkUsed(string prefix)
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
        }
    }
}
