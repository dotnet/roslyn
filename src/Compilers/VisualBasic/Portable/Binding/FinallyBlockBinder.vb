' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Binder for Finally blocks. 
    ''' Its purpose is to hide exit try label of the enclosing try binder.
    ''' </summary>
    Friend NotInheritable Class FinallyBlockBinder
        Inherits ExitableStatementBinder

        Public Sub New(enclosing As Binder)
            MyBase.New(enclosing, continueKind:=SyntaxKind.None, exitKind:=SyntaxKind.None)
        End Sub

        Public Overrides Function GetExitLabel(exitSyntaxKind As SyntaxKind) As LabelSymbol
            If exitSyntaxKind = SyntaxKind.ExitTryStatement Then
                ' Skip parent try binder. 
                ' Its exit label is not in scope when in Finally block
                Return ContainingBinder.ContainingBinder.GetExitLabel(exitSyntaxKind)
            End If

            Return MyBase.GetExitLabel(exitSyntaxKind)
        End Function
    End Class
End Namespace

