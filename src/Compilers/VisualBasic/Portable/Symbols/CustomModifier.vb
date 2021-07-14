' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a custom modifier (modopt/modreq).
    ''' </summary>
    Partial Friend MustInherit Class VisualBasicCustomModifier
        Inherits CustomModifier

        Protected ReadOnly m_Modifier As NamedTypeSymbol

        Private Sub New(modifier As NamedTypeSymbol)
            Debug.Assert(modifier IsNot Nothing)
            Me.m_Modifier = modifier
        End Sub

        ''' <summary>
        ''' A type used as a tag that indicates which type of modification applies.
        ''' </summary>
        Public Overrides ReadOnly Property Modifier As INamedTypeSymbol
            Get
                Return m_Modifier
            End Get
        End Property

        Public ReadOnly Property ModifierSymbol As NamedTypeSymbol
            Get
                Return m_Modifier
            End Get
        End Property

        Public MustOverride Overrides Function GetHashCode() As Integer

        Public MustOverride Overrides Function Equals(obj As Object) As Boolean

        Friend Shared Function CreateOptional(modifier As NamedTypeSymbol) As CustomModifier
            Return New OptionalCustomModifier(modifier)
        End Function

        Friend Shared Function CreateRequired(modifier As NamedTypeSymbol) As CustomModifier
            Return New RequiredCustomModifier(modifier)
        End Function

        Friend Shared Function Convert(customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol))) As ImmutableArray(Of CustomModifier)
            If customModifiers.IsDefault Then
                Return ImmutableArray(Of CustomModifier).Empty
            End If
            Return customModifiers.SelectAsArray(AddressOf Convert)
        End Function

        Private Shared Function Convert(customModifier As ModifierInfo(Of TypeSymbol)) As CustomModifier
            Dim modifier = DirectCast(customModifier.Modifier, NamedTypeSymbol)
            Return If(customModifier.IsOptional, CreateOptional(modifier), CreateRequired(modifier))
        End Function

        Private Class OptionalCustomModifier
            Inherits VisualBasicCustomModifier

            Public Sub New(modifier As NamedTypeSymbol)
                MyBase.New(modifier)
            End Sub

            Public Overrides ReadOnly Property IsOptional As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return m_Modifier.GetHashCode()
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, OptionalCustomModifier)

                Return other IsNot Nothing AndAlso other.m_Modifier.Equals(m_Modifier)
            End Function
        End Class

        Private Class RequiredCustomModifier
            Inherits VisualBasicCustomModifier

            Public Sub New(modifier As NamedTypeSymbol)
                MyBase.New(modifier)
            End Sub

            Public Overrides ReadOnly Property IsOptional As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return m_Modifier.GetHashCode()
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, RequiredCustomModifier)

                Return other IsNot Nothing AndAlso other.m_Modifier.Equals(m_Modifier)
            End Function
        End Class
    End Class
End Namespace
