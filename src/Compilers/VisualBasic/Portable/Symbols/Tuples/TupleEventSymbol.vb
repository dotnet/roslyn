' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents an event of a tuple type (such as (int, byte).SomeEvent)
    ''' that is backed by an event within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TupleEventSymbol
        Inherits WrappedEventSymbol

        Private ReadOnly _containingType As TupleTypeSymbol

        Public Overrides ReadOnly Property IsTupleEvent As Boolean = True

        Public Overrides ReadOnly Property TupleUnderlyingEvent As EventSymbol
            Get
                Return UnderlyingEvent
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return UnderlyingEvent.Type
            End Get
        End Property

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(UnderlyingEvent.AddMethod)
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(UnderlyingEvent.RemoveMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(UnderlyingEvent.AssociatedField)
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(UnderlyingEvent.RaiseMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitInterfaceImplementation As Boolean
            Get
                Return UnderlyingEvent.IsExplicitInterfaceImplementation
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                Return UnderlyingEvent.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingEvent As EventSymbol)
            MyBase.New(underlyingEvent)
            _containingType = container
        End Sub

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, UnderlyingEvent.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return UnderlyingEvent.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, TupleEventSymbol))
        End Function

        Public Overloads Function Equals(other As TupleEventSymbol) As Boolean
            Return (other Is Me) OrElse (other IsNot Nothing AndAlso
                                         _containingType = other._containingType AndAlso
                                         UnderlyingEvent = other.UnderlyingEvent)
        End Function

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return UnderlyingEvent.GetAttributes()
        End Function

    End Class

End Namespace