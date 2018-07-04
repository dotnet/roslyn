' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    <Flags()>
    Friend Enum PropertyAccessKind
        Unknown = 0
        [Get] = &H1
        [Set] = &H2
    End Enum

    Partial Friend Class BoundPropertyAccess
        Public Sub New(syntax As SyntaxNode, propertySymbol As PropertySymbol, propertyGroupOpt As BoundPropertyGroup, accessKind As PropertyAccessKind, isWriteable As Boolean, receiverOpt As BoundExpression, arguments As ImmutableArray(Of BoundExpression),
                       Optional defaultArguments As BitVector = Nothing, Optional hasErrors As Boolean = False)
            Me.New(
                syntax,
                propertySymbol,
                propertyGroupOpt,
                accessKind,
                isWriteable:=isWriteable,
                isLValue:=propertySymbol.ReturnsByRef,
                receiverOpt:=receiverOpt,
                arguments:=arguments,
                defaultArguments:=defaultArguments,
                type:=GetTypeFromAccessKind(propertySymbol, accessKind),
                hasErrors:=hasErrors)
        End Sub

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.PropertySymbol
            End Get
        End Property

        ''' <summary>
        ''' Updates property access kind. To clear the access kind,
        ''' 'newAccessKind' should be Unknown. Otherwise, the current
        ''' access kind should be Unknown or equal to 'newAccessKind'.
        ''' </summary>
        Public Function SetAccessKind(newAccessKind As PropertyAccessKind) As BoundPropertyAccess
            Debug.Assert(newAccessKind = PropertyAccessKind.Unknown OrElse
                    Me.AccessKind = PropertyAccessKind.Unknown OrElse
                    Me.AccessKind = newAccessKind)
            Debug.Assert((newAccessKind And PropertyAccessKind.Set) = 0 OrElse
                    Not Me.PropertySymbol.ReturnsByRef)

            Return Me.Update(
                Me.PropertySymbol,
                Me.PropertyGroupOpt,
                newAccessKind,
                isWriteable:=IsWriteable,
                isLValue:=IsLValue,
                receiverOpt:=ReceiverOpt,
                arguments:=Arguments,
                defaultArguments:=DefaultArguments,
                type:=GetTypeFromAccessKind(Me.PropertySymbol, newAccessKind))
        End Function

#If DEBUG Then
        Private Sub Validate()
            ' if property group is specified it should not have receiver if it was moved to a bound call
            Debug.Assert(Me.ReceiverOpt Is Nothing OrElse Me.PropertyGroupOpt Is Nothing OrElse Me.PropertyGroupOpt.ReceiverOpt Is Nothing)

            Dim expectedType = GetTypeFromAccessKind(Me.PropertySymbol, Me.AccessKind)
            Debug.Assert(Me.Type = expectedType)
            Debug.Assert(DefaultArguments.IsNull OrElse Not Arguments.IsEmpty)
        End Sub
#End If

        Protected Overrides Function MakeRValueImpl() As BoundExpression
            Return MakeRValue()
        End Function

        Public Shadows Function MakeRValue() As BoundPropertyAccess
            Debug.Assert(Me.AccessKind <> PropertyAccessKind.Set)
            If _IsLValue Then
                Return Update(
                    PropertySymbol,
                    PropertyGroupOpt,
                    PropertyAccessKind.Get,
                    isWriteable:=IsWriteable,
                    isLValue:=False,
                    receiverOpt:=ReceiverOpt,
                    arguments:=Arguments,
                    defaultArguments:=DefaultArguments,
                    type:=Type)
            End If

            Return Me
        End Function

        Public Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                If PropertyGroupOpt IsNot Nothing Then
                    Return PropertyGroupOpt.ResultKind
                End If

                Return MyBase.ResultKind
            End Get
        End Property

        ''' <summary>
        ''' If the access includes a set, the type of the expression
        ''' is the type of the setter value parameter. Otherwise, the
        ''' type of the expression is the return type of the getter.
        ''' </summary>
        Private Shared Function GetTypeFromAccessKind([property] As PropertySymbol, accessKind As PropertyAccessKind) As TypeSymbol
            Return If((accessKind And PropertyAccessKind.Set) <> 0,
                          [property].GetTypeFromSetMethod(),
                          [property].GetTypeFromGetMethod())
        End Function
    End Class

End Namespace
