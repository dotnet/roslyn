// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static class SyntaxNodeExtensions
    {
        public static bool IsDelegateOrConstructorOrLocalFunctionOrMethodOrOperatorParameterList(this SyntaxNode node, bool includeOperators)
        {
            if (!node.IsKind(SyntaxKind.ParameterList))
            {
                return false;
            }

            if (node.IsParentKind(SyntaxKind.MethodDeclaration) ||
                node.IsParentKind(SyntaxKind.LocalFunctionStatement) ||
                node.IsParentKind(SyntaxKind.ConstructorDeclaration) ||
                node.IsParentKind(SyntaxKind.DelegateDeclaration))
            {
                return true;
            }

            if (includeOperators)
            {
                return
                    node.IsParentKind(SyntaxKind.OperatorDeclaration) ||
                    node.IsParentKind(SyntaxKind.ConversionOperatorDeclaration);
            }

            return false;
        }
    }
}
