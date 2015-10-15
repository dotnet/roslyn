// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static class SyntaxNodeExtensions
    {
        public static bool IsDelegateOrConstructorOrMethodParameterList(this SyntaxNode node)
        {
            if (!node.IsKind(SyntaxKind.ParameterList))
            {
                return false;
            }

            return
                node.IsParentKind(SyntaxKind.MethodDeclaration) ||
                node.IsParentKind(SyntaxKind.ConstructorDeclaration) ||
                node.IsParentKind(SyntaxKind.DelegateDeclaration);
        }
    }
}
