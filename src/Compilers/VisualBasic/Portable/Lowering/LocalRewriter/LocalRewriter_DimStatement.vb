' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
