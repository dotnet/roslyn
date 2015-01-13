' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private ReadOnly inProgress As SymbolsInProgress(Of FieldSymbol)
        Private ReadOnly field As FieldSymbol

        Friend Sub New(inProgress As SymbolsInProgress(Of FieldSymbol), [next] As Binder, field As FieldSymbol)
            MyBase.New([next])
            Me.inProgress = inProgress
            Me.field = field
        End Sub

        Friend Overrides ReadOnly Property ConstantFieldsInProgress As SymbolsInProgress(Of FieldSymbol)
            Get
                Return inProgress
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingMember As Symbol
            Get
                Return field
            End Get
        End Property
    End Class

End Namespace

