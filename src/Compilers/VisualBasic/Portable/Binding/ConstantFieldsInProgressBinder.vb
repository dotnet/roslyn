' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This binder keeps track of the set of constant fields that are currently being evaluated
    ''' so that the set can be passed into the next call to SourceFieldSymbol.ConstantValue (and
    ''' its callers).
    ''' </summary>
    Friend NotInheritable Class ConstantFieldsInProgressBinder
        Inherits Binder

        Private ReadOnly _inProgress As ConstantFieldsInProgress
        Private ReadOnly _field As FieldSymbol

        Friend Sub New(inProgress As ConstantFieldsInProgress, [next] As Binder, field As FieldSymbol)
            MyBase.New([next])
            Me._inProgress = inProgress
            Me._field = field
        End Sub

        Friend Overrides ReadOnly Property ConstantFieldsInProgress As ConstantFieldsInProgress
            Get
                Return _inProgress
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return _field
            End Get
        End Property

        Public Overrides ReadOnly Property AdditionalContainingMembers As ImmutableArray(Of Symbol)
            Get
                Return ImmutableArray(Of Symbol).Empty
            End Get
        End Property
    End Class

End Namespace

