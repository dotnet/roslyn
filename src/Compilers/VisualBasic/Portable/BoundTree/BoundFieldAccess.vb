' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundFieldAccess

        Public Sub New(syntax As SyntaxNode, receiverOpt As BoundExpression, fieldSymbol As FieldSymbol, isLValue As Boolean, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, receiverOpt, fieldSymbol, isLValue, False, Nothing, type, hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.FieldSymbol
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundFieldAccess
            If _IsLValue Then
                Return Update(_ReceiverOpt, _FieldSymbol, False, Me.SuppressVirtualCalls, Me.ConstantsInProgressOpt, Type)
            End If

            Return Me
        End Function

        Public Overrides ReadOnly Property ConstantValueOpt As ConstantValue
            Get
                ' decimal and datetime const fields require one-time synthesized assignment in cctor
                ' when used as a LHS of an assignment, the access is not a const.
                If _IsLValue Then
                    Return Nothing
                End If

                Dim result As ConstantValue

                Dim constantsInProgress = Me.ConstantsInProgressOpt
                If constantsInProgress IsNot Nothing Then
                    result = Me.FieldSymbol.GetConstantValue(constantsInProgress)
                Else
                    result = Me.FieldSymbol.GetConstantValue(ConstantFieldsInProgress.Empty)
                End If

#If DEBUG Then
                If constantsInProgress Is Nothing OrElse
                   constantsInProgress.IsEmpty OrElse
                   Not constantsInProgress.AnyDependencies() Then
                    ValidateConstantValue(Me.Type, result)
                End If
#End If
                Return result
            End Get
        End Property
    End Class
End Namespace
