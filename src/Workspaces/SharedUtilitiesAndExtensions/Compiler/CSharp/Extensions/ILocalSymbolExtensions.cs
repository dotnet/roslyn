// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ILocalSymbolExtensions
    {
        public static bool CanSafelyMoveLocalToBlock(this ILocalSymbol localSymbol, SyntaxNode currentBlock, SyntaxNode destinationBlock)
        {
            if (currentBlock != destinationBlock)
            {
                var localFunctionOrMethodDeclaration = currentBlock.AncestorsAndSelf()
                    .FirstOrDefault(node => node.Kind() is SyntaxKind.LocalFunctionStatement or SyntaxKind.MethodDeclaration);
                var localFunctionStatement = destinationBlock.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();

                if (localFunctionOrMethodDeclaration != localFunctionStatement &&
                    HasTypeParameterWithName(localFunctionOrMethodDeclaration, localSymbol.Type.Name) &&
                    HasTypeParameterWithName(localFunctionStatement, localSymbol.Type.Name))
                {
                    return false;
                }
            }

            return true;

            static bool HasTypeParameterWithName(SyntaxNode? node, string name)
            {
                SeparatedSyntaxList<TypeParameterSyntax>? typeParameters;
                switch (node)
                {
                    case MethodDeclarationSyntax methodDeclaration:
                        typeParameters = methodDeclaration.TypeParameterList?.Parameters;
                        break;
                    case LocalFunctionStatementSyntax localFunctionStatement:
                        typeParameters = localFunctionStatement.TypeParameterList?.Parameters;
                        break;
                    default:
                        return false;
                }

                return typeParameters.HasValue && typeParameters.Value.Any(typeParameter => typeParameter.Identifier.ValueText == name);
            }
        }
    }
}
