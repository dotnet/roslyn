// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static bool IsAnyAccessor(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                switch (method.MethodKind)
                {
                    case MethodKind.EventAdd:
                    case MethodKind.EventRaise:
                    case MethodKind.EventRemove:
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                        return true;
                }
            }

            return false;
        }

        private AccessorDeclarationSyntax? GenerateAccessorDeclaration(
            SyntaxKind kind,
            ISymbol parent,
            IMethodSymbol? accessorMethod)
        {
            if (accessorMethod == null)
                return null;

            var previousAccessorParent = _currentAccessorParent;
            _currentAccessorParent = parent;

            try
            {
                return AccessorDeclaration(
                    kind,
                    GenerateAttributeLists(accessorMethod.GetAttributes()),
                    GenerateModifiers(accessorMethod),
                    Block());
            }
            finally
            {
                _currentAccessorParent = previousAccessorParent;
            }
        }
    }
}
