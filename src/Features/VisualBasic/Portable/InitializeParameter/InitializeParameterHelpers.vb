' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InitializeParameter
    Friend Class InitializeParameterHelpers
        Public Shared Function GetBody(containingMember As MethodBlockBaseSyntax) As SyntaxNode
            Return containingMember
        End Function

        Public Shared Function IsImplicitConversion(compilation As Compilation, source As ITypeSymbol, destination As ITypeSymbol) As Boolean
            Return compilation.ClassifyConversion(source:=source, destination:=destination).IsWidening
        End Function

        Public Shared Sub InsertStatement(
                editor As SyntaxEditor,
                methodBlock As MethodBlockBaseSyntax,
                statementToAddAfterOpt As SyntaxNode,
                statement As StatementSyntax)

            Dim statements = methodBlock.Statements

            If statementToAddAfterOpt IsNot Nothing Then
                editor.InsertAfter(statementToAddAfterOpt, statement)
            Else
                Dim newStatements = statements.Insert(0, statement)
                editor.SetStatements(methodBlock, newStatements)
            End If
        End Sub
    End Class
End Namespace
