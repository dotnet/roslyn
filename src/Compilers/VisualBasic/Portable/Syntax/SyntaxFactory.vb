﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports VbObjectDisplay = Microsoft.CodeAnalysis.VisualBasic.ObjectDisplay.ObjectDisplay

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFactory
        Public Shared ReadOnly CarriageReturnLineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.CarriageReturnLineFeed, SyntaxTrivia)
        Public Shared ReadOnly LineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.LineFeed, SyntaxTrivia)
        Public Shared ReadOnly CarriageReturn As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.CarriageReturn, SyntaxTrivia)
        Public Shared ReadOnly Space As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.Space, SyntaxTrivia)
        Public Shared ReadOnly Tab As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.Tab, SyntaxTrivia)

        Public Shared ReadOnly ElasticCarriageReturnLineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticCarriageReturnLineFeed, SyntaxTrivia)
        Public Shared ReadOnly ElasticLineFeed As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticLineFeed, SyntaxTrivia)
        Public Shared ReadOnly ElasticCarriageReturn As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticCarriageReturn, SyntaxTrivia)
        Public Shared ReadOnly ElasticSpace As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticSpace, SyntaxTrivia)
        Public Shared ReadOnly ElasticTab As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticTab, SyntaxTrivia)

        Public Shared ReadOnly ElasticMarker As SyntaxTrivia = CType(InternalSyntax.SyntaxFactory.ElasticZeroSpace, SyntaxTrivia)
        Private Shared ReadOnly s_elasticMarkerList As SyntaxTriviaList = SyntaxFactory.TriviaList(CType(InternalSyntax.SyntaxFactory.ElasticZeroSpace, SyntaxTrivia))

        Public Shared Function Whitespace(text As String, Optional elastic As Boolean = True) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.Whitespace(text, elastic), SyntaxTrivia)
        End Function

        Public Shared Function EndOfLine(text As String, Optional elastic As Boolean = True) As SyntaxTrivia
            Return CType(InternalSyntax.SyntaxFactory.EndOfLine(text, elastic), SyntaxTrivia)
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("I", StringComparison.OrdinalIgnoreCase), TypeCharacter.IntegerLiteral, TypeCharacter.None), CULng(value),
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("UI", StringComparison.OrdinalIgnoreCase), TypeCharacter.UIntegerLiteral, TypeCharacter.None), value,
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("L", StringComparison.OrdinalIgnoreCase), TypeCharacter.LongLiteral, TypeCharacter.None), CULng(value),
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
            Return CType(InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, If(text.StartsWith("&H", StringComparison.OrdinalIgnoreCase), LiteralBase.Hexadecimal, If(text.StartsWith("&O", StringComparison.OrdinalIgnoreCase), LiteralBase.Octal, If(text.StartsWith("&B", StringComparison.OrdinalIgnoreCase), LiteralBase.Binary, LiteralBase.Decimal))), If(text.EndsWith("UL", StringComparison.OrdinalIgnoreCase), TypeCharacter.ULongLiteral, TypeCharacter.None), value,
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
        ''' It it returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
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
        ''' It it returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        ''' </param>
        Public Shared Function AreEquivalent(Of TNode As SyntaxNode)(oldList As SeparatedSyntaxList(Of TNode), newList As SeparatedSyntaxList(Of TNode), Optional ignoreChildNode As Func(Of SyntaxKind, Boolean) = Nothing) As Boolean
            Return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, ignoreChildNode, topLevel:=False)
        End Function

    End Class
End Namespace
