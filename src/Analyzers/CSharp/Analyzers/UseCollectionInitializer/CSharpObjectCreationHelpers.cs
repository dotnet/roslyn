// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal static class CSharpObjectCreationHelpers
{
    public static bool IsInitializerOfLocalDeclarationStatement(
        LocalDeclarationStatementSyntax localDeclarationStatement,
        BaseObjectCreationExpressionSyntax rootExpression,
        [NotNullWhen(true)] out VariableDeclaratorSyntax? variableDeclarator)
    {
        foreach (var declarator in localDeclarationStatement.Declaration.Variables)
        {
            if (declarator.Initializer?.Value == rootExpression)
            {
                variableDeclarator = declarator;
                return true;
            }
        }

        variableDeclarator = null;
        return false;
    }
}
