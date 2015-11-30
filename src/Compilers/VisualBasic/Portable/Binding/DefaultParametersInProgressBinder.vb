﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This binder keeps track of the set of parameters that are currently being evaluated
    ''' so that the set can be passed into the next call to ParameterSymbol.DefaultConstantValue (and
    ''' its callers).
    ''' </summary>
    Friend NotInheritable Class DefaultParametersInProgressBinder
        Inherits SymbolsInProgressBinder(Of ParameterSymbol)

        Friend Sub New(inProgress As SymbolsInProgress(Of ParameterSymbol), [next] As Binder)
            MyBase.New(inProgress, [next])
        End Sub

        Friend Overrides ReadOnly Property DefaultParametersInProgress As SymbolsInProgress(Of ParameterSymbol)
            Get
                Return inProgress
            End Get
        End Property
    End Class

    ''' <summary>
    ''' This binder keeps track of the set of symbols that are currently being evaluated
    ''' so that the set can be passed to methods to support breaking infinite recursion
    ''' cycles.
    ''' </summary>
    Friend MustInherit Class SymbolsInProgressBinder(Of T As Symbol)
        Inherits Binder

        Protected ReadOnly inProgress As SymbolsInProgress(Of T)

        Protected Sub New(inProgress As SymbolsInProgress(Of T), [next] As Binder)
            MyBase.New([next])
            Me.inProgress = inProgress
        End Sub

    End Class

End Namespace


