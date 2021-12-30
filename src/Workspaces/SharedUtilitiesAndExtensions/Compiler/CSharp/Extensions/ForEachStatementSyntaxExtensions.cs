// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ForEachStatementSyntaxExtensions
    {
        public static bool IsTypeInferred(this CommonForEachStatementSyntax forEachStatement, SemanticModel semanticModel)
        {
            switch (forEachStatement.Kind())
            {
                case SyntaxKind.ForEachStatement:
                    return ((ForEachStatementSyntax)forEachStatement).Type.IsTypeInferred(semanticModel);
                case SyntaxKind.ForEachVariableStatement:
                    return (((ForEachVariableStatementSyntax)forEachStatement).Variable as DeclarationExpressionSyntax)?.Type
                        .IsTypeInferred(semanticModel) == true;
                default:
                    return false;
            }
        }
    }
}
