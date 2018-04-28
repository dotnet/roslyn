// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
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
            private readonly List<string> _introducedLocalNames;

            // TODO adjust parameters order in this method and the method in the parent class
            public Converter(SemanticModel semanticModel, ISemanticFactsService semanticFacts, ForEachStatementSyntax source, Document document, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _semanticFacts = semanticFacts;
                _forEachStatement = source;
                _document = document;
                _introducedLocalNames = new List<string>();
                _cancellationToken = cancellationToken;
            }

            public bool TryConvert(out SyntaxEditor editor)
            {
                var workspace = _document.Project.Solution.Workspace;
                editor = new SyntaxEditor(_semanticModel.SyntaxTree.GetRoot(_cancellationToken), workspace);
                // replace with BSF algorithm
                var queue = new Queue<StatementSyntax>();
                AddToQueue(queue, new[] { _forEachStatement.Statement }, out var residuaryStatements);
                var identifiers = new List<SyntaxToken>();
                identifiers.Add(_forEachStatement.Identifier);
                if (TryCreateQueryClauses(queue, out var queryClauses, identifiers, out var lastStatements)) // TODO should not by Try. Need to join with queue creation.
                {
                    // TODO consider case assignment + yield return
                    if (residuaryStatements.Length == 1 && TryConvertToSpecificCase(residuaryStatements.Single(), editor, queryClauses))
                    {
                        return true;
                    }
                }

                // No sense to convert a single foreach to foreach over the same collection
                if (queryClauses.Count >= 1)
                {
                    ConvertToDefault(queryClauses, identifiers, lastStatements, editor);
                    return true;
                }

                return false;
            }

            private bool TryConvertToSpecificCase(StatementSyntax residuaryStatement, SyntaxEditor editor, List<QueryClauseSyntax> queryClauses)
            {
                switch (residuaryStatement.Kind())
                {
                    case SyntaxKind.ExpressionStatement:
                        var experessionStatement = (ExpressionStatementSyntax)residuaryStatement;
                        switch (experessionStatement.Expression.Kind())
                        {
                            case SyntaxKind.PostIncrementExpression:
                                // TODO: why selecting SyntaxFactory.IdentifierName(_forEachStatement.Identifier)?
                                var queryExpression1 = CreateQueryExpression(queryClauses, SyntaxFactory.IdentifierName(_forEachStatement.Identifier)); // TODO foreach?
                                ContertToCount(experessionStatement, queryExpression1, ((PostfixUnaryExpressionSyntax)experessionStatement.Expression).Operand, editor);
                                return true;

                            case SyntaxKind.InvocationExpression:
                                var invocationExpression = (InvocationExpressionSyntax)experessionStatement.Expression;
                                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                                    memberAccessExpression.Name.Identifier.ValueText.Equals(nameof(IList.Add)))// TODO better check for Add, maybe it is not List.Add? use ISymbol
                                {
                                    var queryExpression2 = CreateQueryExpression(queryClauses, invocationExpression.ArgumentList.Arguments.Single().Expression);// TODO check for single
                                    ConvertToToList(experessionStatement, queryExpression2, memberAccessExpression.Expression, editor);
                                    return true;
                                }

                                break;
                        }

                        break;

                    case SyntaxKind.YieldReturnStatement:
                        // TODO also need to check that there were no other yield returns above or below.
                        // TODO maybe simple the check: check that there is a single yield return
                        // TODO allow yield break just after yield returns
                        var memberDeclaration = FindParentMemberDeclarationNode(_forEachStatement, out _);
                        var yieldStatements = memberDeclaration.DescendantNodes().OfType<YieldStatementSyntax>();

                        if (yieldStatements.Count() == 1 &&
                            (_forEachStatement.Parent == memberDeclaration || (
                            _forEachStatement.IsParentKind(SyntaxKind.Block) &&
                            _forEachStatement.Parent.Parent == memberDeclaration &&
                            ((BlockSyntax)_forEachStatement.Parent).Statements.Last() == _forEachStatement)))
                        {
                            var expression = ((YieldStatementSyntax)residuaryStatement).Expression;
                            var queryExpression = CreateQueryExpression(queryClauses, expression);
                            editor.ReplaceNode(_forEachStatement, SyntaxFactory.ReturnStatement(queryExpression).WithAdditionalAnnotations(Formatter.Annotation));
                            return true;
                        }

                        break;
                }

                return false;
            }

            private void ContertToCount(
                ExpressionStatementSyntax expressionStatement,
                QueryExpressionSyntax queryExpression,
                ExpressionSyntax expressionToIncrement,
                SyntaxEditor editor)
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
                            ConvertToToCount(lastDeclaration.Initializer.Value, queryExpression, editor);
                            return;
                        }

                        break;

                    case SyntaxKind.ExpressionStatement:
                        if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                            SymbolEquivalenceComparer.Instance.Equals(_semanticModel.GetSymbolInfo(assignmentExpression.Left, _cancellationToken).Symbol, _semanticModel.GetSymbolInfo(expressionToIncrement, _cancellationToken).Symbol) &&
                            assignmentExpression.Right is LiteralExpressionSyntax literalExpression2 && literalExpression2.Token.ValueText == "0")
                        {
                            ConvertToToCount(literalExpression2, queryExpression, editor);
                            return;
                        }

                        break;
                }

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
            }

            private void ConvertToToCount(
                 ExpressionSyntax expressionToReplace,
                 QueryExpressionSyntax queryExpression,
                SyntaxEditor editor)
            {
             
                editor.ReplaceNode(
                    expressionToReplace,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParenthesizedExpression(queryExpression),
                            SyntaxFactory.IdentifierName(nameof(Enumerable.Count)))).WithAdditionalAnnotations(Formatter.Annotation));
                editor.RemoveNode(_forEachStatement);
            }

            private void ConvertToToList(ExpressionStatementSyntax expressionStatement, QueryExpressionSyntax queryExpression, ExpressionSyntax listExpression, SyntaxEditor editor)
            {
                // TODO consider cases:
                // var list = new ...; foreach
                // list = new ... ; foreach
                // var a = ..., list = new list; foreach
                var previous = FindPreviousStatement(_forEachStatement);

                switch (previous?.Kind())
                {
                    // TODO also need to check the variable name
                    // a.b.Add(...)
                    case SyntaxKind.LocalDeclarationStatement:
                        var lastDeclaration = ((LocalDeclarationStatementSyntax)previous).Declaration.Variables.Last();
                        if (listExpression is IdentifierNameSyntax identifierName &&
                            lastDeclaration.Identifier.ValueText.Equals(identifierName.Identifier.ValueText) && // TODO better comparison
                            lastDeclaration.Initializer.Value is ObjectCreationExpressionSyntax objectCreationExpression1 &&
                            _semanticModel.GetSymbolInfo(objectCreationExpression1.Type, _cancellationToken).Symbol is ITypeSymbol typeSymbol1 &&
                            IsList(typeSymbol1))
                        {
                            ConvertToToList(objectCreationExpression1, queryExpression, editor);
                            return;
                        }

                        break;
                    // TODO also need to check the variable name
                    // a.b.Add(...)
                    case SyntaxKind.ExpressionStatement:
                        if (((ExpressionStatementSyntax)previous).Expression is AssignmentExpressionSyntax assignmentExpression &&
                            SymbolEquivalenceComparer.Instance.Equals(_semanticModel.GetSymbolInfo(assignmentExpression.Left, _cancellationToken).Symbol, _semanticModel.GetSymbolInfo(listExpression, _cancellationToken).Symbol) &&
                            assignmentExpression.Right is ObjectCreationExpressionSyntax objectCreationExpression2 &&
                            _semanticModel.GetSymbolInfo(objectCreationExpression2.Type, _cancellationToken).Symbol is ITypeSymbol typeSymbol2 &&
                            IsList(typeSymbol2))
                        {
                            ConvertToToList(objectCreationExpression2, queryExpression, editor);
                            return;
                        }

                        break;
                }

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
            }

            private void ConvertToToList(ExpressionSyntax expressionToReplace, QueryExpressionSyntax queryExpression, SyntaxEditor editor)
            {
                editor.ReplaceNode(
                    expressionToReplace,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParenthesizedExpression(queryExpression),
                            SyntaxFactory.IdentifierName(nameof(Enumerable.ToList)))).WithAdditionalAnnotations(Formatter.Annotation));
                editor.RemoveNode(_forEachStatement);
            }


            // TODO do we need to use Immutable for residuaryStatements?
            private void AddToQueue(Queue<StatementSyntax> queue, IEnumerable<StatementSyntax> statements, out ImmutableArray<StatementSyntax> residuaryStatements)
            {
                var array = statements.ToArray();

                // process the last statement later on
                for (int i = 0; i < array.Length - 1; i++)
                {
                    var statement = array[i];
                    // TODO anymore checks?
                    if (statement.Kind() == SyntaxKind.LocalDeclarationStatement)
                    {
                        queue.Enqueue(statement);
                    }
                    else
                    {
                        residuaryStatements = array.Skip(i).ToImmutableArray();
                        return;
                    }
                }

                // process last statement separately
                var last = array.Last();
                switch (last.Kind())
                {
                    case SyntaxKind.Block:
                        //  TODO tests with extra blocks
                        AddToQueue(queue, ((BlockSyntax)last).Statements, out residuaryStatements);
                        return;

                    case SyntaxKind.ForEachStatement:
                        queue.Enqueue(last);
                        AddToQueue(queue, new[] { ((ForEachStatementSyntax)last).Statement }, out residuaryStatements);
                        return;

                    case SyntaxKind.IfStatement:
                        var ifStatement = (IfStatementSyntax)last;
                        if (ifStatement.Else == null)
                        {
                            queue.Enqueue(last);
                            AddToQueue(queue, new[] { ifStatement.Statement }, out residuaryStatements);
                            return;
                        }
                        else
                        {
                            // default
                            residuaryStatements = new[] { last }.ToImmutableArray();
                            return;
                        }

                    default:
                        // TODO better constructions of the imm array
                        residuaryStatements = new[] { last }.ToImmutableArray();
                        return;
                }
            }

            private void ConvertToDefault(List<QueryClauseSyntax> queryClauses, List<SyntaxToken> identifiers, List<StatementSyntax> lastStatements, SyntaxEditor editor)
            {
                var queryExpression = CreateQueryExpression(queryClauses, identifiers, lastStatements);

                // TODO can there be identifiers.Count == 0; ??
                if (identifiers.Count() == 1)
                {
                    // TODO should there be var identifier
                    // TODO do we need to add a block? what if there is one statement only?
                    editor.ReplaceNode(_forEachStatement, SyntaxFactory.ForEachStatement(VarNameIdentifier, identifiers.Single(), queryExpression, WrapWithBlockIfNecessary(lastStatements)));
                }
                else
                {
                    var anonymousTypeName = "anonymous";
                    var freeName = GetFreeSymbolNameAndMarkUsed(anonymousTypeName);
                    var fromStatementIdentifierName = SyntaxFactory.IdentifierName(freeName);
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
                                                    fromStatementIdentifierName,
                                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                                    SyntaxFactory.IdentifierName(identifier)))))));
                        block = AddToBlockTop(localDeclaration, block);
                    }

                    editor.ReplaceNode(_forEachStatement, SyntaxFactory.ForEachStatement(VarNameIdentifier, freeName, queryExpression, block));
                }
            }

            private static BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
            {
                if (statements.Count() == 1 && statements.Single() is BlockSyntax block)
                {
                    return block;
                }

                return SyntaxFactory.Block(statements);
            }

            private List<SyntaxToken> FilterIdentifiers(List<SyntaxToken> identifiers, List<StatementSyntax> lastStatements)
            {

                var symbols = new HashSet<string>();
                foreach (var statement in lastStatements)
                {
                    foreach (var symbol in _semanticModel.AnalyzeDataFlow(statement).ReadInside)
                    {
                        symbols.Add(symbol.Name);
                    }
                }

                var result = new List<SyntaxToken>();
                foreach (var identifier in identifiers)
                {
                    if (symbols.Contains(identifier.ValueText))
                    {
                        // TODO add a unit tests with no identifiers used in the bottom loop
                        result.Add(identifier);
                    }
                }

                return result;
            }

            // TODO consider removing reccurrency, if using continuation
            private QueryExpressionSyntax CreateQueryExpression(
                List<QueryClauseSyntax> queryClauses,
                List<SyntaxToken> identifiers,
                List<StatementSyntax> lastStatements)
            {

                identifiers = FilterIdentifiers(identifiers, lastStatements);
                identifiers.Reverse();
                ExpressionSyntax selectExpression;
                if (identifiers.Count == 1)
                {
                    selectExpression = SyntaxFactory.IdentifierName(identifiers.Single());
                }
                else
                {
                    // TODO what is 0?
                    selectExpression = SyntaxFactory.AnonymousObjectCreationExpression(
                            SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.IdentifierName(identifier)))));
                }

                return CreateQueryExpression(queryClauses, selectExpression);
            }

            private QueryExpressionSyntax CreateQueryExpression(
                List<QueryClauseSyntax> queryClauses,
                ExpressionSyntax selectExpression)
            {
                // TODO should be generate continuation in some cases?
                return SyntaxFactory.QueryExpression(
                    CreateFromClause(_forEachStatement),
                    SyntaxFactory.QueryBody(
                        SyntaxFactory.List(queryClauses),
                        SyntaxFactory.SelectClause(selectExpression), continuation: null))
                        .WithAdditionalAnnotations(Formatter.Annotation);
            }

            // TODO replace List<statementSyntax> with???
            // Should not we create last statements earlier when creating the queue?
            private bool TryCreateQueryClauses(Queue<StatementSyntax> queue, out List<QueryClauseSyntax> queryClauses, List<SyntaxToken> identifiers, out List<StatementSyntax> lastStatements)
            {
                queryClauses = new List<QueryClauseSyntax>();
                lastStatements = _forEachStatement.Statement is BlockSyntax block ? block.Statements.ToList() : new List<StatementSyntax> { _forEachStatement.Statement }; // TODO test case

                while (queue.Any())
                {
                    var node = queue.Dequeue();
                    switch (node.Kind())
                    {
                        case SyntaxKind.ForEachStatement:
                            var forEachStatement = node as ForEachStatementSyntax;
                            var fromClause = CreateFromClause(forEachStatement);
                            identifiers.Add(forEachStatement.Identifier);
                            queryClauses.Add(fromClause);
                            lastStatements = new List<StatementSyntax> { forEachStatement.Statement };
                            break;

                        case SyntaxKind.IfStatement:
                            // TODO check either here or somewhere else that there is no else
                            // TODO may consdier if () break; else {}
                            var ifStatement = node as IfStatementSyntax;
                            // TODO may consider join for some cases
                            var whereClause = SyntaxFactory.WhereClause(ifStatement.Condition);
                            queryClauses.Add(whereClause);
                            lastStatements = new List<StatementSyntax> { ifStatement.Statement };
                            break;

                        case SyntaxKind.LocalDeclarationStatement:
                            var localDeclaration = node as LocalDeclarationStatementSyntax;
                            foreach (var variable in localDeclaration.Declaration.Variables)
                            {
                                queryClauses.Add(SyntaxFactory.LetClause(variable.Identifier, variable.Initializer.Value));
                                identifiers.Add(variable.Identifier);
                            }

                            // TODO need to check if it belongs
                            lastStatements.Remove(localDeclaration);

                            // TODO The lastbody should be updated by removing the localdeclaration statement from the current lastbody
                            // TODO what if there were no last body set above???
                            break;

                        default:
                            // TODO should crash here?
                            identifiers = default;
                            return false;
                    }
                }

                return true;
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

            private SyntaxToken GetFreeSymbolNameAndMarkUsed(string prefix)
            {
                var freeToken = _semanticFacts.GenerateUniqueName(_semanticModel, _forEachStatement, containerOpt: null, baseName: prefix, _introducedLocalNames, _cancellationToken);
                _introducedLocalNames.Add(freeToken.ValueText);
                return freeToken;
            }

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
