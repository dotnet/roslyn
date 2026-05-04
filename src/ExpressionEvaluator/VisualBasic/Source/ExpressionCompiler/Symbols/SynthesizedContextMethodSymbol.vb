' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    ''' <summary>
    ''' A synthesized instance method used for binding expressions outside 
    ''' of a method - specifically, binding DebuggerDisplayAttribute expressions.
    ''' </summary>
    Friend NotInheritable Class SynthesizedContextMethodSymbol
        Inherits SynthesizedMethodBase

        Public Sub New(container As NamedTypeSymbol)
            MyBase.New(container)
        End Sub

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function GetOverloadResolutionPriority() As Integer
            Return 0
        End Function

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
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
    End Class
End Namespace
