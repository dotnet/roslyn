' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SynthesizedIntrinsicOperatorSymbol
        Inherits SynthesizedMethodBase

        Private ReadOnly _name As String
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _isCheckedBuiltin As Boolean

        Public Sub New(container As NamedTypeSymbol, name As String, rightType As TypeSymbol, returnType As TypeSymbol, isCheckedBuiltin As Boolean)
            MyBase.New(container)

            _name = name
            _returnType = returnType
            _parameters = (New ParameterSymbol() {New SynthesizedOperatorParameterSymbol(Me, container, 0, "left"),
                                                   New SynthesizedOperatorParameterSymbol(Me, rightType, 1, "right")}).AsImmutableOrNull()
            _isCheckedBuiltin = isCheckedBuiltin
        End Sub

        Public Sub New(container As NamedTypeSymbol, name As String, returnType As TypeSymbol, isCheckedBuiltin As Boolean)
            MyBase.New(container)

            _name = name
            _returnType = returnType
            _parameters = (New ParameterSymbol() {New SynthesizedOperatorParameterSymbol(Me, container, 0, "value")}).AsImmutableOrNull()
            _isCheckedBuiltin = isCheckedBuiltin
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
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

            If _isCheckedBuiltin = other._isCheckedBuiltin AndAlso
               _parameters.Length = other._parameters.Length AndAlso
               String.Equals(_name, other._name, StringComparison.Ordinal) AndAlso
               TypeSymbol.Equals(m_containingType, other.m_containingType, TypeCompareKind.ConsiderEverything) AndAlso
               TypeSymbol.Equals(_returnType, other._returnType, TypeCompareKind.ConsiderEverything) Then

                For i As Integer = 0 To _parameters.Length - 1
                    If Not TypeSymbol.Equals(_parameters(i).Type, other._parameters(i).Type, TypeCompareKind.ConsiderEverything) Then
                        Return False
                    End If
                Next

                Return True
            End If

            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_name, Hash.Combine(m_containingType, _parameters.Length))
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
                Return _isCheckedBuiltin
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

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
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
