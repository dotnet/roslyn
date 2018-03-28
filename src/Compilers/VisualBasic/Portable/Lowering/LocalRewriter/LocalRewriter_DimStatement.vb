' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitDimStatement(node As BoundDimStatement) As BoundNode
            Dim inits As ArrayBuilder(Of BoundStatement) = Nothing

            For Each decl In node.LocalDeclarations
                Dim init As BoundNode = Me.Visit(decl)
                If init IsNot Nothing Then
                    If inits Is Nothing Then
                        inits = ArrayBuilder(Of BoundStatement).GetInstance
                    End If
                    inits.Add(DirectCast(init, BoundStatement))
                End If
            Next

            If inits IsNot Nothing Then
                Return New BoundStatementList(node.Syntax, inits.ToImmutableAndFree)
            Else
                Return Nothing
            End If
        End Function
    End Class
End Namespace
