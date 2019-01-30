' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents an event of a tuple type (such as (int, byte).SomeEvent)
    ''' that is backed by an event within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TupleEventSymbol
        Inherits WrappedEventSymbol

        Private ReadOnly _containingType As TupleTypeSymbol

        Public Overrides ReadOnly Property IsTupleEvent As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property TupleUnderlyingEvent As EventSymbol
            Get
                Return Me._underlyingEvent
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._underlyingEvent.Type
            End Get
        End Property

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(Me._underlyingEvent.AddMethod)
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(Me._underlyingEvent.RemoveMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of FieldSymbol)(Me._underlyingEvent.AssociatedField)
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(Me._underlyingEvent.RaiseMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitInterfaceImplementation As Boolean
            Get
                Return Me._underlyingEvent.IsExplicitInterfaceImplementation
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                Return Me._underlyingEvent.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingEvent As EventSymbol)
            MyBase.New(underlyingEvent)
            Me._containingType = container
        End Sub

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, Me._underlyingEvent.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._underlyingEvent.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TupleEventSymbol))
        End Function

        Public Overloads Function Equals(other As TupleEventSymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso TypeSymbol.Equals(Me._containingType, other._containingType, TypeCompareKind.ConsiderEverything) AndAlso Me._underlyingEvent = other._underlyingEvent)
        End Function

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._underlyingEvent.GetAttributes()
        End Function
    End Class
End Namespace
