' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Partial Friend Class VisualBasicSyntaxNode
        Inherits GreenNode

        Friend ReadOnly Property Kind As SyntaxKind
            Get
                Return CType(Me.RawKind, SyntaxKind)
            End Get
        End Property

        Friend ReadOnly Property ContextualKind As SyntaxKind
            Get
                Return CType(Me.RawContextualKind, SyntaxKind)
            End Get
        End Property

        Public Overrides ReadOnly Property KindText As String
            Get
                Return Me.Kind.ToString()
            End Get
        End Property

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        ''' <summary>
        ''' Should only be called during construction.
        ''' </summary>
        ''' <remarks>
        ''' This should probably be an extra constructor parameter, but we don't need more constructor overloads.
        ''' </remarks>
        Protected Sub SetFactoryContext(context As ISyntaxFactoryContext)
            If context.IsWithinAsyncMethodOrLambda Then
                SetFlags(NodeFlags.FactoryContextIsInAsync)
            End If

            If context.IsWithinIteratorContext Then
                SetFlags(NodeFlags.FactoryContextIsInIterator)
            End If
        End Sub

        Friend Function MatchesFactoryContext(context As ISyntaxFactoryContext) As Boolean
            Return context.IsWithinAsyncMethodOrLambda = Me.ParsedInAsync AndAlso
                context.IsWithinIteratorContext = Me.ParsedInIterator
        End Function

        ' The rest of this class is just a convenient place to put some helper functions that are shared by the 
        ' various subclasses.

        Public Overrides ReadOnly Property IsStructuredTrivia As Boolean
            Get
                Return TypeOf Me Is StructuredTriviaSyntax
            End Get
        End Property

        Public Overrides ReadOnly Property IsDirective As Boolean
            Get
                Return TypeOf Me Is DirectiveTriviaSyntax
            End Get
        End Property

        Public Overrides ReadOnly Property IsSkippedTokensTrivia As Boolean
            Get
                Return Me.Kind = SyntaxKind.SkippedTokensTrivia
            End Get
        End Property

        Public Overrides ReadOnly Property IsDocumentationCommentTrivia As Boolean
            Get
                Return Me.Kind = SyntaxKind.DocumentationCommentTrivia
            End Get
        End Property

        Friend Function GetFirstToken() As SyntaxToken
            Return DirectCast(Me.GetFirstTerminal(), SyntaxToken)
        End Function

        Friend Function GetLastToken() As SyntaxToken
            Return DirectCast(Me.GetLastTerminal(), SyntaxToken)
        End Function

        ' Get the leading trivia a green array, recursively to first token.
        Friend Overridable Function GetLeadingTrivia() As GreenNode
            Dim possibleFirstChild = GetFirstToken()
            If possibleFirstChild IsNot Nothing Then
                Return possibleFirstChild.GetLeadingTrivia()
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function GetLeadingTriviaCore() As GreenNode
            Return Me.GetLeadingTrivia()
        End Function

        ' Get the trailing trivia a green array, recursively to first token.
        Friend Overridable Function GetTrailingTrivia() As GreenNode
            Dim possibleLastChild = GetLastToken()
            If possibleLastChild IsNot Nothing Then
                Return possibleLastChild.GetTrailingTrivia()
            Else
                Return Nothing
            End If
        End Function

        Public Overrides Function GetTrailingTriviaCore() As GreenNode
            Return Me.GetTrailingTrivia()
        End Function

        Protected Sub New(kind As SyntaxKind)
            MyBase.New(CType(kind, UInt16))
            GreenStats.NoteGreen(Me)
        End Sub

        Protected Sub New(kind As SyntaxKind, width As Integer)
            MyBase.New(CType(kind, UInt16), width)
            GreenStats.NoteGreen(Me)
        End Sub

        Protected Sub New(kind As SyntaxKind, errors As DiagnosticInfo())
            MyBase.New(CType(kind, UInt16), errors)
            GreenStats.NoteGreen(Me)
        End Sub

        Protected Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), width As Integer)
            MyBase.New(CType(kind, UInt16), errors, width)
            GreenStats.NoteGreen(Me)
        End Sub

        Friend Sub New(kind As SyntaxKind, diagnostics As DiagnosticInfo(), annotations As SyntaxAnnotation())
            MyBase.New(CType(kind, UInt16), diagnostics, annotations)
            GreenStats.NoteGreen(Me)
        End Sub

        Friend Sub New(kind As SyntaxKind, diagnostics As DiagnosticInfo(), annotations As SyntaxAnnotation(), fullWidth As Integer)
            MyBase.New(CType(kind, UInt16), diagnostics, annotations, fullWidth)
            GreenStats.NoteGreen(Me)
        End Sub

        ''' <summary>
        ''' Get all syntax errors associated with this node, or any child nodes, grand-child nodes, etc. The errors
        ''' are not in order.
        ''' </summary>
        Friend Overridable Function GetSyntaxErrors() As IList(Of DiagnosticInfo)
            If Not ContainsDiagnostics Then
                Return Nothing
            End If

            Dim accumulatedErrors As New List(Of DiagnosticInfo)
            AddSyntaxErrors(accumulatedErrors)
            Return accumulatedErrors
        End Function

        Friend Overridable Sub AddSyntaxErrors(accumulatedErrors As List(Of DiagnosticInfo))
            If Me.GetDiagnostics IsNot Nothing Then
                accumulatedErrors.AddRange(Me.GetDiagnostics)
            End If

            Dim cnt = SlotCount()

            If cnt = 0 Then
                Return
            Else
                For i As Integer = 0 To cnt - 1
                    Dim child = GetSlot(i)
                    If child IsNot Nothing AndAlso child.ContainsDiagnostics Then
                        DirectCast(child, VisualBasicSyntaxNode).AddSyntaxErrors(accumulatedErrors)
                    End If
                Next
            End If
        End Sub

        Private Function GetDebuggerDisplay() As String
            Dim text = ToFullString()
            If text.Length > 400 Then
                text = text.Substring(0, 400)
            End If
            Return Kind.ToString & ":" & text
        End Function

        Friend Overloads Shared Function IsEquivalentTo(left As VisualBasicSyntaxNode, right As VisualBasicSyntaxNode) As Boolean
            If left Is right Then
                Return True
            End If

            If left Is Nothing OrElse right Is Nothing Then
                Return False
            End If

            Return left.IsEquivalentTo(right)
        End Function

        ' Use conditional weak table so we always return same identity for structured trivia
        '
        ' As there are commonly few structured trivia per parent, use a SmallDictionary for
        ' mapping from trivia to StructuredTriviaSyntax. Testing against roslyn, of parents
        ' containing structured trivia:
        ' 81.2% contain 1 structured trivia
        ' 96.5% contain 2 or fewer structured trivia
        ' 99.6% contain 4 or fewer structured trivia
        ' 100% contain 7 or fewer structured trivia
        Private Shared ReadOnly s_structuresTable As New ConditionalWeakTable(Of SyntaxNode, SmallDictionary(Of Microsoft.CodeAnalysis.SyntaxTrivia, SyntaxNode))

        Public Overrides Function GetStructure(trivia As Microsoft.CodeAnalysis.SyntaxTrivia) As SyntaxNode
            If Not trivia.HasStructure Then
                Return Nothing
            End If

            Dim parent = trivia.Token.Parent
            If parent Is Nothing Then
                Return VisualBasic.Syntax.StructuredTriviaSyntax.Create(trivia)
            End If

            Dim [structure] As SyntaxNode = Nothing
            Dim structsInParent = s_structuresTable.GetOrCreateValue(parent)

            SyncLock structsInParent
                If Not structsInParent.TryGetValue(trivia, [structure]) Then
                    [structure] = VisualBasic.Syntax.StructuredTriviaSyntax.Create(trivia)
                    structsInParent.Add(trivia, [structure])
                End If
            End SyncLock

            Return [structure]
        End Function

        Public Overrides Function CreateSeparator(element As SyntaxNode) As CodeAnalysis.SyntaxToken
            Dim separatorKind As SyntaxKind = SyntaxKind.CommaToken
            If element.Kind = SyntaxKind.JoinCondition Then
                separatorKind = SyntaxKind.AndKeyword
            End If
            Return VisualBasic.SyntaxFactory.Token(separatorKind)
        End Function

        Public Overrides Function IsTriviaWithEndOfLine() As Boolean
            Return Me.Kind = SyntaxKind.EndOfLineTrivia OrElse Me.Kind = SyntaxKind.CommentTrivia
        End Function
    End Class
End Namespace
