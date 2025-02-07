// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal interface ISyntaxFacts
    {
        ISyntaxKinds SyntaxKinds { get; }

        SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);

        bool IsSimpleAssignmentStatement(SyntaxNode statement);
        void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);

        SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node);
        SyntaxNode GetInitializerOfVariableDeclarator(SyntaxNode node);
        SyntaxNode? GetValueOfEqualsValueClause(SyntaxNode? node);

        /// <summary>
        /// <paramref name="fullHeader"/> controls how much of the type header should be considered. If <see
        /// langword="false"/> only the span up through the type name will be considered.  If <see langword="true"/>
        /// then the span through the base-list will be considered.
        /// </summary>
        bool IsOnTypeHeader(SyntaxNode root, int position, bool fullHeader, [NotNullWhen(true)] out SyntaxNode? typeDeclaration);

        bool IsOnPropertyDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? propertyDeclaration);
        bool IsOnParameterHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? parameter);
        bool IsOnMethodHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? method);
        bool IsOnLocalFunctionHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localFunction);
        bool IsOnLocalDeclarationHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? localDeclaration);
        bool IsOnIfStatementHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? ifStatement);
        bool IsOnForeachHeader(SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? foreachStatement);
    }
}
