' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a method of a tuple type (such as (int, byte).ToString())
    ''' that is backed by a method within the tuple underlying type.
    ''' </summary>
    Friend NotInheritable Class TupleMethodSymbol
        Inherits WrappedMethodSymbol

        Private ReadOnly _containingType As TupleTypeSymbol

        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        Public Overrides ReadOnly Property IsTupleMethod As Boolean = True

        Public Overrides ReadOnly Property TupleUnderlyingMethod As MethodSymbol
            Get
                Return UnderlyingMethod.ConstructedFrom
            End Get
        End Property

        Public Overrides ReadOnly Property UnderlyingMethod As MethodSymbol

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingMethod.ConstructedFrom.AssociatedSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return UnderlyingMethod.ConstructedFrom.ExplicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If _lazyParameters.IsDefault Then
                    InterlockedOperations.Initialize(Of ParameterSymbol)(_lazyParameters, CreateParameters())
                End If
                Return _lazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return UnderlyingMethod.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return UnderlyingMethod.ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingMethod.ReturnTypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingMethod.RefCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return StaticCast(Of TypeSymbol).From(_TypeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Public Sub New(container As TupleTypeSymbol, underlyingMethod As MethodSymbol)
            Debug.Assert(underlyingMethod.ConstructedFrom Is underlyingMethod)
            _containingType = container
            Me.UnderlyingMethod = underlyingMethod

            TypeParameters = underlyingMethod.TypeParameters
        End Sub

        Private Function CreateParameters() As ImmutableArray(Of ParameterSymbol)
            Return UnderlyingMethod.Parameters.SelectAsArray(Of ParameterSymbol)(Function(p) New TupleParameterSymbol(Me, p))
        End Function

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return UnderlyingMethod.GetAttributes()
        End Function

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return UnderlyingMethod.GetReturnTypeAttributes()
        End Function

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            Dim useSiteDiagnostic As DiagnosticInfo = MyBase.GetUseSiteErrorInfo()
            MyBase.MergeUseSiteErrorInfo(useSiteDiagnostic, UnderlyingMethod.GetUseSiteErrorInfo())
            Return useSiteDiagnostic
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _underlyingMethod.ConstructedFrom.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, TupleMethodSymbol))
        End Function

        Public Overloads Function Equals(other As TupleMethodSymbol) As Boolean
            Return (other Is Me) OrElse (other IsNot Nothing AndAlso
                                         _containingType = other._containingType AndAlso
                                         UnderlyingMethod.ConstructedFrom = other._underlyingMethod.ConstructedFrom)
        End Function
    End Class
End Namespace
