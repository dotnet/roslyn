' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports VbObjectDisplay = Microsoft.CodeAnalysis.VisualBasic.ObjectDisplay.ObjectDisplay

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
            Return CType(InternalSyntax.SyntaxFactory.Token(DirectCast(ElasticMarker.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), kind, DirectCast(ElasticMarker.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), text), SyntaxToken)
        End Function

        Friend Shared Function Token(kind As SyntaxKind, trailing As SyntaxTrivia, Optional text As String = Nothing) As SyntaxToken
            Return Token(kind, SyntaxTriviaList.Create(trailing), text)
        End Function

        Public Shared Function Token(kind As SyntaxKind, trailing As SyntaxTriviaList, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(DirectCast(ElasticMarker.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), kind, DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode), text), SyntaxToken)
        End Function

        Public Shared Function Token(leading As SyntaxTriviaList, kind As SyntaxKind, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), kind, DirectCast(ElasticMarker.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), text), SyntaxToken)
        End Function

        Friend Shared Function Token(leading As SyntaxTrivia, kind As SyntaxKind, trailing As SyntaxTrivia, Optional text As String = Nothing) As SyntaxToken
            Return Token(SyntaxTriviaList.Create(leading), kind, SyntaxTriviaList.Create(trailing), text)
        End Function

        Public Shared Function Token(leading As SyntaxTriviaList, kind As SyntaxKind, trailing As SyntaxTriviaList, Optional text As String = Nothing) As SyntaxToken
            VerifySyntaxKindOfToken(kind)
            Return CType(InternalSyntax.SyntaxFactory.Token(DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), kind, DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode), text), SyntaxToken)
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, LiteralBase.Decimal)), If(text.EndsWith("I", StringComparison.OrdinalIgnoreCase), TypeCharacter.IntegerLiteral, TypeCharacter.None), CULng(value),
                        DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, LiteralBase.Decimal)), If(text.EndsWith("UI", StringComparison.OrdinalIgnoreCase), TypeCharacter.UIntegerLiteral, TypeCharacter.None), value,
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, LiteralBase.Decimal)), If(text.EndsWith("L", StringComparison.OrdinalIgnoreCase), TypeCharacter.LongLiteral, TypeCharacter.None), CULng(value),
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, LiteralBase.Decimal)), If(text.EndsWith("UL", StringComparison.OrdinalIgnoreCase), TypeCharacter.ULongLiteral, TypeCharacter.None), value,
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
                        DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind StringLiteralToken from a string value. </summary>
        ''' <param name="value">The string value to be represented by the returned token.</param>
        Public Shared Function Literal(value As String) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.UseQuotes), value)
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
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
        End Function

        ''' <summary> Creates a token with kind CharacterLiteralToken from a character value. </summary>
        ''' <param name="value">The character value to be represented by the returned token.</param>
        Public Shared Function Literal(value As Char) As SyntaxToken
            Return Literal(VbObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.UseQuotes), value)
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
                    DirectCast(leading.Node, InternalSyntax.VisualBasicSyntaxNode), DirectCast(trailing.Node, InternalSyntax.VisualBasicSyntaxNode)), SyntaxToken)
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
        ''' Determines if a submission is complete.
        ''' Returns false if the syntax is valid but incomplete.
        ''' Returns true if the syntax is invalid or complete.
        ''' </summary>
        ''' <param name="tree">Syntax tree.</param>
        Public Shared Function IsCompleteSubmission(tree As SyntaxTree) As Boolean
            Dim options As VisualBasicParseOptions = DirectCast(tree.Options, VisualBasicParseOptions)
            Dim languageVersion As LanguageVersion = options.LanguageVersion

            If (tree Is Nothing) Then
                Throw New ArgumentNullException(NameOf(tree))
            End If

            If (Not tree.HasCompilationUnitRoot) Then
                Return False
            End If

            Dim compilation As CompilationUnitSyntax = DirectCast(tree.GetRoot(), CompilationUnitSyntax)

            For Each err In compilation.GetDiagnostics()
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

            Dim lastNode = compilation.ChildNodes().LastOrDefault()
            If lastNode Is Nothing Then
                ' Invalid submission. The compilation does not have any children.
                Return True
            End If

            Dim lastToken = lastNode.GetLastToken(includeZeroWidth:=True, includeSkipped:=True)

            If lastToken.HasTrailingTrivia AndAlso lastToken.TrailingTrivia.Last().IsKind(SyntaxKind.LineContinuationTrivia) Then
                ' Even if the compilation is correct but has a line continuation trivia return statement as incomplete.
                ' For example `Dim x = 12 _` has no compilation errors but should be treated as an incomplete statement.
                Return False
            ElseIf Not compilation.HasErrors Then
                ' No errors returned. This is a valid and complete submission.
                Return True
            ElseIf lastToken.IsMissing Then
                Return False
            End If

            For Each err In lastToken.GetDiagnostics()
                Select Case DirectCast(err.Code, ERRID)
                    Case ERRID.ERR_UnterminatedStringLiteral
                        If languageVersion = LanguageVersion.VisualBasic14 Then
                            Return False
                        End If
                End Select
            Next

            ' By default mark the submissions as invalid.
            Return True
        End Function
    End Class
End Namespace
