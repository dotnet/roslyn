' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module StatementGenerator

        Friend Function GenerateStatements(statements As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            Return SyntaxFactory.List(statements.OfType(Of StatementSyntax)())
        End Function

        Friend Function GenerateStatements(method As IMethodSymbol) As SyntaxList(Of StatementSyntax)
            Return StatementGenerator.GenerateStatements(CodeGenerationMethodInfo.GetStatements(method))
        End Function
    End Module
End Namespace
