// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
