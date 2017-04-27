' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a property of a tuple type (such as (int, byte).SomeProperty)
    ''' that is backed by a property within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TuplePropertySymbol
        Inherits WrappedPropertySymbol

        Private _containingType As TupleTypeSymbol

        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        Public Overrides ReadOnly Property IsTupleProperty As Boolean = True

        Public Overrides ReadOnly Property TupleUnderlyingProperty As PropertySymbol
            Get
                Return UnderlyingProperty
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return UnderlyingProperty.Type
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingProperty.TypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingProperty.RefCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Dim isDefault As Boolean = _lazyParameters.IsDefault
                If isDefault Then
                    InterlockedOperations.Initialize(Of ParameterSymbol)(_lazyParameters, CreateParameters())
                End If
                Return _lazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(UnderlyingProperty.GetMethod)
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(UnderlyingProperty.SetMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(Of FieldSymbol)(UnderlyingProperty.AssociatedField)
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return UnderlyingProperty.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return UnderlyingProperty.IsOverloads
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return UnderlyingProperty.IsMyGroupCollectionProperty
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingProperty As PropertySymbol)
            MyBase.New(underlyingProperty)
            _containingType = container
        End Sub

        Private Function CreateParameters() As ImmutableArray(Of ParameterSymbol)
            Return UnderlyingProperty.Parameters.SelectAsArray(Of ParameterSymbol)(Function(p) New TupleParameterSymbol(Me, p))
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, UnderlyingProperty.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return UnderlyingProperty.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, TuplePropertySymbol))
        End Function

        Public Overloads Function Equals(other As TuplePropertySymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso _containingType = other._containingType AndAlso UnderlyingProperty = other.UnderlyingProperty)
        End Function

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return UnderlyingProperty.GetAttributes()
        End Function
    End Class
End Namespace
