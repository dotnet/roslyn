' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a parameter of a method or a property of a tuple type
    ''' </summary>
    Friend NotInheritable Class TupleParameterSymbol
        Inherits WrappedParameterSymbol

        Private _container As Symbol

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._container
            End Get
        End Property

        Public Sub New(container As Symbol, underlyingParameter As ParameterSymbol)
            MyBase.New(underlyingParameter)

            Debug.Assert(container IsNot Nothing)
            Me._container = container
        End Sub

        Public Overrides Function GetHashCode() As Integer
            Return Me._underlyingParameter.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TupleParameterSymbol))
        End Function

        Public Overloads Function Equals(other As TupleParameterSymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso Me._container = other._container AndAlso Me._underlyingParameter = other._underlyingParameter)
        End Function
    End Class
End Namespace
