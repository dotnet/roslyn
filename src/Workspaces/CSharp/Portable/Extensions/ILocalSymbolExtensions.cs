// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
                    .FirstOrDefault(node => node.IsKind(SyntaxKind.LocalFunctionStatement) || node.IsKind(SyntaxKind.MethodDeclaration));
                var localFunctionStatement = destinationBlock.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();

                if (localFunctionOrMethodDeclaration != localFunctionStatement &&
                    HasTypeParameterWithName(localFunctionOrMethodDeclaration, localSymbol.Type.Name) &&
                    HasTypeParameterWithName(localFunctionStatement, localSymbol.Type.Name))
                {
                    return false;
                }
            }

            return true;

            bool HasTypeParameterWithName(SyntaxNode node, string name)
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
