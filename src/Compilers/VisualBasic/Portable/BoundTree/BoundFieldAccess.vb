' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundFieldAccess

        Public Sub New(syntax As VisualBasicSyntaxNode, receiverOpt As BoundExpression, fieldSymbol As FieldSymbol, isLValue As Boolean, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, receiverOpt, fieldSymbol, isLValue, False, Nothing, type, hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return FieldSymbol
            End Get
        End Property

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundFieldAccess
            If _IsLValue Then
                Return Update(_ReceiverOpt, _FieldSymbol, False, SuppressVirtualCalls, ConstantsInProgressOpt, Type)
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

                Dim constantsInProgress = ConstantsInProgressOpt
                If constantsInProgress IsNot Nothing Then
                    result = FieldSymbol.GetConstantValue(constantsInProgress)
                Else
                    result = FieldSymbol.GetConstantValue(SymbolsInProgress(Of FieldSymbol).Empty)
                End If

#If DEBUG Then
                ValidateConstantValue(Type, result)
#End If
                Return result
            End Get
        End Property
    End Class
End Namespace
