' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a preprocessing conditional compilation symbol.
    ''' </summary>
    Friend NotInheritable Class PreprocessingSymbol
        Inherits Symbol
        Implements IPreprocessingSymbol

        Private ReadOnly _name As String

        Friend Sub New(name As String)
            MyBase.New()
            _name = name
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(Of VisualBasicSyntaxNode)(Locations)
            End Get
        End Property

        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Preprocessing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

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

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
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
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            ElseIf obj Is Nothing Then
                Return False
            End If

            Dim other As PreprocessingSymbol = TryCast(obj, PreprocessingSymbol)

            Return other IsNot Nothing AndAlso
                IdentifierComparison.Equals(Me.Name, other.Name)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me.Name.GetHashCode()
        End Function

        Public Overloads Overrides Sub Accept(visitor As SymbolVisitor)
            Throw New NotSupportedException()
        End Sub

        Public Overloads Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            Throw New NotSupportedException()
        End Sub

        Public Overloads Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Throw New NotSupportedException()
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Throw New NotSupportedException()
        End Function

        Public Overloads Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Throw New NotSupportedException()
        End Function

        Friend Overloads Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Throw New NotSupportedException()
        End Function
    End Class

End Namespace
