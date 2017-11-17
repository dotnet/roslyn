// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static class SyntaxNodeExtensions
    {
        public static bool IsDelegateOrConstructorOrLocalFunctionOrMethodParameterList(this SyntaxNode node)
        {
            if (!node.IsKind(SyntaxKind.ParameterList))
            {
                return false;
            }

            return
                node.IsParentKind(SyntaxKind.MethodDeclaration) ||
                node.IsParentKind(SyntaxKind.LocalFunctionStatement) ||
                node.IsParentKind(SyntaxKind.ConstructorDeclaration) ||
                node.IsParentKind(SyntaxKind.DelegateDeclaration);
        }
    }
}
