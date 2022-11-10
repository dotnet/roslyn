' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
' Contains hand-written factories for the SyntaxNodes. Most factories are
' code-generated into SyntaxNodes.vb, but some are easier to hand-write.
'-----------------------------------------------------------------------------------------------------------

Imports System.Threading
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports InternalSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Syntax
Imports System.Collections.Immutable
Imports System.ComponentModel

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFactory

#Region "ParseMethods"

        ' direct access to parsing for common grammar areas

        ''' <summary>
        ''' Create a new syntax tree from a syntax node.
        ''' </summary>
        Public Shared Function SyntaxTree(
            root As SyntaxNode,
            Optional options As ParseOptions = Nothing,
            Optional path As String = "",
            Optional encoding As Encoding = Nothing) As SyntaxTree

            Return VisualBasicSyntaxTree.Create(DirectCast(root, VisualBasicSyntaxNode), If(DirectCast(options, VisualBasicParseOptions), VisualBasicParseOptions.Default), path, encoding, SourceHashAlgorithm.Sha1)
        End Function

        ''' <summary>
        ''' Produces a syntax tree by parsing the source text.
        ''' </summary>
        Public Shared Function ParseSyntaxTree(
            text As String,
            options As ParseOptions,
            path As String,
            encoding As Encoding,
            cancellationToken As CancellationToken) As SyntaxTree

            Return ParseSyntaxTree(SourceText.From(text, encoding, SourceHashAlgorithm.Sha1), options, path, cancellationToken)
        End Function

        ''' <summary>
        ''' Produces a syntax tree by parsing the source text.
        ''' </summary>
        Public Shared Function ParseSyntaxTree(
            text As SourceText,
            options As ParseOptions,
            path As String,
            cancellationToken As CancellationToken) As SyntaxTree

            Return VisualBasicSyntaxTree.ParseText(text, DirectCast(options, VisualBasicParseOptions), path, cancellationToken)
        End Function

#Disable Warning RS0026 ' Do not add multiple public overloads with optional parameters.
#Disable Warning RS0027 ' Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        ''' <summary>
        ''' Produces a syntax tree by parsing the source text.
        ''' </summary>
        Public Shared Function ParseSyntaxTree(
            text As String,
            Optional options As ParseOptions = Nothing,
            Optional path As String = "",
            Optional encoding As Encoding = Nothing,
            Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
            Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree

            Return ParseSyntaxTree(SourceText.From(text, encoding), options, path, diagnosticOptions, cancellationToken)
        End Function

        ''' <summary>
        ''' Produces a syntax tree by parsing the source text.
        ''' </summary>
        Public Shared Function ParseSyntaxTree(
            text As SourceText,
            Optional options As ParseOptions = Nothing,
            Optional path As String = "",
            Optional diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
            Optional cancellationToken As CancellationToken = Nothing) As SyntaxTree

            Return VisualBasicSyntaxTree.ParseText(text, DirectCast(options, VisualBasicParseOptions), path, diagnosticOptions, cancellationToken)
        End Function

#Enable Warning RS0026 ' Do not add multiple public overloads with optional parameters.
#Enable Warning RS0027 ' Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        ''' <summary>
        '''Parse the input for leading trivia.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseLeadingTrivia(text As String, Optional offset As Integer = 0) As SyntaxTriviaList
            Dim s = New InternalSyntax.Scanner(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
            Using s
                Return New SyntaxTriviaList(Nothing, s.ScanMultilineTrivia().Node, 0, 0)
            End Using
        End Function

        ''' <summary>
        ''' Parse the input for trailing trivia.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseTrailingTrivia(text As String, Optional offset As Integer = 0) As SyntaxTriviaList
            Dim s = New InternalSyntax.Scanner(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
            Using s
                Return New SyntaxTriviaList(Nothing, s.ScanSingleLineTrivia().Node, 0, 0)
            End Using
        End Function

        ''' <summary>
        ''' Parse one token.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        ''' <param name="startStatement">Scan using rules for the start of a statement</param>
        Public Shared Function ParseToken(text As String, Optional offset As Integer = 0, Optional startStatement As Boolean = False) As SyntaxToken
            Dim s = New InternalSyntax.Scanner(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
            Using s
                Dim state = If(startStatement,
                               InternalSyntax.ScannerState.VBAllowLeadingMultilineTrivia,
                               InternalSyntax.ScannerState.VB)
                s.GetNextTokenInState(state)
                Return New SyntaxToken(Nothing, s.GetCurrentToken, 0, 0)
            End Using
        End Function

        ''' <summary>
        ''' Parse tokens in the input.
        ''' Since this API does not create a <see cref="SyntaxNode"/> that owns all produced tokens,
        ''' the <see cref="SyntaxToken.GetLocation"/> API may yield surprising results for
        ''' the produced tokens and its behavior is generally unspecified.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        ''' <param name="initialTokenPosition">The position of the first token</param>
        Public Shared Iterator Function ParseTokens(text As String,
                                                    Optional offset As Integer = 0,
                                                    Optional initialTokenPosition As Integer = 0,
                                                    Optional options As VisualBasicParseOptions = Nothing) As IEnumerable(Of SyntaxToken)

            Using parser = New InternalSyntax.Parser(MakeSourceText(text, offset), If(options, VisualBasicParseOptions.Default))
                Dim state = InternalSyntax.ScannerState.VBAllowLeadingMultilineTrivia
                Dim curTk As InternalSyntax.SyntaxToken

                Do
                    parser.GetNextToken(state)
                    curTk = parser.CurrentToken

                    Yield New SyntaxToken(Nothing, curTk, initialTokenPosition, 0)

                    initialTokenPosition += curTk.FullWidth
                    state = If(curTk.Kind = SyntaxKind.StatementTerminatorToken,
                               InternalSyntax.ScannerState.VBAllowLeadingMultilineTrivia,
                               InternalSyntax.ScannerState.VB)

                Loop While curTk.Kind <> SyntaxKind.EndOfFileToken
            End Using
        End Function

        ''' <summary>
        ''' Parse a name.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseName(text As String, Optional offset As Integer = 0, Optional consumeFullText As Boolean = True) As NameSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
                p.GetNextToken()
                ' "allow everything" arguments
                Dim node = p.ParseName(
                        requireQualification:=False,
                        allowGlobalNameSpace:=True,
                        allowGenericArguments:=True,
                        allowGenericsWithoutOf:=False,
                        disallowGenericArgumentsOnLastQualifiedName:=False,
                        allowEmptyGenericArguments:=True,
                        allowedEmptyGenericArguments:=True)
                Return DirectCast(If(consumeFullText, p.ConsumeUnexpectedTokens(node), node).CreateRed(Nothing, 0), NameSyntax)
            End Using
        End Function

        ''' <summary>
        ''' Parse a type name.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseTypeName(text As String, Optional offset As Integer = 0, Optional options As ParseOptions = Nothing, Optional consumeFullText As Boolean = True) As TypeSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), If(DirectCast(options, VisualBasicParseOptions), VisualBasicParseOptions.Default))
                p.GetNextToken()
                Dim node = p.ParseGeneralType()
                Return DirectCast(If(consumeFullText, p.ConsumeUnexpectedTokens(node), node).CreateRed(Nothing, 0), TypeSyntax)
            End Using
        End Function

        '' Backcompat overload, do not touch
        ''' <summary>
        ''' Parse a type name.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        <EditorBrowsable(EditorBrowsableState.Never)>
        Public Shared Function ParseTypeName(text As String, offset As Integer, consumeFullText As Boolean) As TypeSyntax
            Return ParseTypeName(text, offset, options:=Nothing, consumeFullText)
        End Function

        ''' <summary>
        ''' Parse an expression.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseExpression(text As String, Optional offset As Integer = 0, Optional consumeFullText As Boolean = True) As ExpressionSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
                p.GetNextToken()
                Dim node = p.ParseExpression()
                Return DirectCast(If(consumeFullText, p.ConsumeUnexpectedTokens(node), node).CreateRed(Nothing, 0), ExpressionSyntax)
            End Using
        End Function

        ''' <summary>
        ''' Parse an executable statement.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseExecutableStatement(text As String, Optional offset As Integer = 0, Optional consumeFullText As Boolean = True) As StatementSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
                Dim node = p.ParseExecutableStatement()
                Return DirectCast(If(consumeFullText, p.ConsumeUnexpectedTokens(node), node).CreateRed(Nothing, 0), StatementSyntax)
            End Using
        End Function

        ''' <summary>
        ''' Parse a compilation unit (a single source file).
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseCompilationUnit(text As String, Optional offset As Integer = 0, Optional options As VisualBasicParseOptions = Nothing) As CompilationUnitSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), If(options, VisualBasicParseOptions.Default))
                Return DirectCast(p.ParseCompilationUnit().CreateRed(Nothing, 0), CompilationUnitSyntax)
            End Using
        End Function

        ''' <summary>
        ''' Parse a parameter list.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseParameterList(text As String, Optional offset As Integer = 0, Optional consumeFullText As Boolean = True) As ParameterListSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
                p.GetNextToken()
                Dim node = p.ParseParameterList()
                Return DirectCast(If(consumeFullText, p.ConsumeUnexpectedTokens(node), node).CreateRed(Nothing, 0), ParameterListSyntax)
            End Using
        End Function

        ''' <summary>
        ''' Parse an argument list.
        ''' </summary>
        ''' <param name="text">The input string</param>
        ''' <param name="offset">The starting offset in the string</param>
        Public Shared Function ParseArgumentList(text As String, Optional offset As Integer = 0, Optional consumeFullText As Boolean = True) As ArgumentListSyntax
            Using p = New InternalSyntax.Parser(MakeSourceText(text, offset), VisualBasicParseOptions.Default)
                p.GetNextToken()
                Dim node = p.ParseParenthesizedArguments()
                Return DirectCast(If(consumeFullText, p.ConsumeUnexpectedTokens(node), node).CreateRed(Nothing, 0), ArgumentListSyntax)
            End Using
        End Function

        ''' <summary>
        ''' Helper method for wrapping a string and offset in a SourceText.
        ''' </summary>
        Friend Shared Function MakeSourceText(text As String, offset As Integer) As SourceText
            Return SourceText.From(text, Encoding.UTF8).GetSubText(offset)
        End Function

        ''' <summary>
        ''' Try parse the attribute represented as a stand-alone string like [cref="A.B"] and recognize 
        ''' 'cref' and 'name' attributes like in documentation-comment mode. This method should only be
        '''  used internally from code handling documentation comment includes.
        ''' </summary>
        Friend Shared Function ParseDocCommentAttributeAsStandAloneEntity(text As String, parentElementName As String) As BaseXmlAttributeSyntax
            Using scanner As New InternalSyntax.Scanner(MakeSourceText(text, 0), VisualBasicParseOptions.Default) ' NOTE: Default options should be enough
                scanner.ForceScanningXmlDocMode()

                Dim parser = New InternalSyntax.Parser(scanner)
                parser.GetNextToken(InternalSyntax.ScannerState.Element)

                Dim xmlName = InternalSyntax.SyntaxFactory.XmlName(
                    Nothing, InternalSyntax.SyntaxFactory.XmlNameToken(parentElementName, SyntaxKind.XmlNameToken, Nothing, Nothing))

                Return DirectCast(
                    parser.ParseXmlAttribute(
                        requireLeadingWhitespace:=False,
                        AllowNameAsExpression:=False,
                        xmlElementName:=xmlName).CreateRed(Nothing, 0), BaseXmlAttributeSyntax)
            End Using
        End Function

#End Region

#Region "TokenFactories"
        Public Shared Function IntegerLiteralToken(text As String, base As LiteralBase, typeSuffix As TypeCharacter, value As ULong) As SyntaxToken
            Return IntegerLiteralToken(SyntaxFactory.TriviaList(ElasticMarker), text, base, typeSuffix, value, SyntaxFactory.TriviaList(ElasticMarker))
        End Function

        Public Shared Function IntegerLiteralToken(leadingTrivia As SyntaxTriviaList, text As String, base As LiteralBase, typeSuffix As TypeCharacter, value As ULong, trailingTrivia As SyntaxTriviaList) As SyntaxToken
            If text Is Nothing Then
                Throw New ArgumentNullException(NameOf(text))
            End If

            Return New SyntaxToken(Nothing, InternalSyntax.SyntaxFactory.IntegerLiteralToken(text, base, typeSuffix, value, leadingTrivia.Node, trailingTrivia.Node), 0, 0)
        End Function

        Public Shared Function FloatingLiteralToken(text As String, typeSuffix As TypeCharacter, value As Double) As SyntaxToken
            Return FloatingLiteralToken(SyntaxFactory.TriviaList(ElasticMarker), text, typeSuffix, value, SyntaxFactory.TriviaList(ElasticMarker))
        End Function

        Public Shared Function FloatingLiteralToken(leadingTrivia As SyntaxTriviaList, text As String, typeSuffix As TypeCharacter, value As Double, trailingTrivia As SyntaxTriviaList) As SyntaxToken
            If text Is Nothing Then
                Throw New ArgumentNullException(NameOf(text))
            End If

            Return New SyntaxToken(Nothing, InternalSyntax.SyntaxFactory.FloatingLiteralToken(text, typeSuffix, value, leadingTrivia.Node, trailingTrivia.Node), 0, 0)
        End Function

        Public Shared Function Identifier(text As String, isBracketed As Boolean, identifierText As String, typeCharacter As TypeCharacter) As SyntaxToken
            Return Identifier(SyntaxFactory.TriviaList(ElasticMarker), text, isBracketed, identifierText, typeCharacter, SyntaxFactory.TriviaList(ElasticMarker))
        End Function

        Friend Shared Function Identifier(leadingTrivia As SyntaxTrivia, text As String, isBracketed As Boolean, identifierText As String, typeCharacter As TypeCharacter, trailingTrivia As SyntaxTrivia) As SyntaxToken
            Return Identifier(SyntaxTriviaList.Create(leadingTrivia), text, isBracketed, identifierText, typeCharacter, SyntaxTriviaList.Create(trailingTrivia))
        End Function

        Public Shared Function Identifier(leadingTrivia As SyntaxTriviaList, text As String, isBracketed As Boolean, identifierText As String, typeCharacter As TypeCharacter, trailingTrivia As SyntaxTriviaList) As SyntaxToken
            If text Is Nothing Then
                Throw New ArgumentException(NameOf(text))
            End If

            Return New SyntaxToken(Nothing, New InternalSyntax.ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, text, leadingTrivia.Node, trailingTrivia.Node, SyntaxKind.IdentifierToken, isBracketed, identifierText, typeCharacter), 0, 0)
        End Function

        Public Shared Function Identifier(text As String) As SyntaxToken
            Return Identifier(SyntaxFactory.TriviaList(ElasticMarker), text, SyntaxFactory.TriviaList(ElasticMarker))
        End Function

        Friend Shared Function Identifier(leadingTrivia As SyntaxTrivia, text As String, trailingTrivia As SyntaxTrivia) As SyntaxToken
            Return Identifier(SyntaxTriviaList.Create(leadingTrivia), text, SyntaxTriviaList.Create(trailingTrivia))
        End Function

        Public Shared Function Identifier(leadingTrivia As SyntaxTriviaList, text As String, trailingTrivia As SyntaxTriviaList) As SyntaxToken
            If text Is Nothing Then
                Throw New ArgumentException(NameOf(text))
            End If

            Return New SyntaxToken(Nothing, New InternalSyntax.ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, text, leadingTrivia.Node, trailingTrivia.Node, SyntaxKind.IdentifierToken, False, text, TypeCharacter.None), 0, 0)
        End Function

        ''' <summary>
        ''' Create a bracketed identifier.
        ''' </summary>
        Public Shared Function BracketedIdentifier(text As String) As SyntaxToken
            Return BracketedIdentifier(SyntaxFactory.TriviaList(ElasticMarker), text, SyntaxFactory.TriviaList(ElasticMarker))
        End Function

        ''' <summary>
        ''' Create a bracketed identifier.
        ''' </summary>
        Public Shared Function BracketedIdentifier(leadingTrivia As SyntaxTriviaList, text As String, trailingTrivia As SyntaxTriviaList) As SyntaxToken
            If text Is Nothing Then
                Throw New ArgumentException(NameOf(text))
            End If

            If MakeHalfWidthIdentifier(text.First) = "[" AndAlso MakeHalfWidthIdentifier(text.Last) = "]" Then
                Throw New ArgumentException(NameOf(text))
            End If

            Return New SyntaxToken(Nothing, New InternalSyntax.ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, "[" + text + "]", leadingTrivia.Node, trailingTrivia.Node, SyntaxKind.IdentifierToken, True, text, TypeCharacter.None), 0, 0)
        End Function

        ''' <summary>
        ''' Create a missing identifier.
        ''' </summary>
        Friend Shared Function MissingIdentifier() As SyntaxToken
            Return New SyntaxToken(Nothing, New InternalSyntax.SimpleIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, "",
                    ElasticMarker.UnderlyingNode, ElasticMarker.UnderlyingNode), 0, 0)
        End Function

        ''' <summary>
        ''' Create a missing contextual keyword.
        ''' </summary>
        Friend Shared Function MissingIdentifier(kind As SyntaxKind) As SyntaxToken
            Return New SyntaxToken(Nothing, New InternalSyntax.ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, "",
                    ElasticMarker.UnderlyingNode, ElasticMarker.UnderlyingNode,
                    kind, False, "", TypeCharacter.None), 0, 0)
        End Function

        ''' <summary>
        ''' Create a missing keyword.
        ''' </summary>
        Friend Shared Function MissingKeyword(kind As SyntaxKind) As SyntaxToken
            Return New SyntaxToken(Nothing, New InternalSyntax.KeywordSyntax(kind, "",
                    ElasticMarker.UnderlyingNode, ElasticMarker.UnderlyingNode), 0, 0)
        End Function

        ''' <summary>
        ''' Create a missing punctuation mark.
        ''' </summary>
        Friend Shared Function MissingPunctuation(kind As SyntaxKind) As SyntaxToken
            Return New SyntaxToken(Nothing, New InternalSyntax.PunctuationSyntax(kind, "",
                    ElasticMarker.UnderlyingNode, ElasticMarker.UnderlyingNode), 0, 0)
        End Function

        ''' <summary>
        ''' Create a missing string literal.
        ''' </summary>
        Friend Shared Function MissingStringLiteral() As SyntaxToken
            Return SyntaxFactory.StringLiteralToken("", "")
        End Function

        ''' <summary>
        ''' Create a missing character literal.
        ''' </summary>
        Friend Shared Function MissingCharacterLiteralToken() As SyntaxToken
            Return CharacterLiteralToken("", Nothing)
        End Function

        ''' <summary>
        ''' Create a missing integer literal.
        ''' </summary>
        Friend Shared Function MissingIntegerLiteralToken() As SyntaxToken
            Return IntegerLiteralToken(SyntaxFactory.TriviaList(ElasticMarker), "", LiteralBase.Decimal, TypeCharacter.None, Nothing, SyntaxFactory.TriviaList(ElasticMarker))
        End Function

        ''' <summary>
        ''' Creates a copy of a token.
        ''' <para name="err"></para>
        ''' <para name="trivia"></para>
        ''' </summary>
        ''' <returns>The new token</returns>
        Friend Shared Function MissingToken(kind As SyntaxKind) As SyntaxToken
            Dim t As SyntaxToken

            Select Case kind
                Case SyntaxKind.StatementTerminatorToken
                    t = SyntaxFactory.Token(SyntaxKind.StatementTerminatorToken)

                Case SyntaxKind.EndOfFileToken
                    t = SyntaxFactory.Token(SyntaxKind.EndOfFileToken)

                Case SyntaxKind.AddHandlerKeyword,
                SyntaxKind.AddressOfKeyword,
                SyntaxKind.AliasKeyword,
                SyntaxKind.AndKeyword,
                SyntaxKind.AndAlsoKeyword,
                SyntaxKind.AsKeyword,
                SyntaxKind.BooleanKeyword,
                SyntaxKind.ByRefKeyword,
                SyntaxKind.ByteKeyword,
                SyntaxKind.ByValKeyword,
                SyntaxKind.CallKeyword,
                SyntaxKind.CaseKeyword,
                SyntaxKind.CatchKeyword,
                SyntaxKind.CBoolKeyword,
                SyntaxKind.CByteKeyword,
                SyntaxKind.CCharKeyword,
                SyntaxKind.CDateKeyword,
                SyntaxKind.CDecKeyword,
                SyntaxKind.CDblKeyword,
                SyntaxKind.CharKeyword,
                SyntaxKind.CIntKeyword,
                SyntaxKind.ClassKeyword,
                SyntaxKind.CLngKeyword,
                SyntaxKind.CObjKeyword,
                SyntaxKind.ConstKeyword,
                SyntaxKind.ContinueKeyword,
                SyntaxKind.CSByteKeyword,
                SyntaxKind.CShortKeyword,
                SyntaxKind.CSngKeyword,
                SyntaxKind.CStrKeyword,
                SyntaxKind.CTypeKeyword,
                SyntaxKind.CUIntKeyword,
                SyntaxKind.CULngKeyword,
                SyntaxKind.CUShortKeyword,
                SyntaxKind.DateKeyword,
                SyntaxKind.DecimalKeyword,
                SyntaxKind.DeclareKeyword,
                SyntaxKind.DefaultKeyword,
                SyntaxKind.DelegateKeyword,
                SyntaxKind.DimKeyword,
                SyntaxKind.DirectCastKeyword,
                SyntaxKind.DoKeyword,
                SyntaxKind.DoubleKeyword,
                SyntaxKind.EachKeyword,
                SyntaxKind.ElseKeyword,
                SyntaxKind.ElseIfKeyword,
                SyntaxKind.EndKeyword,
                SyntaxKind.EnumKeyword,
                SyntaxKind.EraseKeyword,
                SyntaxKind.ErrorKeyword,
                SyntaxKind.EventKeyword,
                SyntaxKind.ExitKeyword,
                SyntaxKind.FalseKeyword,
                SyntaxKind.FinallyKeyword,
                SyntaxKind.ForKeyword,
                SyntaxKind.FriendKeyword,
                SyntaxKind.FunctionKeyword,
                SyntaxKind.GetKeyword,
                SyntaxKind.GetTypeKeyword,
                SyntaxKind.GetXmlNamespaceKeyword,
                SyntaxKind.GlobalKeyword,
                SyntaxKind.GoToKeyword,
                SyntaxKind.HandlesKeyword,
                SyntaxKind.IfKeyword,
                SyntaxKind.ImplementsKeyword,
                SyntaxKind.ImportsKeyword,
                SyntaxKind.InKeyword,
                SyntaxKind.InheritsKeyword,
                SyntaxKind.IntegerKeyword,
                SyntaxKind.InterfaceKeyword,
                SyntaxKind.IsKeyword,
                SyntaxKind.IsNotKeyword,
                SyntaxKind.LetKeyword,
                SyntaxKind.LibKeyword,
                SyntaxKind.LikeKeyword,
                SyntaxKind.LongKeyword,
                SyntaxKind.LoopKeyword,
                SyntaxKind.MeKeyword,
                SyntaxKind.ModKeyword,
                SyntaxKind.ModuleKeyword,
                SyntaxKind.MustInheritKeyword,
                SyntaxKind.MustOverrideKeyword,
                SyntaxKind.MyBaseKeyword,
                SyntaxKind.MyClassKeyword,
                SyntaxKind.NameOfKeyword,
                SyntaxKind.NamespaceKeyword,
                SyntaxKind.NarrowingKeyword,
                SyntaxKind.NextKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.NotKeyword,
                SyntaxKind.NothingKeyword,
                SyntaxKind.NotInheritableKeyword,
                SyntaxKind.NotOverridableKeyword,
                SyntaxKind.ObjectKeyword,
                SyntaxKind.OfKeyword,
                SyntaxKind.OnKeyword,
                SyntaxKind.OperatorKeyword,
                SyntaxKind.OptionKeyword,
                SyntaxKind.OptionalKeyword,
                SyntaxKind.OrKeyword,
                SyntaxKind.OrElseKeyword,
                SyntaxKind.OverloadsKeyword,
                SyntaxKind.OverridableKeyword,
                SyntaxKind.OverridesKeyword,
                SyntaxKind.ParamArrayKeyword,
                SyntaxKind.PartialKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.PropertyKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.RaiseEventKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.ReDimKeyword,
                SyntaxKind.ReferenceKeyword,
                SyntaxKind.REMKeyword,
                SyntaxKind.RemoveHandlerKeyword,
                SyntaxKind.ResumeKeyword,
                SyntaxKind.ReturnKeyword,
                SyntaxKind.SByteKeyword,
                SyntaxKind.SelectKeyword,
                SyntaxKind.SetKeyword,
                SyntaxKind.ShadowsKeyword,
                SyntaxKind.SharedKeyword,
                SyntaxKind.ShortKeyword,
                SyntaxKind.SingleKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.StepKeyword,
                SyntaxKind.StopKeyword,
                SyntaxKind.StringKeyword,
                SyntaxKind.StructureKeyword,
                SyntaxKind.SubKeyword,
                SyntaxKind.SyncLockKeyword,
                SyntaxKind.ThenKeyword,
                SyntaxKind.ThrowKeyword,
                SyntaxKind.ToKeyword,
                SyntaxKind.TrueKeyword,
                SyntaxKind.TryKeyword,
                SyntaxKind.TryCastKeyword,
                SyntaxKind.TypeOfKeyword,
                SyntaxKind.UIntegerKeyword,
                SyntaxKind.ULongKeyword,
                SyntaxKind.UShortKeyword,
                SyntaxKind.UsingKeyword,
                SyntaxKind.WhenKeyword,
                SyntaxKind.WhileKeyword,
                SyntaxKind.WideningKeyword,
                SyntaxKind.WithKeyword,
                SyntaxKind.WithEventsKeyword,
                SyntaxKind.WriteOnlyKeyword,
                SyntaxKind.XorKeyword,
                SyntaxKind.EndIfKeyword,
                SyntaxKind.GosubKeyword,
                SyntaxKind.VariantKeyword,
                SyntaxKind.WendKeyword,
                SyntaxKind.OutKeyword
                    t = SyntaxFactory.MissingKeyword(kind)

                Case SyntaxKind.AggregateKeyword,
                SyntaxKind.AllKeyword,
                SyntaxKind.AnsiKeyword,
                SyntaxKind.AscendingKeyword,
                SyntaxKind.AssemblyKeyword,
                SyntaxKind.AutoKeyword,
                SyntaxKind.BinaryKeyword,
                SyntaxKind.ByKeyword,
                SyntaxKind.CompareKeyword,
                SyntaxKind.CustomKeyword,
                SyntaxKind.DescendingKeyword,
                SyntaxKind.DisableKeyword,
                SyntaxKind.DistinctKeyword,
                SyntaxKind.EnableKeyword,
                SyntaxKind.EqualsKeyword,
                SyntaxKind.ExplicitKeyword,
                SyntaxKind.ExternalSourceKeyword,
                SyntaxKind.ExternalChecksumKeyword,
                SyntaxKind.FromKeyword,
                SyntaxKind.GroupKeyword,
                SyntaxKind.InferKeyword,
                SyntaxKind.IntoKeyword,
                SyntaxKind.IsFalseKeyword,
                SyntaxKind.IsTrueKeyword,
                SyntaxKind.JoinKeyword,
                SyntaxKind.KeyKeyword,
                SyntaxKind.MidKeyword,
                SyntaxKind.OffKeyword,
                SyntaxKind.OrderKeyword,
                SyntaxKind.PreserveKeyword,
                SyntaxKind.RegionKeyword,
                SyntaxKind.SkipKeyword,
                SyntaxKind.StrictKeyword,
                SyntaxKind.TextKeyword,
                SyntaxKind.TakeKeyword,
                SyntaxKind.UnicodeKeyword,
                SyntaxKind.UntilKeyword,
                SyntaxKind.WarningKeyword,
                SyntaxKind.WhereKeyword
                    ' These are identifiers that have a contextual kind
                    Return SyntaxFactory.MissingIdentifier(kind)

                Case SyntaxKind.ExclamationToken,
                    SyntaxKind.CommaToken,
                    SyntaxKind.HashToken,
                    SyntaxKind.AmpersandToken,
                    SyntaxKind.SingleQuoteToken,
                    SyntaxKind.OpenParenToken,
                    SyntaxKind.CloseParenToken,
                    SyntaxKind.OpenBraceToken,
                    SyntaxKind.CloseBraceToken,
                    SyntaxKind.DoubleQuoteToken,
                    SyntaxKind.SemicolonToken,
                    SyntaxKind.AsteriskToken,
                    SyntaxKind.PlusToken,
                    SyntaxKind.MinusToken,
                    SyntaxKind.DotToken,
                    SyntaxKind.SlashToken,
                    SyntaxKind.ColonToken,
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanEqualsToken,
                    SyntaxKind.LessThanGreaterThanToken,
                    SyntaxKind.EqualsToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.GreaterThanEqualsToken,
                    SyntaxKind.BackslashToken,
                    SyntaxKind.CaretToken,
                    SyntaxKind.ColonEqualsToken,
                    SyntaxKind.AmpersandEqualsToken,
                    SyntaxKind.AsteriskEqualsToken,
                    SyntaxKind.PlusEqualsToken,
                    SyntaxKind.MinusEqualsToken,
                    SyntaxKind.SlashEqualsToken,
                    SyntaxKind.BackslashEqualsToken,
                    SyntaxKind.CaretEqualsToken,
                    SyntaxKind.LessThanLessThanToken,
                    SyntaxKind.GreaterThanGreaterThanToken,
                    SyntaxKind.LessThanLessThanEqualsToken,
                    SyntaxKind.GreaterThanGreaterThanEqualsToken,
                    SyntaxKind.QuestionToken
                    t = SyntaxFactory.MissingPunctuation(kind)

                Case SyntaxKind.FloatingLiteralToken
                    t = SyntaxFactory.FloatingLiteralToken("", TypeCharacter.None, Nothing)

                Case SyntaxKind.DecimalLiteralToken
                    t = SyntaxFactory.DecimalLiteralToken("", TypeCharacter.None, Nothing)

                Case SyntaxKind.DateLiteralToken
                    t = SyntaxFactory.DateLiteralToken("", Nothing)

                Case SyntaxKind.XmlNameToken
                    t = SyntaxFactory.XmlNameToken("", SyntaxKind.XmlNameToken)

                Case SyntaxKind.XmlTextLiteralToken
                    t = SyntaxFactory.XmlTextLiteralToken("", "")

                Case SyntaxKind.SlashGreaterThanToken,
                    SyntaxKind.LessThanSlashToken,
                    SyntaxKind.LessThanExclamationMinusMinusToken,
                    SyntaxKind.MinusMinusGreaterThanToken,
                    SyntaxKind.LessThanQuestionToken,
                    SyntaxKind.QuestionGreaterThanToken,
                    SyntaxKind.LessThanPercentEqualsToken,
                    SyntaxKind.PercentGreaterThanToken,
                    SyntaxKind.BeginCDataToken,
                    SyntaxKind.EndCDataToken
                    t = SyntaxFactory.MissingPunctuation(kind)

                Case SyntaxKind.IdentifierToken
                    t = SyntaxFactory.MissingIdentifier()

                Case SyntaxKind.IntegerLiteralToken
                    t = MissingIntegerLiteralToken()

                Case SyntaxKind.StringLiteralToken
                    t = SyntaxFactory.MissingStringLiteral()

                Case SyntaxKind.CharacterLiteralToken
                    t = SyntaxFactory.MissingCharacterLiteralToken()

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
            Return t
        End Function

        Public Shared Function BadToken(text As String) As SyntaxToken
            Return BadToken(Nothing, text, Nothing)
        End Function

        Public Shared Function BadToken(leadingTrivia As SyntaxTriviaList, text As String, trailingTrivia As SyntaxTriviaList) As SyntaxToken
            If text Is Nothing Then
                Throw New ArgumentException(NameOf(text))
            End If
            Return New SyntaxToken(Nothing, New InternalSyntax.BadTokenSyntax(SyntaxKind.BadToken, InternalSyntax.SyntaxSubKind.None, Nothing, Nothing, text,
                    leadingTrivia.Node, trailingTrivia.Node), 0, 0)
        End Function

