// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static class SyntaxNodeExtensions
    {
        public static bool IsDelegateOrConstructorOrLocalFunctionOrMethodOrOperatorParameterList([NotNullWhen(true)] this SyntaxNode? node, bool includeOperators)
        {
            if (!node.IsKind(SyntaxKind.ParameterList))
            {
                return false;
            }

            if (node?.Parent?.Kind()
                    is SyntaxKind.MethodDeclaration
                    or SyntaxKind.LocalFunctionStatement
                    or SyntaxKind.ConstructorDeclaration
                    or SyntaxKind.DelegateDeclaration)
            {
                return true;
            }

            if (includeOperators)
                return node?.Parent?.Kind() is SyntaxKind.OperatorDeclaration or SyntaxKind.ConversionOperatorDeclaration;

            return false;
        }
    }
}
