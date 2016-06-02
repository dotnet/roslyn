' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Partial Class CharacterLiteralTokenSyntax
        Friend NotOverridable Overrides ReadOnly Property ObjectValue As Object
            Get
                Return Me.Value
            End Get
        End Property
    End Class

    Friend Partial Class DateLiteralTokenSyntax
        Friend NotOverridable Overrides ReadOnly Property ObjectValue As Object
            Get
                Return Me.Value
            End Get
        End Property
    End Class

    Friend Partial Class DecimalLiteralTokenSyntax
        Friend NotOverridable Overrides ReadOnly Property ObjectValue As Object
            Get
                Return Me.Value
            End Get
        End Property
    End Class

    Friend Partial Class StringLiteralTokenSyntax
        Friend NotOverridable Overrides ReadOnly Property ObjectValue As Object
            Get
                Return Me.Value
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ValueText As String
            Get
                Return Me.Value
            End Get
        End Property
    End Class

    Friend NotInheritable Class IntegerLiteralTokenSyntax(Of T)
        Inherits IntegerLiteralTokenSyntax

        Friend ReadOnly _value As T

        Friend Sub New(kind As SyntaxKind, text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, base As LiteralBase, typeSuffix As TypeCharacter, value As T)
            MyBase.New(kind, text, leadingTrivia, trailingTrivia, base, typeSuffix)
            Me._value = value
        End Sub

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, base As LiteralBase, typeSuffix As TypeCharacter, value As T)
            MyBase.New(kind, errors, annotations, text, leadingTrivia, trailingTrivia, base, typeSuffix)
            Me._value = value
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            Me._value = CType(reader.ReadValue(), T)
        End Sub

        Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
            Return Function(r) New IntegerLiteralTokenSyntax(Of T)(r)
        End Function

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteValue(Me._value)
        End Sub

        ''' <summary>
        ''' The value of the token.
        ''' </summary>
        Friend ReadOnly Property Value As T
            Get
                Return Me._value
            End Get
        End Property

        Friend Overrides ReadOnly Property ValueText As String
            Get
                Return _value.ToString
            End Get
        End Property

        Friend Overrides ReadOnly Property ObjectValue As Object
            Get
                Return Me.Value
            End Get
        End Property

        Public Overrides Function WithLeadingTrivia(trivia As GreenNode) As GreenNode
            Return New IntegerLiteralTokenSyntax(Of T)(Kind, GetDiagnostics, GetAnnotations, Text, DirectCast(trivia, VisualBasicSyntaxNode), GetTrailingTrivia, _base, _typeSuffix, _value)
        End Function

        Public Overrides Function WithTrailingTrivia(trivia As GreenNode) As GreenNode
            Return New IntegerLiteralTokenSyntax(Of T)(Kind, GetDiagnostics, GetAnnotations, Text, GetLeadingTrivia, DirectCast(trivia, VisualBasicSyntaxNode), _base, _typeSuffix, _value)
        End Function

        Friend Overrides Function SetDiagnostics(newErrors As DiagnosticInfo()) As GreenNode
            Return New IntegerLiteralTokenSyntax(Of T)(Kind, newErrors, GetAnnotations, Text, GetLeadingTrivia, GetTrailingTrivia, _base, _typeSuffix, _value)
        End Function

        Friend Overrides Function SetAnnotations(annotations As SyntaxAnnotation()) As GreenNode
            Return New IntegerLiteralTokenSyntax(Of T)(Kind, GetDiagnostics, annotations, Text, GetLeadingTrivia, GetTrailingTrivia, _base, _typeSuffix, _value)
        End Function
    End Class

    ''' <summary>
    ''' Represents an integer literal token.
    ''' </summary>
    Friend MustInherit Class IntegerLiteralTokenSyntax
        Inherits SyntaxToken

        Friend ReadOnly _base As LiteralBase
        Friend ReadOnly _typeSuffix As TypeCharacter

        Friend Sub New(kind As SyntaxKind, text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, base As LiteralBase, typeSuffix As TypeCharacter)
            MyBase.New(kind, text, leadingTrivia, trailingTrivia)
            Me._base = base
            Me._typeSuffix = typeSuffix
        End Sub

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, base As LiteralBase, typeSuffix As TypeCharacter)
            MyBase.New(kind, errors, annotations, text, leadingTrivia, trailingTrivia)
            Me._base = base
            Me._typeSuffix = typeSuffix
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            Me._base = CType(reader.ReadByte(), LiteralBase)
            Me._typeSuffix = CType(reader.ReadByte(), TypeCharacter)
        End Sub

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteByte(CType(Me._base, Byte))
            writer.WriteByte(CType(Me._typeSuffix, Byte))
        End Sub

        ''' <summary>
        ''' Whether the token was specified in base 10, 16, 8, or 2.
        ''' </summary>
        Friend ReadOnly Property Base As LiteralBase
            Get
                Return Me._base
            End Get
        End Property

        ''' <summary>
        ''' The type suffix or type character that was on the literal, if any. If no suffix
        ''' was present, TypeCharacter.None is returned.
        ''' </summary>
        Friend ReadOnly Property TypeSuffix As TypeCharacter
            Get
                Return Me._typeSuffix
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Represents an floating literal token.
    ''' </summary>
    Friend NotInheritable Class FloatingLiteralTokenSyntax(Of T)
        Inherits FloatingLiteralTokenSyntax

        Friend ReadOnly _value As T

        Friend Sub New(kind As SyntaxKind, text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, typeSuffix As TypeCharacter, value As T)
            MyBase.New(kind, text, leadingTrivia, trailingTrivia, typeSuffix)
            Me._value = value
        End Sub

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, typeSuffix As TypeCharacter, value As T)
            MyBase.New(kind, errors, annotations, text, leadingTrivia, trailingTrivia, typeSuffix)
            Me._value = value
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            Me._value = CType(reader.ReadValue(), T)
        End Sub

        Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
            Return Function(r) New FloatingLiteralTokenSyntax(Of T)(r)
        End Function

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteValue(Me._value)
        End Sub

        ''' <summary>
        ''' The value of the token.
        ''' </summary>
        Friend ReadOnly Property Value As T
            Get
                Return Me._value
            End Get
        End Property

        Friend Overrides ReadOnly Property ValueText As String
            Get
                Return _value.ToString
            End Get
        End Property

        Friend Overrides ReadOnly Property ObjectValue As Object
            Get
                Return Me.Value
            End Get
        End Property

        Public Overrides Function WithLeadingTrivia(trivia As GreenNode) As GreenNode
            Return New FloatingLiteralTokenSyntax(Of T)(Kind, GetDiagnostics, GetAnnotations, Text, DirectCast(trivia, VisualBasicSyntaxNode), GetTrailingTrivia, _typeSuffix, _value)
        End Function

        Public Overrides Function WithTrailingTrivia(trivia As GreenNode) As GreenNode
            Return New FloatingLiteralTokenSyntax(Of T)(Kind, GetDiagnostics, GetAnnotations, Text, GetLeadingTrivia, DirectCast(trivia, VisualBasicSyntaxNode), _typeSuffix, _value)
        End Function

        Friend Overrides Function SetDiagnostics(newErrors As DiagnosticInfo()) As GreenNode
            Return New FloatingLiteralTokenSyntax(Of T)(Kind, newErrors, GetAnnotations, Text, GetLeadingTrivia, GetTrailingTrivia, _typeSuffix, _value)
        End Function

        Friend Overrides Function SetAnnotations(annotations As SyntaxAnnotation()) As GreenNode
            Return New FloatingLiteralTokenSyntax(Of T)(Kind, GetDiagnostics, annotations, Text, GetLeadingTrivia, GetTrailingTrivia, _typeSuffix, _value)
        End Function
    End Class

    ''' <summary>
    ''' Represents an floating literal token.
    ''' </summary>
    Friend MustInherit Class FloatingLiteralTokenSyntax
        Inherits SyntaxToken

        Friend ReadOnly _typeSuffix As TypeCharacter

        Friend Sub New(kind As SyntaxKind, text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, typeSuffix As TypeCharacter)
            MyBase.New(kind, text, leadingTrivia, trailingTrivia)
            Me._typeSuffix = typeSuffix
        End Sub

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode, typeSuffix As TypeCharacter)
            MyBase.New(kind, errors, annotations, text, leadingTrivia, trailingTrivia)
            Me._typeSuffix = typeSuffix
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            Me._typeSuffix = CType(reader.ReadByte(), TypeCharacter)
        End Sub

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteByte(CType(Me._typeSuffix, Byte))
        End Sub

        ''' <summary>
        ''' The type suffix or type character that was on the literal, if any. If no suffix
        ''' was present, TypeCharacter.None is returned.
        ''' </summary>
        Friend ReadOnly Property TypeSuffix As TypeCharacter
            Get
                Return Me._typeSuffix
            End Get
        End Property
    End Class
End Namespace
