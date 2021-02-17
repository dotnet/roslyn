' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ''' <summary>
    ''' Represents an intrinsic debugger method with byref return type.
    ''' </summary>
    Friend NotInheritable Class PlaceholderMethodSymbol
        Inherits SynthesizedMethodBase

        Friend Delegate Function GetTypeParameters(method As PlaceholderMethodSymbol) As ImmutableArray(Of TypeParameterSymbol)
        Friend Delegate Function GetParameters(method As PlaceholderMethodSymbol) As ImmutableArray(Of ParameterSymbol)
        Friend Delegate Function GetReturnType(method As PlaceholderMethodSymbol) As TypeSymbol

        Private ReadOnly _name As String
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnType As TypeSymbol

        Friend Sub New(
            container As NamedTypeSymbol,
            name As String,
            getTypeParameters As GetTypeParameters,
            getReturnType As GetReturnType,
            getParameters As GetParameters)

            MyBase.New(container)
            _name = name
            _typeParameters = getTypeParameters(Me)
            _returnType = getReturnType(Me)
            _parameters = getParameters(Me)
        End Sub

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Friend
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(_typeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

#If DEBUG Then
        Protected Overrides Function CreateCciAdapter() As MethodSymbolAdapter
            Return New PlaceholderMethodSymbolAdapter(Me)
        End Function
#End If
    End Class

#If DEBUG Then
    Friend NotInheritable Class PlaceholderMethodSymbolAdapter
        Inherits MethodSymbolAdapter

        Friend Sub New(underlying As PlaceholderMethodSymbol)
            MyBase.New(underlying)
        End Sub
    End Class
#End If

#If DEBUG Then
    Partial Friend Class PlaceholderMethodSymbolAdapter
#Else
    Partial Friend Class PlaceholderMethodSymbol
#End If
        Implements Cci.ISignature

        Private ReadOnly Property ReturnValueIsByRef As Boolean Implements Cci.ISignature.ReturnValueIsByRef
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace
