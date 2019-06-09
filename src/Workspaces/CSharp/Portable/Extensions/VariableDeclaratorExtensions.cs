// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class VariableDeclaratorExtensions
    {
        public static TypeSyntax GetVariableType(this VariableDeclaratorSyntax declarator)
        {
            if (declarator.Parent is VariableDeclarationSyntax variableDeclaration)
            {
                return variableDeclaration.Type;
            }

            return null;
        }

        public static bool IsTypeInferred(this VariableDeclaratorSyntax variable, SemanticModel semanticModel)
        {
            var variableTypeName = variable.GetVariableType();
            if (variableTypeName == null)
            {
                return false;
            }

            return variableTypeName.IsTypeInferred(semanticModel);
        }
    }
}
