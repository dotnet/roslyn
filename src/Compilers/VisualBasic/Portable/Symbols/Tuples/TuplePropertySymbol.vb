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

        Public Overrides ReadOnly Property IsTupleProperty As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property TupleUnderlyingProperty As PropertySymbol
            Get
                Return Me._underlyingProperty
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._underlyingProperty.Type
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingProperty.TypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingProperty.RefCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Dim isDefault As Boolean = Me._lazyParameters.IsDefault
                If isDefault Then
                    InterlockedOperations.Initialize(Of ParameterSymbol)(Me._lazyParameters, Me.CreateParameters())
                End If
                Return Me._lazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(Me._underlyingProperty.GetMethod)
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of MethodSymbol)(Me._underlyingProperty.SetMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of FieldSymbol)(Me._underlyingProperty.AssociatedField)
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return Me._underlyingProperty.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return Me._underlyingProperty.IsOverloads
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return Me._underlyingProperty.IsMyGroupCollectionProperty
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingProperty As PropertySymbol)
            MyBase.New(underlyingProperty)
            Me._containingType = container
        End Sub

        Private Function CreateParameters() As ImmutableArray(Of ParameterSymbol)
            Return Me._underlyingProperty.Parameters.SelectAsArray(Of ParameterSymbol)(Function(p) New TupleParameterSymbol(Me, p))
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, Me._underlyingProperty.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._underlyingProperty.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TuplePropertySymbol))
        End Function

        Public Overloads Function Equals(other As TuplePropertySymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso TypeSymbol.Equals(Me._containingType, other._containingType, TypeCompareKind.ConsiderEverything) AndAlso Me._underlyingProperty = other._underlyingProperty)
        End Function

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._underlyingProperty.GetAttributes()
        End Function
    End Class
End Namespace
