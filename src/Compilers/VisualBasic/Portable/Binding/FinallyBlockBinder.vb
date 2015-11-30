﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

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

