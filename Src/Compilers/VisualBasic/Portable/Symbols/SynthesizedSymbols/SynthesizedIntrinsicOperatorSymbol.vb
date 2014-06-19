' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SynthesizedIntrinsicOperatorSymbol
        Inherits SynthesizedMethodBase

        Private ReadOnly m_Name As String
        Private ReadOnly m_Parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly m_ReturnType As TypeSymbol
        Private ReadOnly m_IsCheckedBuiltin As Boolean

        Public Sub New(container As NamedTypeSymbol, name As String, rightType As TypeSymbol, returnType As TypeSymbol, isCheckedBuiltin As Boolean)
            MyBase.New(container)

            m_Name = name
            m_ReturnType = returnType
            m_Parameters = (New ParameterSymbol() {New SynthesizedOperatorParameterSymbol(Me, container, 0, "left"),
                                                   New SynthesizedOperatorParameterSymbol(Me, rightType, 1, "right")}).AsImmutableOrNull()
            m_IsCheckedBuiltin = isCheckedBuiltin
        End Sub

        Public Sub New(container As NamedTypeSymbol, name As String, returnType As TypeSymbol, isCheckedBuiltin As Boolean)
            MyBase.New(container)

            m_Name = name
            m_ReturnType = returnType
            m_Parameters = (New ParameterSymbol() {New SynthesizedOperatorParameterSymbol(Me, container, 0, "value")}).AsImmutableOrNull()
            m_IsCheckedBuiltin = isCheckedBuiltin
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return m_Parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_ReturnType
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            End If

            Dim other = TryCast(obj, SynthesizedIntrinsicOperatorSymbol)

            If other Is Nothing Then
                Return False
            End If

            If m_IsCheckedBuiltin = other.m_IsCheckedBuiltin AndAlso
               m_Parameters.Length = other.m_Parameters.Length AndAlso
               String.Equals(m_Name, other.m_Name, StringComparison.Ordinal) AndAlso
               m_containingType = other.m_containingType AndAlso
               m_ReturnType = other.m_ReturnType Then

                For i As Integer = 0 To m_Parameters.Length - 1
                    If m_Parameters(i).Type <> other.m_Parameters(i).Type Then
                        Return False
                    End If
                Next

                Return True
            End If

            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(m_Name, Hash.Combine(m_containingType, m_Parameters.Length))
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
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

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.BuiltinOperator
            End Get
        End Property

        Public Overrides ReadOnly Property IsCheckedBuiltin As Boolean
            Get
                Return m_IsCheckedBuiltin
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentId() As String
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return False
        End Function

        Private NotInheritable Class SynthesizedOperatorParameterSymbol
            Inherits SynthesizedParameterSimpleSymbol

            Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, name As String)
                MyBase.New(container, type, ordinal, name)
            End Sub

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, SynthesizedOperatorParameterSymbol)

                If other Is Nothing Then
                    Return False
                End If

                Return Ordinal = other.Ordinal AndAlso ContainingSymbol = other.ContainingSymbol
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(ContainingSymbol, Ordinal.GetHashCode())
            End Function
        End Class

    End Class
End Namespace
