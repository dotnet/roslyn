' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a method of a tuple type (such as (int, byte).ToString())
    ''' that is backed by a method within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TupleMethodSymbol
        Inherits WrappedMethodSymbol

        Private ReadOnly _containingType As TupleTypeSymbol

        Private ReadOnly _underlyingMethod As MethodSymbol

        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        Public Overrides ReadOnly Property IsTupleMethod As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property TupleUnderlyingMethod As MethodSymbol
            Get
                Return Me._underlyingMethod.ConstructedFrom
            End Get
        End Property

        Public Overrides ReadOnly Property UnderlyingMethod As MethodSymbol
            Get
                Return Me._underlyingMethod
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Me._containingType.GetTupleMemberSymbolForUnderlyingMember(Of Symbol)(Me._underlyingMethod.ConstructedFrom.AssociatedSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return Me._underlyingMethod.ConstructedFrom.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If Me._lazyParameters.IsDefault Then
                    InterlockedOperations.Initialize(Of ParameterSymbol)(Me._lazyParameters, Me.CreateParameters())
                End If
                Return Me._lazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return Me._underlyingMethod.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return Me._underlyingMethod.ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingMethod.ReturnTypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingMethod.RefCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return StaticCast(Of TypeSymbol).From(Me._typeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return Me._typeParameters
            End Get
        End Property

        Public Sub New(container As TupleTypeSymbol, underlyingMethod As MethodSymbol)
            Debug.Assert(underlyingMethod.ConstructedFrom Is underlyingMethod)
            Me._containingType = container
            Me._underlyingMethod = underlyingMethod

            Me._typeParameters = Me._underlyingMethod.TypeParameters
        End Sub

        Private Function CreateParameters() As ImmutableArray(Of ParameterSymbol)
            Return Me._underlyingMethod.Parameters.SelectAsArray(Of ParameterSymbol)(Function(p) New TupleParameterSymbol(Me, p))
        End Function

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._underlyingMethod.GetAttributes()
        End Function

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._underlyingMethod.GetReturnTypeAttributes()
        End Function

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo()
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, Me._underlyingMethod.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._underlyingMethod.ConstructedFrom.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, TupleMethodSymbol))
        End Function

        Public Overloads Function Equals(other As TupleMethodSymbol) As Boolean
            Return other Is Me OrElse
                (other IsNot Nothing AndAlso TypeSymbol.Equals(Me._containingType, other._containingType, TypeCompareKind.ConsiderEverything) AndAlso Me._underlyingMethod.ConstructedFrom = other._underlyingMethod.ConstructedFrom)
        End Function
    End Class
End Namespace