#End Region

#Region "TriviaFactories"

        Public Shared Function Trivia(node As StructuredTriviaSyntax) As SyntaxTrivia
            Return New SyntaxTrivia(Nothing, node.Green, position:=0, index:=0)
        End Function

#End Region

#Region "ListFactories"

        ''' <summary>
        ''' Creates an empty list of syntax nodes.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        Public Shared Function List(Of TNode As SyntaxNode)() As SyntaxList(Of TNode)
            Return New SyntaxList(Of TNode)
        End Function

        ''' <summary>
        ''' Creates a singleton list of syntax nodes.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="node">The single element node.</param>
        Public Shared Function SingletonList(Of TNode As SyntaxNode)(node As TNode) As SyntaxList(Of TNode)
            Return New SyntaxList(Of TNode)(node)
        End Function

        ''' <summary>
        ''' Creates a list of syntax nodes.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="nodes">A sequence of element nodes.</param>
        Public Shared Function List(Of TNode As SyntaxNode)(nodes As IEnumerable(Of TNode)) As SyntaxList(Of TNode)
            Return New SyntaxList(Of TNode)(nodes)
        End Function

        ''' <summary>
        ''' Creates an empty list of tokens.
        ''' </summary>
        Public Shared Function TokenList() As SyntaxTokenList
            Return New SyntaxTokenList()
        End Function

        ''' <summary>
        ''' Creates a singleton list of tokens.
        ''' </summary>
        ''' <param name="token">The single token.</param>
        Public Shared Function TokenList(token As SyntaxToken) As SyntaxTokenList
            Return New SyntaxTokenList(token)
        End Function

        ''' <summary>
        ''' Creates a list of tokens.
        ''' </summary>
        ''' <param name="tokens">An array of tokens.</param>
        Public Shared Function TokenList(ParamArray tokens As SyntaxToken()) As SyntaxTokenList
            Return New SyntaxTokenList(tokens)
        End Function

        ''' <summary>
        ''' Creates a list of tokens.
        ''' </summary>
        ''' <param name="tokens"></param>
        Public Shared Function TokenList(tokens As IEnumerable(Of SyntaxToken)) As SyntaxTokenList
            Return New SyntaxTokenList(tokens)
        End Function

        ''' <summary>
        ''' Creates an empty list of trivia.
        ''' </summary>
        Public Shared Function TriviaList() As SyntaxTriviaList
            Return New SyntaxTriviaList()
        End Function

        ''' <summary>
        ''' Creates a singleton list of trivia.
        ''' </summary>
        ''' <param name="trivia">A single trivia.</param>
        Public Shared Function TriviaList(trivia As SyntaxTrivia) As SyntaxTriviaList
            Return New SyntaxTriviaList(Nothing, trivia.UnderlyingNode)
        End Function

        ''' <summary>
        ''' Creates a list of trivia.
        ''' </summary>
        ''' <param name="trivias">An array of trivia.</param>
        Public Shared Function TriviaList(ParamArray trivias As SyntaxTrivia()) As SyntaxTriviaList
            Return New SyntaxTriviaList(trivias)
        End Function

        ''' <summary>
        ''' Creates a list of trivia.
        ''' </summary>
        ''' <param name="trivias">A sequence of trivia.</param>
        Public Shared Function TriviaList(trivias As IEnumerable(Of SyntaxTrivia)) As SyntaxTriviaList
            Return New SyntaxTriviaList(trivias)
        End Function

        ''' <summary>
        ''' Creates an empty separated list.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        Public Shared Function SeparatedList(Of TNode As SyntaxNode)() As SeparatedSyntaxList(Of TNode)
            Return New SeparatedSyntaxList(Of TNode)
        End Function

        ''' <summary>
        ''' Creates a singleton separated list.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="node">A single node.</param>
        Public Shared Function SingletonSeparatedList(Of TNode As SyntaxNode)(node As TNode) As SeparatedSyntaxList(Of TNode)
            Return New SeparatedSyntaxList(Of TNode)(node, 0)
        End Function

        ''' <summary>
        ''' Creates a separated list of nodes from a sequence of nodes, synthesizing comma separators in between.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="nodes">A sequence of syntax nodes.</param>
        Public Shared Function SeparatedList(Of TNode As SyntaxNode)(nodes As IEnumerable(Of TNode)) As SeparatedSyntaxList(Of TNode)
            If nodes Is Nothing Then Return Nothing

            Dim collection = TryCast(nodes, ICollection(Of TNode))

            If collection IsNot Nothing AndAlso collection.Count = 0 Then Return Nothing

            Using enumerator = nodes.GetEnumerator()
                If Not enumerator.MoveNext() Then Return Nothing

                Dim firstNode = enumerator.Current

                If Not enumerator.MoveNext() Then Return SingletonSeparatedList(firstNode)

                Dim builder As New SeparatedSyntaxListBuilder(Of TNode)(If(collection IsNot Nothing, (collection.Count * 2) - 1, 3))

                builder.Add(firstNode)

                Dim commaToken = Token(SyntaxKind.CommaToken)

                Do
                    builder.AddSeparator(commaToken)
                    builder.Add(enumerator.Current)
                Loop While enumerator.MoveNext()

                Return builder.ToList()
            End Using
        End Function

        ''' <summary>
        ''' Creates a separated list of nodes from a sequence of nodes and a sequence of separator tokens.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="nodes">A sequence of syntax nodes.</param>
        ''' <param name="separators">A sequence of token to be interleaved between the nodes. The number of tokens must
        ''' be one less than the number of nodes.</param>
        Public Shared Function SeparatedList(Of TNode As SyntaxNode)(nodes As IEnumerable(Of TNode), separators As IEnumerable(Of SyntaxToken)) As SeparatedSyntaxList(Of TNode)

            If nodes IsNot Nothing Then

                Dim nodeEnum = nodes.GetEnumerator
                Dim builder = SeparatedSyntaxListBuilder(Of TNode).Create()

                If separators IsNot Nothing Then

                    For Each separator In separators

                        ' The number of nodes must be equal to or one greater than the number of separators
                        If nodeEnum.MoveNext() Then
                            builder.Add(nodeEnum.Current)
                        Else
                            Throw New ArgumentException()
                        End If
                        builder.AddSeparator(separator)

                    Next

                End If

                ' Check that there is zero or one node left in the enumerator
                If nodeEnum.MoveNext() Then
                    builder.Add(nodeEnum.Current)

                    If nodeEnum.MoveNext() Then
                        Throw New ArgumentException()
                    End If
                End If

                Return builder.ToList()

            ElseIf separators Is Nothing Then
                ' Both are nothing so return empty list
                Return New SeparatedSyntaxList(Of TNode)
            Else
                ' No nodes but have separators.  This is an argument error.
                Throw New ArgumentException()
            End If

        End Function

        ''' <summary>
        ''' Creates a separated list from a sequence of nodes or tokens.
        ''' The sequence must start with a node and alternate between nodes and separator tokens.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="nodesAndTokens">A alternating sequence of nodes and tokens.</param>
        Public Shared Function SeparatedList(Of TNode As SyntaxNode)(nodesAndTokens As IEnumerable(Of SyntaxNodeOrToken)) As SeparatedSyntaxList(Of TNode)
            Return SeparatedList(Of TNode)(NodeOrTokenList(nodesAndTokens))
        End Function

        ''' <summary>
        ''' Creates a separated list from a <see cref="SyntaxNodeOrTokenList"/>.
        ''' The <see cref="SyntaxNodeOrTokenList"/> must start with a node and alternate between nodes and separator tokens.
        ''' </summary>
        ''' <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        ''' <param name="nodesAndTokens">An alternating list of nodes and tokens.</param>
        Public Shared Function SeparatedList(Of TNode As SyntaxNode)(nodesAndTokens As SyntaxNodeOrTokenList) As SeparatedSyntaxList(Of TNode)
            If Not HasSeparatedNodeTokenPattern(nodesAndTokens) Then
                Throw New ArgumentException(CodeAnalysisResources.NodeOrTokenOutOfSequence)
            End If

            If Not NodesAreCorrectType(Of TNode)(nodesAndTokens) Then
                Throw New ArgumentException(CodeAnalysisResources.UnexpectedTypeOfNodeInList)
            End If

            Return New SeparatedSyntaxList(Of TNode)(nodesAndTokens)
        End Function

        Private Shared Function NodesAreCorrectType(Of TNode)(list As SyntaxNodeOrTokenList) As Boolean
            Dim n = list.Count
            For i = 0 To n - 1
                Dim element = list(i)
                If element.IsNode AndAlso Not (TypeOf element.AsNode() Is TNode) Then
                    Return False
                End If
            Next
            Return True
        End Function

        Private Shared Function HasSeparatedNodeTokenPattern(list As SyntaxNodeOrTokenList) As Boolean
            For i = 0 To list.Count - 1
                Dim element = list(i)
                If element.IsToken = ((i And 1) = 0) Then
                    Return False
                End If
            Next
            Return True
        End Function

        ''' <summary>
        ''' Creates an empty <see cref="SyntaxNodeOrTokenList"/>.
        ''' </summary>
        Public Shared Function NodeOrTokenList() As SyntaxNodeOrTokenList
            Return Nothing
        End Function

        ''' <summary>
        ''' Creates a <see cref="SyntaxNodeOrTokenList"/> from a sequence of nodes and tokens.
        ''' </summary>
        ''' <param name="nodesAndTokens">A sequence of nodes and tokens.</param>
        Public Shared Function NodeOrTokenList(nodesAndTokens As IEnumerable(Of SyntaxNodeOrToken)) As SyntaxNodeOrTokenList
            Return New SyntaxNodeOrTokenList(nodesAndTokens)
        End Function

        ''' <summary>
        ''' Creates a <see cref="SyntaxNodeOrTokenList"/> from one or more nodes and tokens.
        ''' </summary>
        ''' <param name="nodesAndTokens">An array of nodes and tokens.</param>
        Public Shared Function NodeOrTokenList(ParamArray nodesAndTokens As SyntaxNodeOrToken()) As SyntaxNodeOrTokenList
            Return New SyntaxNodeOrTokenList(nodesAndTokens)
        End Function

#End Region

        Public Shared Function InvocationExpression(expression As ExpressionSyntax) As InvocationExpressionSyntax
            Return InvocationExpression(expression, Nothing)
        End Function

    End Class
End Namespace
