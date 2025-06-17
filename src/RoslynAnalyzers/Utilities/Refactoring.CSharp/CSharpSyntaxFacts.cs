//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//#nullable disable warnings

//using System.Collections.Immutable;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using Analyzer.Utilities.Extensions;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;

//namespace Analyzer.Utilities
//{
//    internal sealed class CSharpSyntaxFacts : AbstractSyntaxFacts, ISyntaxFacts
//    {
//        public static CSharpSyntaxFacts Instance { get; } = new CSharpSyntaxFacts();

//        private CSharpSyntaxFacts()
//        {
//        }

//        public override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

//        public SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node)
//            => ((ExpressionStatementSyntax)node).Expression;

//        public bool IsSimpleAssignmentStatement(SyntaxNode statement)
//        {
//            return statement is ExpressionStatementSyntax exprStatement
//                && exprStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression);
//        }

//        public void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right)
//        {
//            var expression = statement;
//            if (statement is ExpressionStatementSyntax expressionStatement)
//            {
//                expression = expressionStatement.Expression;
//            }

//            var assignment = (AssignmentExpressionSyntax)expression;
//            left = assignment.Left;
//            operatorToken = assignment.OperatorToken;
//            right = assignment.Right;
//        }

//        public override SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node)
//            => node.GetAttributeLists();

//        public SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node)
//            => ((LocalDeclarationStatementSyntax)node).Declaration.Variables;

//        public SyntaxNode GetInitializerOfVariableDeclarator(SyntaxNode node)
//            => ((VariableDeclaratorSyntax)node).Initializer;

//        public SyntaxNode? GetValueOfEqualsValueClause(SyntaxNode? node)
//            => ((EqualsValueClauseSyntax?)node)?.Value;

//        public bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
//        {
//            var node = TryGetAncestorForLocation<BaseTypeDeclarationSyntax>(root, position);
//            if (node is null)
//            {
//                typeDeclaration = null;
//                return false;
//            }

//            typeDeclaration = node;
//            var lastToken = (node as TypeDeclarationSyntax)?.TypeParameterList?.GetLastToken() ?? node.Identifier;
//            if (fullHeader)
//                lastToken = node.BaseList?.GetLastToken() ?? lastToken;

//            return IsOnHeader(root, position, node, lastToken);
//        }

//        public bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration)
//        {
//            var node = TryGetAncestorForLocation<PropertyDeclarationSyntax>(root, position);
//            if (node is null)
//            {
//                propertyDeclaration = null;
//                return false;
//            }

//            propertyDeclaration = node;
//            return IsOnHeader(root, position, node, node.Identifier);
//        }

//        public bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter)
//        {
//            var node = TryGetAncestorForLocation<ParameterSyntax>(root, position);
//            if (node is null)
//            {
//                parameter = null;
//                return false;
//            }

//            parameter = node;
//            return IsOnHeader(root, position, node, node);
//        }

//        public bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method)
//        {
//            var node = TryGetAncestorForLocation<MethodDeclarationSyntax>(root, position);
//            if (node is null)
//            {
//                method = null;
//                return false;
//            }

//            method = node;
//            return IsOnHeader(root, position, node, node.ParameterList);
//        }

//        public bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction)
//        {
//            var node = TryGetAncestorForLocation<LocalFunctionStatementSyntax>(root, position);
//            if (node is null)
//            {
//                localFunction = null;
//                return false;
//            }

//            localFunction = node;
//            return IsOnHeader(root, position, node, node.ParameterList);
//        }

//        public bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
//        {
//            var node = TryGetAncestorForLocation<LocalDeclarationStatementSyntax>(root, position);
//            if (node is null)
//            {
//                localDeclaration = null;
//                return false;
//            }

//            localDeclaration = node;
//            var initializersExpressions = node.Declaration.Variables
//                .Where(v => v.Initializer != null)
//                .Select(initializedV => initializedV.Initializer.Value)
//                .ToImmutableArray();
//            return IsOnHeader(root, position, node, node, holes: initializersExpressions);
//        }

//        public bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement)
//        {
//            var node = TryGetAncestorForLocation<IfStatementSyntax>(root, position);
//            if (node is null)
//            {
//                ifStatement = null;
//                return false;
//            }

//            ifStatement = node;
//            return IsOnHeader(root, position, node, node.CloseParenToken);
//        }

//        public bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement)
//        {
//            var node = TryGetAncestorForLocation<ForEachStatementSyntax>(root, position);
//            if (node is null)
//            {
//                foreachStatement = null;
//                return false;
//            }

//            foreachStatement = node;
//            return IsOnHeader(root, position, node, node.CloseParenToken);
//        }
//    }
//}
