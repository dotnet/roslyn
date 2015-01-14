' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
