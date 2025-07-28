// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class VariableDeclaratorExtensions
{
    extension(VariableDeclaratorSyntax declarator)
    {
        public TypeSyntax GetVariableType()
        {
            if (declarator.Parent is VariableDeclarationSyntax variableDeclaration)
            {
                return variableDeclaration.Type;
            }

            return null;
        }
    }

    extension(VariableDeclaratorSyntax variable)
    {
        public bool IsTypeInferred(SemanticModel semanticModel)
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
