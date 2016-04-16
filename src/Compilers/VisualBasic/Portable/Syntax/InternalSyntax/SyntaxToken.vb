' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class SyntaxToken
        Inherits VisualBasicSyntaxNode

        Private ReadOnly _text As String
        Private ReadOnly _trailingTriviaOrTriviaInfo As Object

        Friend Class TriviaInfo
            Implements IObjectWritable, IObjectReadable

            Private Sub New(leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode)
                Me._leadingTrivia = leadingTrivia
                Me._trailingTrivia = trailingTrivia
            End Sub

            Private Const s_maximumCachedTriviaWidth As Integer = 40
            Private Const s_triviaInfoCacheSize As Integer = 64

            Private Shared ReadOnly s_triviaKeyHasher As Func(Of VisualBasicSyntaxNode, Integer) =
            Function(key) Hash.Combine(key.ToFullString(), CShort(key.Kind))

            Private Shared ReadOnly s_triviaKeyEquality As Func(Of VisualBasicSyntaxNode, TriviaInfo, Boolean) =
            Function(key, value) (key Is value._leadingTrivia) OrElse ((key.Kind = value._leadingTrivia.Kind) AndAlso (key.FullWidth = value._leadingTrivia.FullWidth) AndAlso (key.ToFullString() = value._leadingTrivia.ToFullString()))

            Private Shared ReadOnly s_triviaInfoCache As CachingFactory(Of VisualBasicSyntaxNode, TriviaInfo) = New CachingFactory(Of VisualBasicSyntaxNode, TriviaInfo)(s_triviaInfoCacheSize, Nothing, s_triviaKeyHasher, s_triviaKeyEquality)

            Private Shared Function ShouldCacheTriviaInfo(leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode) As Boolean
                Debug.Assert(leadingTrivia IsNot Nothing)

                If trailingTrivia Is Nothing Then

                    ' Leading doc comment (which may include whitespace). No trailing trivia.
                    Return leadingTrivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia AndAlso leadingTrivia.Flags = NodeFlags.IsNotMissing AndAlso
                        leadingTrivia.FullWidth <= s_maximumCachedTriviaWidth

                Else

                    ' Leading whitespace and a trailing single space.
                    Return leadingTrivia.Kind = SyntaxKind.WhitespaceTrivia AndAlso leadingTrivia.Flags = NodeFlags.IsNotMissing AndAlso
                         trailingTrivia.Kind = SyntaxKind.WhitespaceTrivia AndAlso trailingTrivia.Flags = NodeFlags.IsNotMissing AndAlso
                         trailingTrivia.FullWidth = 1 AndAlso trailingTrivia.ToFullString() = " " AndAlso
                         leadingTrivia.FullWidth <= s_maximumCachedTriviaWidth

                End If

            End Function

            Public Shared Function Create(leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode) As TriviaInfo
                Debug.Assert(leadingTrivia IsNot Nothing)

                ' PERF: Cache common kinds of TriviaInfo
                If ShouldCacheTriviaInfo(leadingTrivia, trailingTrivia) Then
                    Dim retVal As TriviaInfo = Nothing
                    SyncLock s_triviaInfoCache
                        ' Note: Only the leading trivia is considered as a key into the cache. That works because
                        ' we cache only a couple of different cases. In one case, the trailing trivia is Nothing
                        ' and in the other it's a single space, and the two cases can be distinguished just by
                        ' examining the Kind on the leading trivia. If we ever decide to cache more kinds of trivia
                        ' this may have to be revisited.
                        If s_triviaInfoCache.TryGetValue(leadingTrivia, retVal) Then
                            Debug.Assert(trailingTrivia Is Nothing OrElse retVal._trailingTrivia.IsEquivalentTo(trailingTrivia))
                            Debug.Assert(retVal._leadingTrivia.IsEquivalentTo(leadingTrivia))
                        Else
                            retVal = New TriviaInfo(leadingTrivia, trailingTrivia)
                            s_triviaInfoCache.Add(leadingTrivia, retVal)
                        End If
                    End SyncLock

                    Return retVal
                End If

                Return New TriviaInfo(leadingTrivia, trailingTrivia)
            End Function

            Public _leadingTrivia As VisualBasicSyntaxNode
            Public _trailingTrivia As VisualBasicSyntaxNode

            Public Sub New(reader As ObjectReader)
                Me._leadingTrivia = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
                Me._trailingTrivia = DirectCast(reader.ReadValue(), VisualBasicSyntaxNode)
            End Sub

            Public Function GetReader() As Func(Of ObjectReader, Object) Implements IObjectReadable.GetReader
                Return Function(r) New TriviaInfo(r)
            End Function

            Public Sub WriteTo(writer As ObjectWriter) Implements IObjectWritable.WriteTo
                writer.WriteValue(_leadingTrivia)
                writer.WriteValue(_trailingTrivia)
            End Sub
        End Class

        Protected Sub New(kind As SyntaxKind, text As String, precedingTrivia As VisualBasicSyntaxNode, followingTrivia As VisualBasicSyntaxNode)
            MyBase.New(kind, text.Length)
            Me.SetFlags(NodeFlags.IsNotMissing)

            Me._text = text
            If followingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(followingTrivia)
                Me._trailingTriviaOrTriviaInfo = followingTrivia
            End If

            If precedingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(precedingTrivia)
                Me._trailingTriviaOrTriviaInfo = TriviaInfo.Create(precedingTrivia, DirectCast(Me._trailingTriviaOrTriviaInfo, VisualBasicSyntaxNode))
            End If

            ClearFlagIfMissing()
        End Sub

        Protected Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), text As String, precedingTrivia As VisualBasicSyntaxNode, followingTrivia As VisualBasicSyntaxNode)
            MyBase.New(kind, errors, text.Length)
            Me.SetFlags(NodeFlags.IsNotMissing)

            Me._text = text
            If followingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(followingTrivia)
                Me._trailingTriviaOrTriviaInfo = followingTrivia
            End If

            If precedingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(precedingTrivia)
                Me._trailingTriviaOrTriviaInfo = TriviaInfo.Create(precedingTrivia, DirectCast(Me._trailingTriviaOrTriviaInfo, VisualBasicSyntaxNode))
            End If

            ClearFlagIfMissing()
        End Sub

        Protected Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, precedingTrivia As VisualBasicSyntaxNode, followingTrivia As VisualBasicSyntaxNode)
            MyBase.New(kind, errors, annotations, text.Length)
            Me.SetFlags(NodeFlags.IsNotMissing)

            Me._text = text
            If followingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(followingTrivia)
                Me._trailingTriviaOrTriviaInfo = followingTrivia
            End If

            If precedingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(precedingTrivia)
                Me._trailingTriviaOrTriviaInfo = TriviaInfo.Create(precedingTrivia, DirectCast(Me._trailingTriviaOrTriviaInfo, VisualBasicSyntaxNode))
            End If

            ClearFlagIfMissing()
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
            Me.SetFlags(NodeFlags.IsNotMissing)

            Me._text = reader.ReadString()
            Me.FullWidth = Me._text.Length

            Me._trailingTriviaOrTriviaInfo = reader.ReadValue()

            Dim info = TryCast(Me._trailingTriviaOrTriviaInfo, TriviaInfo)

            Dim followingTrivia = If(info IsNot Nothing, info._trailingTrivia, TryCast(Me._trailingTriviaOrTriviaInfo, VisualBasicSyntaxNode))
            Dim precedingTrivia = If(info IsNot Nothing, info._leadingTrivia, Nothing)

            If followingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(followingTrivia)
            End If

            If precedingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                AdjustFlagsAndWidth(precedingTrivia)
            End If

            ClearFlagIfMissing()
        End Sub

        Private Sub ClearFlagIfMissing()
            If Text.Length = 0 AndAlso Kind <> SyntaxKind.EndOfFileToken AndAlso Kind <> SyntaxKind.EmptyToken Then
                ' If a token has text then it is not missing.  The only 0 length tokens that are not considered missing are the end of file token because no text exists for this token 
                ' and the empty token which exists solely so that the empty statement has a token.
                Me.ClearFlags(NodeFlags.IsNotMissing)
            End If
        End Sub

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteString(Me._text)
            writer.WriteValue(Me._trailingTriviaOrTriviaInfo)
        End Sub

        Friend ReadOnly Property Text As String
            Get
                Return Me._text
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSlot(index As Integer) As GreenNode
            Throw ExceptionUtilities.Unreachable
        End Function

        ' Get the leading trivia as GreenNode array.
        Friend NotOverridable Overrides Function GetLeadingTrivia() As VisualBasicSyntaxNode
            Dim t = TryCast(_trailingTriviaOrTriviaInfo, TriviaInfo)
            If t IsNot Nothing Then
                Return t._leadingTrivia
            End If
            Return Nothing
        End Function

        Private ReadOnly Property _leadingTriviaWidth() As Integer
            Get
                Dim t = TryCast(_trailingTriviaOrTriviaInfo, TriviaInfo)
                If t IsNot Nothing Then
                    Return t._leadingTrivia.FullWidth
                End If
                Return 0
            End Get
        End Property

        ' Get the width of the leading trivia
        Public NotOverridable Overrides Function GetLeadingTriviaWidth() As Integer
            Return _leadingTriviaWidth()
        End Function

        ' Get the following trivia as GreenNode array.
        Friend NotOverridable Overrides Function GetTrailingTrivia() As VisualBasicSyntaxNode
            Dim arr = TryCast(_trailingTriviaOrTriviaInfo, VisualBasicSyntaxNode)
            If arr IsNot Nothing Then
                Return arr
            End If
            Dim t = TryCast(_trailingTriviaOrTriviaInfo, TriviaInfo)
            If t IsNot Nothing Then
                Return t._trailingTrivia
            End If
            Return Nothing
        End Function

        ' Get the width of the following trivia
        Public NotOverridable Overrides Function GetTrailingTriviaWidth() As Integer
            Return FullWidth - _text.Length - _leadingTriviaWidth()
        End Function

        Friend NotOverridable Overrides Sub AddSyntaxErrors(accumulatedErrors As List(Of DiagnosticInfo))
            If Me.GetDiagnostics IsNot Nothing Then
                accumulatedErrors.AddRange(Me.GetDiagnostics)
            End If

            Dim leadingTrivia = GetLeadingTrivia()
            If leadingTrivia IsNot Nothing Then
                Dim triviaList = New SyntaxList(Of VisualBasicSyntaxNode)(leadingTrivia)
                For i = 0 To triviaList.Count - 1
                    DirectCast(triviaList.ItemUntyped(i), VisualBasicSyntaxNode).AddSyntaxErrors(accumulatedErrors)
                Next
            End If
            Dim trailingTrivia = GetTrailingTrivia()
            If trailingTrivia IsNot Nothing Then
                Dim triviaList = New SyntaxList(Of VisualBasicSyntaxNode)(trailingTrivia)
                For i = 0 To triviaList.Count - 1
                    DirectCast(triviaList.ItemUntyped(i), VisualBasicSyntaxNode).AddSyntaxErrors(accumulatedErrors)
                Next
            End If
        End Sub

        Friend Overrides Sub WriteToOrFlatten(writer As IO.TextWriter, stack As ArrayBuilder(Of GreenNode))

            Dim leadingTrivia = GetLeadingTrivia()
            If leadingTrivia IsNot Nothing Then
                leadingTrivia.WriteTo(writer) 'Append leading trivia
            End If

            writer.Write(_text) 'Append text of token itself

            Dim trailingTrivia = GetTrailingTrivia()
            If trailingTrivia IsNot Nothing Then
                trailingTrivia.WriteTo(writer) ' Append trailing trivia
            End If
        End Sub

        ''' <summary>
        ''' Add this token to the token list builder.
        ''' </summary>
        Friend NotOverridable Overrides Sub CollectConstituentTokensAndDiagnostics(tokenListBuilder As SyntaxListBuilder(Of SyntaxToken),
                                                                    nonTokenDiagnostics As IList(Of DiagnosticInfo))
            tokenListBuilder.Add(Me)
        End Sub

        Public NotOverridable Overrides Function Accept(visitor As VisualBasicSyntaxVisitor) As VisualBasicSyntaxNode
            Return visitor.VisitSyntaxToken(Me)
        End Function

        Public Overrides Function ToString() As String
            Return Me._text
        End Function

        Public NotOverridable Overrides ReadOnly Property IsToken As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Helper to check whether the token is a keyword
        ''' </summary>
        Friend Overridable ReadOnly Property IsKeyword As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overridable ReadOnly Property ObjectValue As Object
            Get
                Return ValueText
            End Get
        End Property

        Public Overrides Function GetValue() As Object
            Return Me.ObjectValue
        End Function

        Friend Overridable ReadOnly Property ValueText As String
            Get
                Return Text
            End Get
        End Property

        Public Overrides Function GetValueText() As String
            Return Me.ValueText
        End Function

        ''' <summary>
        ''' Helpers to check whether the token is a binary operator
        ''' </summary>
        ''' <returns>True if it is a binary operator</returns>
        Public Function IsBinaryOperator() As Boolean
            Select Case Kind
                Case SyntaxKind.MinusToken,
                    SyntaxKind.PlusToken,
                    SyntaxKind.AsteriskToken,
                    SyntaxKind.SlashToken,
                    SyntaxKind.BackslashToken,
                    SyntaxKind.CaretToken,
                    SyntaxKind.AmpersandToken,
                    SyntaxKind.LessThanLessThanToken,
                    SyntaxKind.GreaterThanGreaterThanToken,
                    SyntaxKind.ModKeyword,
                    SyntaxKind.OrKeyword,
                    SyntaxKind.OrElseKeyword,
                    SyntaxKind.XorKeyword,
                    SyntaxKind.AndKeyword,
                    SyntaxKind.AndAlsoKeyword,
                    SyntaxKind.LikeKeyword,
                    SyntaxKind.EqualsToken,
                    SyntaxKind.LessThanGreaterThanToken,
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanEqualsToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.GreaterThanEqualsToken,
                    SyntaxKind.IsKeyword,
                    SyntaxKind.IsNotKeyword
                    Return True
            End Select

            Return False
        End Function

        ''' <summary>
        ''' Check whether the token is a statement terminator
        ''' </summary>
        ''' <returns>True if it is statement terminator</returns>
        Friend ReadOnly Property IsEndOfLine As Boolean
            Get
                Return Kind = SyntaxKind.StatementTerminatorToken OrElse Kind = SyntaxKind.EndOfFileToken
            End Get
        End Property

        ''' <summary>
        ''' Check whether token is end of text
        ''' </summary>
        Friend ReadOnly Property IsEndOfParse As Boolean
            Get
                Return Kind = SyntaxKind.EndOfFileToken
            End Get
        End Property

        ''' <summary>
        ''' Create a new token with the trivia prepended to the existing preceding trivia
        ''' </summary>
        Public Shared Function AddLeadingTrivia(Of T As SyntaxToken)(token As T, newTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As T
            Debug.Assert(token IsNot Nothing)

            If newTrivia.Node Is Nothing Then
                Return token
            End If

            Dim oldTrivia = New SyntaxList(Of VisualBasicSyntaxNode)(token.GetLeadingTrivia())
            Dim leadingTrivia As VisualBasicSyntaxNode

            If oldTrivia.Node Is Nothing Then
                leadingTrivia = newTrivia.Node
            Else
                Dim leadingTriviaBuilder = SyntaxListBuilder(Of VisualBasicSyntaxNode).Create()
                leadingTriviaBuilder.AddRange(newTrivia)

                leadingTriviaBuilder.AddRange(oldTrivia)
                leadingTrivia = leadingTriviaBuilder.ToList.Node
            End If

            Return DirectCast(token.WithLeadingTrivia(leadingTrivia), T)
        End Function

        ''' <summary>
        ''' Create a new token with the trivia appended to the existing following trivia
        ''' </summary>
        Public Shared Function AddTrailingTrivia(Of T As SyntaxToken)(token As T, newTrivia As SyntaxList(Of VisualBasicSyntaxNode)) As T
            Debug.Assert(token IsNot Nothing)

            If newTrivia.Node Is Nothing Then
                Return token
            End If

            Dim oldTrivia = New SyntaxList(Of VisualBasicSyntaxNode)(token.GetTrailingTrivia())
            Dim trailingTrivia As VisualBasicSyntaxNode

            If oldTrivia.Node Is Nothing Then
                trailingTrivia = newTrivia.Node
            Else
                Dim trailingTriviaBuilder = SyntaxListBuilder(Of VisualBasicSyntaxNode).Create()
                trailingTriviaBuilder.AddRange(oldTrivia)

                trailingTriviaBuilder.AddRange(newTrivia)
                trailingTrivia = trailingTriviaBuilder.ToList.Node
            End If

            Return DirectCast(token.WithTrailingTrivia(trailingTrivia), T)
        End Function

        Friend Shared Function Create(kind As SyntaxKind, Optional leading As VisualBasicSyntaxNode = Nothing, Optional trailing As VisualBasicSyntaxNode = Nothing, Optional text As String = Nothing) As SyntaxToken

            ' use default token text if text is nothing. If it's empty or anything else, use the given one
            Dim tokenText = If(text Is Nothing, SyntaxFacts.GetText(kind), text)

            If CInt(kind) >= SyntaxKind.AddHandlerKeyword Then

                If CInt(kind) <= SyntaxKind.YieldKeyword OrElse kind = SyntaxKind.NameOfKeyword Then
                    Return New KeywordSyntax(kind, tokenText, leading, trailing)
                ElseIf CInt(kind) <= SyntaxKind.EndOfXmlToken OrElse
                       kind = SyntaxKind.EndOfInterpolatedStringToken OrElse
                       kind = SyntaxKind.DollarSignDoubleQuoteToken _
                Then
                    Return New PunctuationSyntax(kind, tokenText, leading, trailing)
                End If
            End If

            Throw ExceptionUtilities.UnexpectedValue(kind)
        End Function

        Public Shared Narrowing Operator CType(token As SyntaxToken) As Microsoft.CodeAnalysis.SyntaxToken
            Return New Microsoft.CodeAnalysis.SyntaxToken(Nothing, token, position:=0, index:=0)
        End Operator

        Public Overrides Function IsEquivalentTo(other As GreenNode) As Boolean
            If Not MyBase.IsEquivalentTo(other) Then
                Return False
            End If

            Dim otherToken = DirectCast(other, SyntaxToken)

            If Not String.Equals(Me.Text, otherToken.Text, StringComparison.Ordinal) Then
                Return False
            End If

            If Me.HasLeadingTrivia <> otherToken.HasLeadingTrivia OrElse
               Me.HasTrailingTrivia <> otherToken.HasTrailingTrivia Then
                Return False
            End If

            If Me.HasLeadingTrivia AndAlso Not Me.GetLeadingTrivia().IsEquivalentTo(otherToken.GetLeadingTrivia()) Then
                Return False
            End If

            If Me.HasTrailingTrivia AndAlso Not Me.GetTrailingTrivia().IsEquivalentTo(otherToken.GetTrailingTrivia()) Then
                Return False
            End If

            Return True
        End Function

        Friend Overrides Function CreateRed(parent As SyntaxNode, position As Integer) As SyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class

    Friend Partial Class XmlTextTokenSyntax
        Friend NotOverridable Overrides ReadOnly Property ValueText As String
            Get
                Return Me.Value
            End Get
        End Property
    End Class

    Friend Partial Class InterpolatedStringTextTokenSyntax
        Friend NotOverridable Overrides ReadOnly Property ValueText As String
            Get
                Return Me.Value
            End Get
        End Property
    End Class

    Friend Partial Class KeywordSyntax

        Friend NotOverridable Overrides ReadOnly Property ObjectValue As Object
            Get
                Select Case MyBase.Kind
                    Case SyntaxKind.NothingKeyword
                        Return Nothing
                    Case SyntaxKind.TrueKeyword
                        Return Boxes.BoxedTrue
                    Case SyntaxKind.FalseKeyword
                        Return Boxes.BoxedFalse
                    Case Else
                        Return Me.Text
                End Select
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsKeyword As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace
