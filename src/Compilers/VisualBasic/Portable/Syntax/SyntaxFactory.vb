' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports VbObjectDisplay = Microsoft.CodeAnalysis.VisualBasic.ObjectDisplay.ObjectDisplay
Imports Parser = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Parser
Imports Feature = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Feature

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFactory
        ''' <summary>
        ''' A trivia with kind EndOfLineTrivia containing both the carriage return And line feed characters.
        ''' </summary>
        Public Shared ReadOnly Property CarriageReturnLineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.CarriageReturnLineFeed, SyntaxTrivia)

        ''' <summary>
        ''' A trivia with kind EndOfLineTrivia containing a single line feed character.
        ''' </summary>
        Public Shared ReadOnly Property LineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.LineFeed, SyntaxTrivia)

        ''' <summary>
        ''' A trivia with kind EndOfLineTrivia containing a single carriage return character.
        ''' </summary>
        Public Shared ReadOnly Property CarriageReturn As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.CarriageReturn, SyntaxTrivia)

        ''' <summary>
        '''  A trivia with kind WhitespaceTrivia containing a single space character.
        ''' </summary>
        Public Shared ReadOnly Property Space As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.Space, SyntaxTrivia)

        ''' <summary>
        ''' A trivia with kind WhitespaceTrivia containing a single tab character.
        ''' </summary>
        Public Shared ReadOnly Property Tab As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.Tab, SyntaxTrivia)

        ''' <summary>
        ''' An elastic trivia with kind EndOfLineTrivia containing both the carriage return And line feed characters.
        ''' Elastic trivia are used to denote trivia that was Not produced by parsing source text, And are usually Not
        ''' preserved during formatting.
        ''' </summary>
        Public Shared ReadOnly Property ElasticCarriageReturnLineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxTrivia)

        ''' <summary>
        ''' An elastic trivia with kind EndOfLineTrivia containing a single line feed character. Elastic trivia are used
        ''' to denote trivia that was Not produced by parsing source text, And are usually Not preserved during
        ''' formatting.
        ''' </summary>
        Public Shared ReadOnly Property ElasticLineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticLineFeed, SyntaxTrivia)

        ''' <summary>
        ''' An elastic trivia with kind EndOfLineTrivia containing a single carriage return character. Elastic trivia
        ''' are used to denote trivia that was Not produced by parsing source text, And are usually Not preserved during
        ''' formatting.
        ''' </summary>
        Public Shared ReadOnly Property ElasticCarriageReturn As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticCarriageReturn, SyntaxTrivia)

        ''' <summary>
        ''' An elastic trivia with kind WhitespaceTrivia containing a single space character. Elastic trivia are used to
        ''' denote trivia that was Not produced by parsing source text, And are usually Not preserved during formatting.
        ''' </summary>
        Public Shared ReadOnly Property ElasticSpace As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticSpace, SyntaxTrivia)

        ''' <summary>
        ''' An elastic trivia with kind WhitespaceTrivia containing a single tab character. Elastic trivia are used to
        ''' denote trivia that was Not produced by parsing source text, And are usually Not preserved during formatting.
        ''' </summary>
        Public Shared ReadOnly Property ElasticTab As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticTab, SyntaxTrivia)

        ''' <summary>
        ''' An elastic trivia with kind WhitespaceTrivia containing no characters. Elastic marker trivia are included
        ''' automatically by factory methods when trivia Is Not specified. Syntax formatting will replace elastic
        ''' markers with appropriate trivia.
        ''' </summary>
        Public Shared ReadOnly Property ElasticMarker As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticZeroSpace, SyntaxTrivia)
        Private Shared ReadOnly s_elasticMarkerList As SyntaxTriviaList = SyntaxFactory.TriviaList(CType(InternalSyntax.SyntaxFactory.ElasticZeroSpace, SyntaxTrivia))

        ''' <summary>
        ''' Creates a trivia with kind EndOfLineTrivia containing the specified text. 
        ''' </summary>
        ''' <param name="text">The text of the end of line. Any text can be specified here, however only carriage return And
        ''' line feed characters are recognized by the parser as end of line.</param>
        Public Shared Function EndOfLine(text As String) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.EndOfLine(text, elastic:=False), SyntaxTrivia)
        End Function

        ''' <summary>
        ''' Creates a trivia with kind EndOfLineTrivia containing the specified text. Elastic trivia are used to
        ''' denote trivia that was Not produced by parsing source text, And are usually Not preserved during formatting.
        ''' </summary>
        ''' <param name="text">The text of the end of line. Any text can be specified here, however only carriage return And
        ''' line feed characters are recognized by the parser as end of line.</param>
        Public Shared Function ElasticEndOfLine(text As String) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.EndOfLine(text, elastic:=True), SyntaxTrivia)
        End Function

        <Obsolete("Use SyntaxFactory.EndOfLine or SyntaxFactory.ElasticEndOfLine")>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        Public Shared Function EndOfLine(text As String, elastic As Boolean) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.EndOfLine(text, elastic), SyntaxTrivia)
        End Function

        ''' <summary>
        ''' Creates a trivia with kind WhitespaceTrivia containing the specified text.
        ''' </summary>
        ''' <param name="text">The text of the whitespace. Any text can be specified here, however only specific
        ''' whitespace characters are recognized by the parser.</param>
        Public Shared Function Whitespace(text As String) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.Whitespace(text, elastic:=False), SyntaxTrivia)
        End Function

        ''' <summary>
        ''' Creates a trivia with kind WhitespaceTrivia containing the specified text. Elastic trivia are used to
        ''' denote trivia that was Not produced by parsing source text, And are usually Not preserved during formatting.
        ''' </summary>
        ''' <param name="text">The text of the whitespace. Any text can be specified here, however only specific
        ''' whitespace characters are recognized by the parser.</param>
        Public Shared Function ElasticWhitespace(text As String) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.Whitespace(text, elastic:=True), SyntaxTrivia)
        End Function

        <Obsolete("Use SyntaxFactory.Whitespace or SyntaxFactory.ElasticWhitespace")>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        Public Shared Function Whitespace(text As String, elastic As Boolean) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.Whitespace(text, elastic), SyntaxTrivia)
        End Function

        ''' <summary> 
        ''' Names on the right of qualified names and member access expressions are not stand-alone expressions.
        ''' This method returns the appropriate parent of name syntax nodes that are on right of these constructs.
        ''' </summary> 
        Public Shared Function GetStandaloneExpression(node As ExpressionSyntax) As ExpressionSyntax
            Dim expr = TryCast(node, ExpressionSyntax)
            If expr IsNot Nothing Then
                Dim parent = TryCast(node.Parent, ExpressionSyntax)
                If parent IsNot Nothing Then
                    Select Case node.Kind
                        Case SyntaxKind.IdentifierName, SyntaxKind.GenericName
                            Select Case parent.Kind
                                Case SyntaxKind.QualifiedName
                                    If (DirectCast(parent, QualifiedNameSyntax)).Right Is node Then
                                        Return parent
                                    End If
                                Case SyntaxKind.SimpleMemberAccessExpression
                                    If (DirectCast(parent, MemberAccessExpressionSyntax)).Name Is node Then
                                        Return parent
                                    End If
                            End Select

                        Case SyntaxKind.XmlBracketedName
                            Select Case parent.Kind
                                Case SyntaxKind.XmlElementAccessExpression, SyntaxKind.XmlAttributeAccessExpression, SyntaxKind.XmlDescendantAccessExpression
                                    If (DirectCast(parent, XmlMemberAccessExpressionSyntax)).Name Is node Then
                                        Return parent
                                    End If
                            End Select

                        Case SyntaxKind.XmlElementStartTag, SyntaxKind.XmlElementEndTag
                            If parent.Kind = SyntaxKind.XmlElement Then
                                Return parent
                            End If
                    End Select
                End If
            End If

            Return expr
        End Function

        Friend Shared Sub VerifySyntaxKindOfToken(kind As SyntaxKind)
            Select Case kind
                Case SyntaxKind.AddHandlerKeyword To SyntaxKind.EndOfXmlToken,
                     SyntaxKind.NameOfKeyword,
                     SyntaxKind.DollarSignDoubleQuoteToken,
                     SyntaxKind.InterpolatedStringTextToken,
                     SyntaxKind.EndOfInterpolatedStringToken

                Case Else
                    Throw New ArgumentOutOfRangeException(NameOf(kind))
            End Select
        End Sub

        Public Shared Function Token(kind As SyntaxKind, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(ElasticMarker.UnderlyingNode, kind, ElasticMarker.UnderlyingNode, text), SyntaxToken)
        End Function

        Friend Shared Function Token(kind As SyntaxKind, trailing As SyntaxTrivia, Optional text As String = Nothing) As SyntaxToken
            Return Token(kind, SyntaxTriviaList.Create(trailing), text)
        End Function

        Public Shared Function Token(kind As SyntaxKind, trailing As SyntaxTriviaList, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(ElasticMarker.UnderlyingNode, kind, trailing.Node, text), SyntaxToken)
        End Function

        Public Shared Function Token(leading As SyntaxTriviaList, kind As SyntaxKind, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(leading.Node, kind, ElasticMarker.UnderlyingNode, text), SyntaxToken)
        End Function

        Friend Shared Function Token(leading As SyntaxTrivia, kind As SyntaxKind, trailing As SyntaxTrivia, Optional text As String = Nothing) As SyntaxToken
            Return Token(SyntaxTriviaList.Create(leading), kind, SyntaxTriviaList.Create(trailing), text)
        End Function

        Public Shared Function Token(leading As SyntaxTriviaList, kind As SyntaxKind, trailing As SyntaxTriviaList, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(leading.Node, kind, trailing.Node, text), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from a 4-byte signed integer value. </summary> 
        ''' <param name="value">The 4-byte signed integer value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Integer) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None), value)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 4-byte signed integer value. </summary> 
        ''' <param name="text">The raw text of the literal.</param> <param name="value">The 4-byte signed integer value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As Integer) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 4-byte signed integer value. </summary> 
        ''' <param name="leading">A list of trivia immediately preceding the token.</param> 
        ''' <param name="text">The raw text of the literal.</param> 
        ''' <param name="value">The 4-byte signed integer value to be represented by the returned token.</param> 
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As Integer, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("I", StringComparison.OrdinalIgnoreCase), TypeCharacter.IntegerLiteral, TypeCharacter.None), CULng(value),
                        leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from a 4-byte unsigned integer
        ''' value. </summary>
        ''' <param name="value">The 4-byte unsigned integer value to be represented by the returned token.</param>
        Public Shared Function Literal(value As UInteger) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 4-byte unsigned integer value. </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 4-byte unsigned integer value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As UInteger) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 4-byte unsigned integer value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 4-byte unsigned integer value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As UInteger, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("UI", StringComparison.OrdinalIgnoreCase), TypeCharacter.UIntegerLiteral, TypeCharacter.None), value,
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from an 8-byte signed integer value. </summary>
        ''' <param name="value">The 8-byte signed integer value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Long) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 8-byte signed integer value. </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 8-byte signed integer value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As Long) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 8-byte signed integer value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 8-byte signed integer value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As Long, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("L", StringComparison.OrdinalIgnoreCase), TypeCharacter.LongLiteral, TypeCharacter.None), CULng(value),
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from an 8-byte unsigned integer value. </summary>
        ''' <param name="value">The 8-byte unsigned integer value to be represented by the returned token.</param>
        Public Shared Function Literal(value As ULong) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 8-byte unsigned integer value. </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 8-byte unsigned integer value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As ULong) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind IntegerLiteralToken from the text and corresponding 8-byte unsigned integer value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 8-byte unsigned integer value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As ULong, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("UL", StringComparison.OrdinalIgnoreCase), TypeCharacter.ULongLiteral, TypeCharacter.None), value,
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind FloatingLiteralToken from a 4-byte floating point value. </summary>
        ''' <param name="value">The 4-byte floating point value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Single) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value)
        End Function

        ''' <summary> Creates a token with kind FloatingLiteralToken from the text and corresponding 4-byte floating point value. </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 4-byte floating point value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As Single) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind FloatingLiteralToken from the text and corresponding 4-byte floating point value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 4-byte floating point value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As Single, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.FloatingLiteralToken(text, If(text.EndsWith("F", StringComparison.Ordinal), TypeCharacter.Single, TypeCharacter.None), value,
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind FloatingLiteralToken from an 8-byte floating point value. </summary>
        ''' <param name="value">The 8-byte floating point value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Double) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None), value)
        End Function

        ''' <summary> Creates a token with kind FloatingLiteralToken from the text and corresponding 8-byte floating point value. </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 8-byte floating point value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As Double) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind FloatingLiteralToken from the text and corresponding 8-byte floating point value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The 8-byte floating point value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As Double, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.FloatingLiteralToken(text, If(text.EndsWith("R", StringComparison.OrdinalIgnoreCase), TypeCharacter.DoubleLiteral, TypeCharacter.None), value,
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind DecimalLiteralToken from a decimal value. </summary>
        ''' <param name="value">The decimal value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Decimal) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value)
        End Function

        ''' <summary> Creates a token with kind DecimalLiteralToken from the text and corresponding decimal value. </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The decimal value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As Decimal) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind DecimalLiteralToken from the text and corresponding decimal value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The decimal value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As Decimal, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.DecimalLiteralToken(text, If(text.EndsWith("M", StringComparison.OrdinalIgnoreCase), TypeCharacter.DecimalLiteral, TypeCharacter.None), value,
                        leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind StringLiteralToken from a string value. </summary>
        ''' <param name="value">The string value to be represented by the returned token.</param>
        Public Shared Function Literal(value As String) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.UseQuotes Or ObjectDisplayOptions.EscapeNonPrintableCharacters), value)
        End Function

        ''' <summary> Creates a token with kind StringLiteralToken from the text and corresponding string value. </summary>
        ''' <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        ''' <param name="value">The string value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As String) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind StringLiteralToken from the text and corresponding string value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        ''' <param name="value">The string value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As String, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.StringLiteralToken(text, value,
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind CharacterLiteralToken from a character value. </summary>
        ''' <param name="value">The character value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Char) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.UseQuotes Or ObjectDisplayOptions.EscapeNonPrintableCharacters), value)
        End Function

        ''' <summary> Creates a token with kind CharacterLiteralToken from the text and corresponding character value. </summary>
        ''' <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        ''' <param name="value">The character value to be represented by the returned token.</param>
        Public Shared Function Literal(text As String, value As Char) As SyntaxToken
            Return Literal(s_elasticMarkerList, text, value, s_elasticMarkerList)
        End Function

        ''' <summary> Creates a token with kind CharacterLiteralToken from the text and corresponding character value. </summary>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        ''' <param name="value">The character value to be represented by the returned token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function Literal(leading As SyntaxTriviaList, text As String, value As Char, trailing As SyntaxTriviaList) As SyntaxToken
            Return CType(InternalSyntax.SyntaxFactory.CharacterLiteralToken(text, value,
                    leading.Node, trailing.Node), SyntaxToken)
        End Function

        Public Shared Function TypeBlock(ByVal blockKind As SyntaxKind, ByVal begin As TypeStatementSyntax, Optional ByVal [inherits] As SyntaxList(Of InheritsStatementSyntax) = Nothing, Optional ByVal [implements] As SyntaxList(Of ImplementsStatementSyntax) = Nothing, Optional ByVal members As SyntaxList(Of StatementSyntax) = Nothing, Optional ByVal [end] As EndBlockStatementSyntax = Nothing) As TypeBlockSyntax
            Select Case blockKind
                Case SyntaxKind.ModuleBlock
                    Return SyntaxFactory.ModuleBlock(DirectCast(begin, ModuleStatementSyntax), [inherits], [implements], members, [end])

                Case SyntaxKind.ClassBlock
                    Return SyntaxFactory.ClassBlock(DirectCast(begin, ClassStatementSyntax), [inherits], [implements], members, [end])

                Case SyntaxKind.StructureBlock
                    Return SyntaxFactory.StructureBlock(DirectCast(begin, StructureStatementSyntax), [inherits], [implements], members, [end])

                Case SyntaxKind.InterfaceBlock
                    Return SyntaxFactory.InterfaceBlock(DirectCast(begin, InterfaceStatementSyntax), [inherits], [implements], members, [end])

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(blockKind)
            End Select
        End Function

        Public Shared Function TypeStatement(ByVal statementKind As SyntaxKind, Optional ByVal attributes As SyntaxList(Of AttributeListSyntax) = Nothing, Optional ByVal modifiers As SyntaxTokenList = Nothing, Optional ByVal keyword As SyntaxToken = Nothing, Optional ByVal identifier As SyntaxToken = Nothing, Optional ByVal typeParameterList As TypeParameterListSyntax = Nothing) As TypeStatementSyntax
            Select Case statementKind
                Case SyntaxKind.ModuleStatement
                    Return SyntaxFactory.ModuleStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case SyntaxKind.ClassStatement
                    Return SyntaxFactory.ClassStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case SyntaxKind.StructureStatement
                    Return SyntaxFactory.StructureStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case SyntaxKind.InterfaceStatement
                    Return SyntaxFactory.InterfaceStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statementKind)
            End Select
        End Function

        ''' <summary>
        ''' Creates an xml documentation comment that abstracts xml syntax creation.
        ''' </summary>
        ''' <param name="content">
        ''' A list of xml node syntax that will be the content within the xml documentation comment
        ''' (e.g. a summary element, a returns element, exception element and so on).
        ''' </param>
        Public Shared Function DocumentationComment(ParamArray content As XmlNodeSyntax()) As DocumentationCommentTriviaSyntax
            Return DocumentationCommentTrivia(List(content)).WithLeadingTrivia(DocumentationCommentExteriorTrivia("''' ")).WithTrailingTrivia(EndOfLine(""))
        End Function

        ''' <summary>
        ''' Creates a summary element within an xml documentation comment.
        ''' </summary>
        ''' <param name="content">A list of xml node syntax that will be the content within the summary element.</param>
        Public Shared Function XmlSummaryElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlSummaryElement(List(content))
        End Function

        ''' <summary>
        ''' Creates a summary element within an xml documentation comment.
        ''' </summary>
        ''' <param name="content">A list of xml node syntax that will be the content within the summary element.</param>
        Public Shared Function XmlSummaryElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlMultiLineElement(DocumentationCommentXmlNames.SummaryElementName, content)
        End Function

        ''' <summary>
        ''' Creates a see element within an xml documentation comment.
        ''' </summary>
        ''' <param name="cref">A cref syntax node that points to the referenced item (e.g. a class, struct).</param>
        Public Shared Function XmlSeeElement(cref As CrefReferenceSyntax) As XmlEmptyElementSyntax
            Return XmlEmptyElement(DocumentationCommentXmlNames.SeeElementName).AddAttributes(XmlCrefAttribute(cref))
        End Function

        ''' <summary>
        ''' Creates a seealso element within an xml documentation comment.
        ''' </summary>
        ''' <param name="cref">A cref syntax node that points to the referenced item (e.g. a class, struct).</param>
        Public Shared Function XmlSeeAlsoElement(cref As CrefReferenceSyntax) As XmlEmptyElementSyntax
            Return XmlEmptyElement(DocumentationCommentXmlNames.SeeAlsoElementName).AddAttributes(XmlCrefAttribute(cref))
        End Function

        ''' <summary>
        ''' Creates a seealso element within an xml documentation comment.
        ''' </summary>
        ''' <param name="linkAddress">The uri of the referenced item.</param>
        ''' <param name="linkText"> A list of xml node syntax that will be used as the link text for the referenced item.</param>
        Public Shared Function XmlSeeAlsoElement(linkAddress As Uri, linkText As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Dim linkAddressString = linkAddress.ToString().ToLowerInvariant()
            Dim element = XmlElement(DocumentationCommentXmlNames.SeeAlsoElementName, linkText)

            Return element.WithStartTag(element.StartTag.AddAttributes(
                XmlAttribute(
                    XmlName(Nothing, XmlTextLiteralToken(DocumentationCommentXmlNames.CrefAttributeName, DocumentationCommentXmlNames.CrefAttributeName)),
                    XmlString(
                        Token(SyntaxKind.DoubleQuoteToken),
                        SyntaxTokenList.Create(
                            XmlTextLiteralToken(linkAddressString, linkAddressString)),
                        Token(SyntaxKind.DoubleQuoteToken)))))
        End Function

        ''' <summary>
        ''' Creates a threadsafety element within an xml documentation comment.
        ''' </summary>
        Public Shared Function XmlThreadSafetyElement() As XmlEmptyElementSyntax
            Return XmlThreadSafetyElement(True, False)
        End Function

        ''' <summary>
        ''' Creates a threadsafety element within an xml documentation comment.
        ''' </summary>
        ''' <param name="isStatic" static="sfd">Indicates whether static member of this type are safe for multi-threaded operations.</param>
        ''' <param name="isInstance">Indicates whether instance members of this type are safe for multi-threaded operations.</param>
        ''' <threadsafety static="true" instance=""/>
        Public Shared Function XmlThreadSafetyElement(isStatic As Boolean, isInstance As Boolean) As XmlEmptyElementSyntax
            Dim staticValueString = isStatic.ToString().ToLowerInvariant()
            Dim instanceValueString = isInstance.ToString().ToLowerInvariant()

            Return XmlEmptyElement(XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.ThreadSafetyElementName, SyntaxKind.XmlNameToken)).WithTrailingTrivia(ElasticSpace)).AddAttributes(
                XmlAttribute(
                    XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.StaticAttributeName, SyntaxKind.XmlNameToken)),
                    XmlString(
                        Token(SyntaxKind.DoubleQuoteToken),
                        SyntaxTokenList.Create(XmlTextLiteralToken(staticValueString, staticValueString)),
                        Token(SyntaxKind.DoubleQuoteToken))).WithTrailingTrivia(ElasticSpace),
                XmlAttribute(
                    XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.InstanceAttributeName, SyntaxKind.XmlNameToken)),
                    XmlString(
                        Token(SyntaxKind.DoubleQuoteToken),
                        SyntaxTokenList.Create(XmlTextLiteralToken(instanceValueString, instanceValueString)),
                        Token(SyntaxKind.DoubleQuoteToken))))
        End Function

        ''' <summary>
        ''' Creates a syntax node for a name attribute in a xml element within a xml documentation comment.
        ''' </summary>
        ''' <param name="parameterName">The value of the name attribute.</param>
        Public Shared Function XmlNameAttribute(parameterName As String) As XmlNameAttributeSyntax
            Return XmlNameAttribute(XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.NameAttributeName, SyntaxKind.XmlNameToken)), Token(SyntaxKind.DoubleQuoteToken), IdentifierName(parameterName), Token(SyntaxKind.DoubleQuoteToken)).WithLeadingTrivia(Whitespace(" "))
        End Function

        ''' <summary>
        ''' Creates a syntax node for a preliminary element within a xml documentation comment.
        ''' </summary>
        Public Shared Function XmlPreliminaryElement() As XmlEmptyElementSyntax
            Return XmlEmptyElement(DocumentationCommentXmlNames.PreliminaryElementName)
        End Function

        ''' <summary>
        ''' Creates a syntax node for a cref attribute within a xml documentation comment.
        ''' </summary>
        ''' <param name="cref">The <see cref="CrefReferenceSyntax"/> used for the xml cref attribute syntax.</param>
        Public Shared Function XmlCrefAttribute(cref As CrefReferenceSyntax) As XmlCrefAttributeSyntax
            Return XmlCrefAttribute(cref, SyntaxKind.DoubleQuoteToken)
        End Function

        ''' <summary>
        ''' Creates a syntax node for a cref attribute within a xml documentation comment.
        ''' </summary>
        ''' <param name="cref">The <see cref="CrefReferenceSyntax"/> used for the xml cref attribute syntax.</param>
        ''' <param name="quoteKind">The kind of the quote for the referenced item in the cref attribute.</param>
        Public Shared Function XmlCrefAttribute(cref As CrefReferenceSyntax, quoteKind As SyntaxKind) As XmlCrefAttributeSyntax
            cref = cref.ReplaceTokens(cref.DescendantTokens(), AddressOf XmlReplaceBracketTokens)
            Return XmlCrefAttribute(XmlName(Nothing, XmlNameToken(DocumentationCommentXmlNames.CrefAttributeName, SyntaxKind.XmlNameToken)), Token(quoteKind), cref, Token(quoteKind)).WithLeadingTrivia(Whitespace(" "))
        End Function

        ''' <summary>
        ''' Creates a remarks element within an xml documentation comment.
        ''' </summary>
        ''' <param name="content">A list of xml node syntax that will be the content within the remarks element.</param>
        Public Shared Function XmlRemarksElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlRemarksElement(List(content))
        End Function

        ''' <summary>
        ''' Creates a remarks element within an xml documentation comment.
        ''' </summary>
        ''' <param name="content">A list of xml node syntax that will be the content within the remarks element.</param>
        Public Shared Function XmlRemarksElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlMultiLineElement(DocumentationCommentXmlNames.RemarksElementName, content)
        End Function

        ''' <summary>
        ''' Creates a returns element within an xml documentation comment.
        ''' </summary>
        ''' <param name="content">A list of xml node syntax that will be the content within the returns element.</param>
        Public Shared Function XmlReturnsElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlReturnsElement(List(content))
        End Function

        ''' <summary>
        ''' Creates a returns element within an xml documentation comment.
        ''' </summary>
        ''' <param name="content">A list of xml node syntax that will be the content within the returns element.</param>
        Public Shared Function XmlReturnsElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlMultiLineElement(DocumentationCommentXmlNames.ReturnsElementName, content)
        End Function

        ''' <summary>
        ''' Creates the the syntax representation of an xml value element (e.g. for xml documentation comments).
        ''' </summary>
        ''' <param name="content">A list of xml syntax nodes that represents the content of the value element.</param>
        Public Shared Function XmlValueElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlValueElement(List(content))
        End Function

        ''' <summary>
        ''' Creates the the syntax representation of an xml value element (e.g. for xml documentation comments).
        ''' </summary>
        ''' <param name="content">A list of xml syntax nodes that represents the content of the value element.</param>
        Public Shared Function XmlValueElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlMultiLineElement(DocumentationCommentXmlNames.ValueElementName, content)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an exception element within xml documentation comments.
        ''' </summary>
        ''' <param name="cref">Syntax representation of the reference to the exception type.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the exception element.</param>
        Public Shared Function XmlExceptionElement(cref As CrefReferenceSyntax, ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlExceptionElement(cref, List(content))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an exception element within xml documentation comments.
        ''' </summary>
        ''' <param name="cref">Syntax representation of the reference to the exception type.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the exception element.</param>
        Public Shared Function XmlExceptionElement(cref As CrefReferenceSyntax, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Dim element As XmlElementSyntax = XmlElement(DocumentationCommentXmlNames.ExceptionElementName, content)
            Return element.WithStartTag(element.StartTag.AddAttributes(XmlCrefAttribute(cref)))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a permission element within xml documentation comments.
        ''' </summary>
        ''' <param name="cref">Syntax representation of the reference to the permission type.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the permission element.</param>
        Public Shared Function XmlPermissionElement(cref As CrefReferenceSyntax, ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlPermissionElement(cref, List(content))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a permission element within xml documentation comments.
        ''' </summary>
        ''' <param name="cref">Syntax representation of the reference to the permission type.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the permission element.</param>
        Public Shared Function XmlPermissionElement(cref As CrefReferenceSyntax, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Dim element As XmlElementSyntax = XmlElement(DocumentationCommentXmlNames.PermissionElementName, content)
            Return element.WithStartTag(element.StartTag.AddAttributes(XmlCrefAttribute(cref)))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an example element within xml documentation comments.
        ''' </summary>
        ''' <param name="content">A list of syntax nodes that represents the content of the example element.</param>
        Public Shared Function XmlExampleElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlExampleElement(List(content))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an example element within xml documentation comments.
        ''' </summary>
        ''' <param name="content">A list of syntax nodes that represents the content of the example element.</param>
        Public Shared Function XmlExampleElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Dim element As XmlElementSyntax = XmlElement(DocumentationCommentXmlNames.ExampleElementName, content)
            Return element.WithStartTag(element.StartTag)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a para element within xml documentation comments.
        ''' </summary>
        ''' <param name="content">A list of syntax nodes that represents the content of the para element.</param>
        Public Shared Function XmlParaElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlParaElement(List(content))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a para element within xml documentation comments.
        ''' </summary>
        ''' <param name="content">A list of syntax nodes that represents the content of the para element.</param>
        Public Shared Function XmlParaElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlElement(DocumentationCommentXmlNames.ParaElementName, content)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a param element within xml documentation comments (e.g. for
        ''' documentation of method parameters).
        ''' </summary>
        ''' <param name="parameterName">The name of the parameter.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the param element (e.g. 
        ''' the description and meaning of the parameter).</param>
        Public Shared Function XmlParamElement(parameterName As String, ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlParamElement(parameterName, List(content))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a param element within xml documentation comments (e.g. for
        ''' documentation of method parameters).
        ''' </summary>
        ''' <param name="parameterName">The name of the parameter.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the param element (e.g. 
        ''' the description and meaning of the parameter).</param>
        Public Shared Function XmlParamElement(parameterName As String, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Dim element As XmlElementSyntax = XmlElement(DocumentationCommentXmlNames.ParameterElementName, content)
            Return element.WithStartTag(element.StartTag.AddAttributes(XmlNameAttribute(parameterName)))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a paramref element within xml documentation comments (e.g. for
        ''' referencing particular parameters of a method).
        ''' </summary>
        ''' <param name="parameterName">The name of the referenced parameter.</param>
        Public Shared Function XmlParamRefElement(parameterName As String) As XmlEmptyElementSyntax
            Return XmlEmptyElement(DocumentationCommentXmlNames.ParameterReferenceElementName).AddAttributes(XmlNameAttribute(parameterName))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a see element within xml documentation comments,
        ''' that points to the 'null' language keyword.
        ''' </summary>
        Public Shared Function XmlNullKeywordElement() As XmlEmptyElementSyntax
            Return XmlKeywordElement("null")
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a see element within xml documentation comments,
        ''' that points to a language keyword.
        ''' </summary>
        ''' <param name="keyword">The language keyword to which the see element points to.</param>
        Private Shared Function XmlKeywordElement(keyword As String) As XmlEmptyElementSyntax
            Dim attribute As XmlAttributeSyntax =
                XmlAttribute(
                    XmlName(
                        Nothing,
                        XmlTextLiteralToken(DocumentationCommentXmlNames.LangwordAttributeName, DocumentationCommentXmlNames.LangwordAttributeName)),
                    XmlString(
                        Token(SyntaxKind.DoubleQuoteToken),
                        SyntaxTokenList.Create(XmlTextLiteralToken(keyword, keyword)),
                        Token(SyntaxKind.DoubleQuoteToken)))

            Return XmlEmptyElement(DocumentationCommentXmlNames.SeeElementName).AddAttributes(attribute)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a placeholder element within xml documentation comments.
        ''' </summary>
        ''' <param name="content">A list of syntax nodes that represents the content of the placeholder element.</param>
        Public Shared Function XmlPlaceholderElement(ParamArray content As XmlNodeSyntax()) As XmlElementSyntax
            Return XmlPlaceholderElement(List(content))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a placeholder element within xml documentation comments.
        ''' </summary>
        ''' <param name="content">A list of syntax nodes that represents the content of the placeholder element.</param>
        Public Shared Function XmlPlaceholderElement(content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlElement(DocumentationCommentXmlNames.PlaceholderElementName, content)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a named empty xml element within xml documentation comments.
        ''' </summary>
        ''' <param name="localName">The name of the empty xml element.</param>
        Public Shared Function XmlEmptyElement(localName As String) As XmlEmptyElementSyntax
            Return XmlEmptyElement(XmlName(Nothing, XmlNameToken(localName, SyntaxKind.XmlNameToken)))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a named xml element within xml documentation comments.
        ''' </summary>
        ''' <param name="localName">The name of the empty xml element.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the xml element.</param>
        Public Shared Function XmlElement(localName As String, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlElement(XmlName(Nothing, XmlNameToken(localName, SyntaxKind.XmlNameToken)), content)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of a named xml element within xml documentation comments.
        ''' </summary>
        ''' <param name="name">The name of the empty xml element.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the xml element.</param>
        Public Shared Function XmlElement(name As XmlNameSyntax, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlElement(XmlElementStartTag(name), content, XmlElementEndTag(name))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an xml element that spans multiple text lines.
        ''' </summary>
        ''' <param name="localName">The name of the xml element.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the xml multi line element.</param>
        Public Shared Function XmlMultiLineElement(localName As String, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlMultiLineElement(XmlName(Nothing, XmlNameToken(localName, SyntaxKind.XmlNameToken)), content)
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an xml element that spans multiple text lines.
        ''' </summary>
        ''' <param name="name">The name of the xml element.</param>
        ''' <param name="content">A list of syntax nodes that represents the content of the xml multi line element.</param>
        Public Shared Function XmlMultiLineElement(name As XmlNameSyntax, content As SyntaxList(Of XmlNodeSyntax)) As XmlElementSyntax
            Return XmlElement(XmlElementStartTag(name), content, XmlElementEndTag(name))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an xml text that contains a newline token with a documentation comment 
        ''' exterior trivia at the end (continued documentation comment).
        ''' </summary>
        ''' <param name="text">The raw text within the new line.</param>
        Public Shared Function XmlNewLine(text As String) As XmlTextSyntax
            Return XmlText(XmlTextNewLine(text))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an xml newline token with a documentation comment exterior trivia at 
        ''' the end (continued documentation comment).
        ''' </summary>
        ''' <param name="text">The raw text within the new line.</param>
        Public Shared Function XmlTextNewLine(text As String) As SyntaxToken
            Return XmlTextNewLine(text, True)
        End Function


        ''' <summary>
        ''' Creates a token with kind XmlTextLiteralNewLineToken.
        ''' </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The xml text new line value.</param>
        ''' <param name="leading">A list of trivia immediately preceding the token.</param>
        ''' <param name="trailing">A list of trivia immediately following the token.</param>
        Public Shared Function XmlTextNewLine(text As String, value As String, leading As SyntaxTriviaList, trailing As SyntaxTriviaList) As SyntaxToken
            Return New SyntaxToken(
                InternalSyntax.SyntaxFactory.DocumentationCommentLineBreakToken(
                    text,
                    value,
                    leading.Node,
                    trailing.Node))
        End Function

        ''' <summary>
        ''' Creates the syntax representation of an xml newline token for xml documentation comments.
        ''' </summary>
        ''' <param name="text">The raw text within the new line.</param>
        ''' <param name="continueXmlDocumentationComment">
        ''' If set to true, a documentation comment exterior token will be added to the trailing trivia
        ''' of the new token.</param>
        Public Shared Function XmlTextNewLine(text As String, continueXmlDocumentationComment As Boolean) As SyntaxToken
            Dim token = New SyntaxToken(
                InternalSyntax.SyntaxFactory.DocumentationCommentLineBreakToken(
                    text,
                    text,
                    ElasticMarker.UnderlyingNode,
                    ElasticMarker.UnderlyingNode))

            If continueXmlDocumentationComment Then
                token = token.WithTrailingTrivia(token.TrailingTrivia.Add(DocumentationCommentExteriorTrivia("''' ")))
            End If

            Return token
        End Function

        ''' <summary>
        ''' Generates the syntax representation of a xml text node (e.g. for xml documentation comments).
        ''' </summary>
        ''' <param name="value">The string literal used as the text of the xml text node.</param>
        Public Shared Function XmlText(value As String) As XmlTextSyntax
            Return XmlText(XmlTextLiteral(value))
        End Function

        ''' <summary>
        ''' Generates the syntax representation of a xml text node (e.g. for xml documentation comments).
        ''' </summary>
        ''' <param name="textTokens">A list of text tokens used as the text of the xml text node.</param>
        Public Shared Function XmlText(ParamArray textTokens As SyntaxToken()) As XmlTextSyntax
            Return XmlText(TokenList(textTokens))
        End Function

        ''' <summary>
        ''' Generates the syntax representation of an xml text literal.
        ''' </summary>
        ''' <param name="value">The text used within the xml text literal.</param>
        Public Shared Function XmlTextLiteral(value As String) As SyntaxToken
            ' TODO: [RobinSedlaczek] It is no compiler hot path here I think. But the contribution guide
            '       states to avoid LINQ (https://github.com/dotnet/roslyn/wiki/Contributing-Code). With
            '       XText we have a reference to System.Xml.Linq. Isn't this rule valid here? 
            Dim encoded As String = New XText(value).ToString()

            Return XmlTextLiteral(encoded, value)
        End Function

        ''' <summary>
        ''' Generates the syntax representation of an xml text literal.
        ''' </summary>
        ''' <param name="text">The raw text of the literal.</param>
        ''' <param name="value">The text used within the xml text literal.</param>
        Public Shared Function XmlTextLiteral(text As String, value As String) As SyntaxToken
            Return New SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.XmlTextLiteralToken(text, value, ElasticMarker.UnderlyingNode, ElasticMarker.UnderlyingNode))
        End Function

        ''' <summary>
        ''' Helper method that replaces less-than and greater-than characters with brackets. 
        ''' </summary>
        ''' <param name="originalToken">The original token that is to be replaced.</param>
        ''' <param name="rewrittenToken">The new rewritten token.</param>
        ''' <returns>Returns the new rewritten token with replaced characters.</returns>
        Private Shared Function XmlReplaceBracketTokens(originalToken As SyntaxToken, rewrittenToken As SyntaxToken) As SyntaxToken
            If rewrittenToken.IsKind(SyntaxKind.LessThanToken) AndAlso String.Equals("<", rewrittenToken.Text, StringComparison.Ordinal) Then
                Return Token(rewrittenToken.LeadingTrivia, SyntaxKind.LessThanToken, rewrittenToken.TrailingTrivia, rewrittenToken.ValueText)
            End If

            If rewrittenToken.IsKind(SyntaxKind.GreaterThanToken) AndAlso String.Equals(">", rewrittenToken.Text, StringComparison.Ordinal) Then
                Return Token(rewrittenToken.LeadingTrivia, SyntaxKind.GreaterThanToken, rewrittenToken.TrailingTrivia, rewrittenToken.ValueText)
            End If

            Return rewrittenToken
        End Function

        ''' <summary>
        ''' Determines if two trees are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldTree">The original tree.</param>
        ''' <param name="newTree">The new tree.</param>
        ''' <param name="topLevel"> 
        ''' True to ignore any differences of nodes inside bodies of methods, operators, constructors and accessors, and field and auto-property initializers, 
        ''' otherwise all nodes and tokens must be equivalent. 
        ''' </param>
        Public Shared Function AreEquivalent(oldTree As SyntaxTree, newTree As SyntaxTree, topLevel As Boolean) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldTree, newTree, ignoreChildNode:=Nothing, topLevel:=topLevel)
        End Function

        ''' <summary>
        ''' Determines if two syntax nodes are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldNode">The old node.</param>
        ''' <param name="newNode">The new node.</param>
        ''' <param name="topLevel"> 
        ''' True to ignore any differences of nodes inside bodies of methods, operators, constructors and accessors, and field and auto-property initializers, 
        ''' otherwise all nodes and tokens must be equivalent. 
        ''' </param>
        Public Shared Function AreEquivalent(oldNode As SyntaxNode, newNode As SyntaxNode, topLevel As Boolean) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldNode, newNode, ignoreChildNode:=Nothing, topLevel:=topLevel)
        End Function

        ''' <summary>
        ''' Determines if two syntax nodes are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldNode">The old node.</param>
        ''' <param name="newNode">The new node.</param>
        ''' <param name="ignoreChildNode">
        ''' If specified called for every child syntax node (not token) that is visited during the comparison. 
        ''' It it returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        ''' </param>
        Public Shared Function AreEquivalent(oldNode As SyntaxNode, newNode As SyntaxNode, Optional ignoreChildNode As Func(Of SyntaxKind, Boolean) = Nothing) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldNode, newNode, ignoreChildNode:=ignoreChildNode, topLevel:=False)
        End Function

        ''' <summary>
        ''' Determines if two syntax tokens are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldToken">The old token.</param>
        ''' <param name="newToken">The new token.</param>
        Public Shared Function AreEquivalent(oldToken As SyntaxToken, newToken As SyntaxToken) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldToken, newToken)
        End Function

        ''' <summary>
        ''' Determines if two lists of tokens are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldList">The old token list.</param>
        ''' <param name="newList">The new token list.</param>
        Public Shared Function AreEquivalent(oldList As SyntaxTokenList, newList As SyntaxTokenList) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldList, newList)
        End Function

        ''' <summary>
        ''' Determines if two lists of syntax nodes are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldList">The old list.</param>
        ''' <param name="newList">The new list.</param>
        ''' <param name="ignoreChildNode">
        ''' If specified called for every child syntax node (not token) that is visited during the comparison. 
        ''' It returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        ''' </param>
        Public Shared Function AreEquivalent(Of TNode As SyntaxNode)(oldList As SyntaxList(Of TNode), newList As SyntaxList(Of TNode), Optional ignoreChildNode As Func(Of SyntaxKind, Boolean) = Nothing) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, ignoreChildNode, topLevel:=False)
        End Function

        ''' <summary>
        ''' Determines if two lists of syntax nodes are the same, disregarding trivia differences.
        ''' </summary>
        ''' <param name="oldList">The old list.</param>
        ''' <param name="newList">The new list.</param>
        ''' <param name="ignoreChildNode">
        ''' If specified called for every child syntax node (not token) that is visited during the comparison. 
        ''' It returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        ''' </param>
        Public Shared Function AreEquivalent(Of TNode As SyntaxNode)(oldList As SeparatedSyntaxList(Of TNode), newList As SeparatedSyntaxList(Of TNode), Optional ignoreChildNode As Func(Of SyntaxKind, Boolean) = Nothing) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, ignoreChildNode, topLevel:=False)
        End Function

        ''' <summary>
        ''' Determines if a submission contains an LINQ query not followed by an empty line.
        '''
        ''' Examples:
        ''' 1. <c>Dim x = 1</c> returns false since the statement is not a LINQ query
        ''' 2. <c>
        ''' Dim x = FROM {1, 2, 3}
        '''
        ''' </c> return false since the statement is followed by an empty new line.
        ''' 3. <c>Dim x = FROM {1, 2, 3}</c> returns true since the LINQ statement is not followed by an empty new line.
        ''' </summary>
        ''' <param name="token">Last expected token of the last statement in a submission.</param>
        ''' <param name="statementNode">Top level statement which token is part of.</param>
        ''' <param name="endOfFileToken">Token that marks the end of submission.</param>
        Private Shared Function IsPartOfLinqQueryNotFollowedByNewLine(token As SyntaxToken, statementNode As SyntaxNode, endOfFileToken As SyntaxToken) As Boolean
            ' Checking if the submission ends with a new line.
            For Each leadingTrivia In endOfFileToken.LeadingTrivia
                If leadingTrivia.IsKind(SyntaxKind.EndOfLineTrivia) Then
                    Return False
                End If
            Next

            ' Checking if the last token is part of a LINQ query.
            Dim node = token.Parent
            Do
                If node.IsKind(SyntaxKind.QueryExpression) Then
                    Return True
                End If

                If node Is statementNode Then
                    Return False
                End If

                node = node.Parent
            Loop
        End Function

        ''' <summary>
        ''' Determines if a submission is complete.
        ''' Returns false if the syntax is valid but incomplete.
        ''' Returns true if the syntax is invalid or complete.
        ''' Throws <see cref="ArgumentNullException"/> in case the tree is null.
        ''' Throws <see cref="ArgumentException"/> in case the tree is not a submission.
        ''' </summary>
        ''' <param name="tree">Syntax tree.</param>
        Friend Shared Function IsCompleteSubmission(tree As SyntaxTree) As Boolean
            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If

            Dim options As VisualBasicParseOptions = DirectCast(tree.Options, VisualBasicParseOptions)
            If options.Kind = SourceCodeKind.Regular Then
                Throw New ArgumentException(VBResources.SyntaxTreeIsNotASubmission)
            End If

            Dim languageVersion As LanguageVersion = options.LanguageVersion

            If Not tree.HasCompilationUnitRoot Then
                Return False
            End If

            Dim compilationUnit As CompilationUnitSyntax = DirectCast(tree.GetRoot(), CompilationUnitSyntax)

            For Each err In compilationUnit.GetDiagnostics()
                Select Case DirectCast(err.Code, ERRID)
                    Case ERRID.ERR_LbExpectedEndIf,
                         ERRID.ERR_ExpectedEndRegion
                        Return False
                    Case ERRID.ERR_ExpectedEOS
                        ' Invalid statements that should have been separated by a newline or a colon.
                        ' For example: `If condition statement`
                        Return True
                End Select
            Next

            Dim lastTopLevelNode = compilationUnit.ChildNodes().LastOrDefault()
            If lastTopLevelNode Is Nothing Then
                ' Invalid submission. The compilation does not have any children.
                Return True
            End If

            Dim lastToken = lastTopLevelNode.GetLastToken(includeZeroWidth:=True, includeSkipped:=True)

            If IsPartOfLinqQueryNotFollowedByNewLine(lastToken, lastTopLevelNode, compilationUnit.EndOfFileToken) OrElse
                    (lastToken.HasTrailingTrivia AndAlso lastToken.TrailingTrivia.Last().IsKind(SyntaxKind.LineContinuationTrivia)) Then
                ' Even if the compilation is correct but has a line continuation trivia return statement as incomplete.
                ' For example `Dim x = 12 _` has no compilation errors but should be treated as an incomplete statement.
                Return False
            ElseIf Not compilationUnit.HasErrors Then
                ' No errors returned. This is a valid and complete submission.
                Return True
            ElseIf lastTopLevelNode.IsKind(SyntaxKind.IncompleteMember) OrElse lastToken.IsMissing Then
                Return False
            End If

            For Each err In lastToken.GetDiagnostics()
                Select Case DirectCast(err.Code, ERRID)
                    Case ERRID.ERR_UnterminatedStringLiteral
                        If Parser.CheckFeatureAvailability(languageVersion, Feature.MultilineStringLiterals) Then
                            Return False
                        End If
                End Select
            Next

            ' By default mark submissions as invalid since there's at least one error.
            Return True
        End Function
    End Class
End Namespace
