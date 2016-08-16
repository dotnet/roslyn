' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class ComplexIdentifierSyntax
        Inherits IdentifierTokenSyntax

        Private ReadOnly _possibleKeywordKind As SyntaxKind
        Private ReadOnly _isBracketed As Boolean
        Private ReadOnly _identifierText As String
        Private ReadOnly _typeCharacter As TypeCharacter

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, precedingTrivia As GreenNode, followingTrivia As GreenNode, possibleKeywordKind As SyntaxKind, isBracketed As Boolean, identifierText As String, typeCharacter As TypeCharacter)
            MyBase.New(kind, errors, annotations, text, precedingTrivia, followingTrivia)

            Me._possibleKeywordKind = possibleKeywordKind
            Me._isBracketed = isBracketed
            Me._identifierText = identifierText
            Me._typeCharacter = typeCharacter

        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            _possibleKeywordKind = CType(reader.ReadUInt16(), SyntaxKind)
            _isBracketed = reader.ReadBoolean()
            _identifierText = reader.ReadString()
            _typeCharacter = CType(reader.ReadByte(), TypeCharacter)
        End Sub

        Friend Overrides Function GetReader() As Func(Of ObjectReader, Object)
            Return Function(r) New ComplexIdentifierSyntax(r)
        End Function

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteUInt16(CType(_possibleKeywordKind, UInt16))
            writer.WriteBoolean(_isBracketed)
            writer.WriteString(_identifierText)
            writer.WriteByte(CType(_typeCharacter, Byte))
        End Sub

        ''' <summary>
        ''' Contextual Nodekind
        ''' </summary>
        Friend Overrides ReadOnly Property PossibleKeywordKind As SyntaxKind
            Get
                Return Me._possibleKeywordKind
            End Get
        End Property

        Public Overrides ReadOnly Property RawContextualKind As Integer
            Get
                Return Me._possibleKeywordKind
            End Get
        End Property

        ''' <summary>
        ''' If true, the identifier was enclosed in brackets, such as "[End]".
        ''' </summary>
        Friend Overrides ReadOnly Property IsBracketed As Boolean
            Get
                Return Me._isBracketed
            End Get
        End Property

        ''' <summary>
        ''' The text of the identifier, not including the brackets or type character.
        ''' </summary>
        Friend Overrides ReadOnly Property IdentifierText As String
            Get
                Return Me._identifierText
            End Get
        End Property

        ''' <summary>
        ''' The type character suffix, if present. Returns TypeCharacter.None if no type
        ''' character was present. The only allowed values are None, Integer, Long,
        ''' Decimal, Single, Double, and String.
        ''' </summary>
        Friend Overrides ReadOnly Property TypeCharacter As TypeCharacter
            Get
                Return Me._typeCharacter
            End Get
        End Property

        Public Overrides Function WithLeadingTrivia(trivia As GreenNode) As GreenNode
            Return New ComplexIdentifierSyntax(Kind, GetDiagnostics, GetAnnotations, Text, trivia, GetTrailingTrivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function

        Public Overrides Function WithTrailingTrivia(trivia As GreenNode) As GreenNode
            Return New ComplexIdentifierSyntax(Kind, GetDiagnostics, GetAnnotations, Text, GetLeadingTrivia, trivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function

        Friend Overrides Function SetDiagnostics(newErrors As DiagnosticInfo()) As GreenNode
            Return New ComplexIdentifierSyntax(Kind, newErrors, GetAnnotations, Text, GetLeadingTrivia, GetTrailingTrivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function

        Friend Overrides Function SetAnnotations(annotations As SyntaxAnnotation()) As GreenNode
            Return New ComplexIdentifierSyntax(Kind, GetDiagnostics, annotations, Text, GetLeadingTrivia, GetTrailingTrivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function
    End Class

End Namespace
