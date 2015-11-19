' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

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

        Friend Shared Function SetFactoryContext(flags As NodeFlags, context As ISyntaxFactoryContext) As NodeFlags
            If context.IsWithinAsyncMethodOrLambda Then
                flags = flags Or NodeFlags.FactoryContextIsInAsync
            End If

            If context.IsWithinIteratorContext Then
                flags = flags Or NodeFlags.FactoryContextIsInIterator
            End If

            Return flags
        End Function

        Friend Function MatchesFactoryContext(context As ISyntaxFactoryContext) As Boolean
            Return context.IsWithinAsyncMethodOrLambda = Me.ParsedInAsync AndAlso
                context.IsWithinIteratorContext = Me.ParsedInIterator
        End Function

        ''' <summary>
        ''' Append the full text of this node including children and trivia to the given stringbuilder.
        ''' </summary>
        Public Overrides Sub WriteTo(writer As IO.TextWriter)
            Dim stack = ArrayBuilder(Of GreenNode).GetInstance
            stack.Push(Me)

            While stack.Count > 0
                DirectCast(stack.Pop(), InternalSyntax.VisualBasicSyntaxNode).WriteToOrFlatten(writer, stack)
            End While

            stack.Free()
        End Sub

        Protected Overrides Sub WriteTo(writer As IO.TextWriter, leading As Boolean, trailing As Boolean)
            Me.WriteTo(writer)
        End Sub

        ''' <summary>
        ''' NOTE: the method should write OR push children, but never do both
        ''' </summary>
        Friend Overridable Sub WriteToOrFlatten(writer As IO.TextWriter, stack As ArrayBuilder(Of GreenNode))
            ' By default just push children to the stack
            For i = Me.SlotCount() - 1 To 0 Step -1
                Dim node As GreenNode = GetSlot(i)
                If node IsNot Nothing Then
                    stack.Push(GetSlot(i))
                End If
            Next
        End Sub

#Region "Serialization"
        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
        End Sub
#End Region

        ''' <summary>
        ''' Add all the tokens in this node and children to the build token list builder. While doing this, add any
        ''' diagnostics not on tokens to the given diagnostic info list.
        ''' </summary>
        Friend Overridable Sub CollectConstituentTokensAndDiagnostics(tokenListBuilder As SyntaxListBuilder(Of SyntaxToken),
                                                                      nonTokenDiagnostics As IList(Of DiagnosticInfo))
            ' This implementation is overridden for tokens; this is the implementation for non-token nodes.

            ' Add diagnostics.
            Dim diagnostics As DiagnosticInfo() = Me.GetDiagnostics()
            If diagnostics IsNot Nothing AndAlso diagnostics.Length > 0 Then
                For Each diag In diagnostics
                    nonTokenDiagnostics.Add(diag)
                Next
            End If

            ' Recurse to subtrees.
            For i = 0 To SlotCount() - 1
                Dim green = GetSlot(i)
                If green IsNot Nothing Then
                    DirectCast(green, VisualBasicSyntaxNode).CollectConstituentTokensAndDiagnostics(tokenListBuilder, nonTokenDiagnostics)
                End If
            Next
        End Sub

        ' The rest of this class is just a convenient place to put some helper functions that are shared by the 
        ' various subclasses.

        ''' <summary>
        ''' Returns the string representation of this node, not including its leading and trailing trivia.
        ''' </summary>
        ''' <returns>The string representation of this node, not including its leading and trailing trivia.</returns>
        ''' <remarks>The length of the returned string is always the same as Span.Length</remarks>
        Public Overrides Function ToString() As String
            ' We get the full text into the string builder, and then only
            ' grab the part that doesn't contain the preceding and trailing trivia.

            Dim builder = Collections.PooledStringBuilder.GetInstance()
            Dim writer As New IO.StringWriter(builder, System.Globalization.CultureInfo.InvariantCulture)

            WriteTo(writer)

            Dim leadingWidth = GetLeadingTriviaWidth()
            Dim trailingWidth = GetTrailingTriviaWidth()

            Debug.Assert(FullWidth = builder.Length)
            Debug.Assert(FullWidth >= leadingWidth + trailingWidth)

            Return builder.ToStringAndFree(leadingWidth, FullWidth - leadingWidth - trailingWidth)
        End Function

        ''' <summary>
        ''' Returns full string representation of this node including its leading and trailing trivia.
        ''' </summary>
        ''' <returns>The full string representation of this node including its leading and trailing trivia.</returns>
        ''' <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        Public Overrides Function ToFullString() As String
            Dim builder = Collections.PooledStringBuilder.GetInstance()
            Dim writer As New IO.StringWriter(builder, System.Globalization.CultureInfo.InvariantCulture)

            WriteTo(writer)

            Return builder.ToStringAndFree()
        End Function

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

        Protected Overrides Function GetSlotCount() As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Protected Property _slotCount As Integer
            Get
                Return Me.SlotCount
            End Get

            Set(value As Integer)
                Me.SlotCount = value
            End Set
        End Property

        Friend Function GetFirstToken() As SyntaxToken
            Return DirectCast(Me.GetFirstTerminal(), SyntaxToken)
        End Function

        Friend Function GetLastToken() As SyntaxToken
            Return DirectCast(Me.GetLastTerminal(), SyntaxToken)
        End Function

        ' Get the leading trivia a green array, recursively to first token.
        Friend Overridable Function GetLeadingTrivia() As VisualBasicSyntaxNode
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
        Friend Overridable Function GetTrailingTrivia() As VisualBasicSyntaxNode
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
        ''' Add an error to the given node, creating a new node that is the same except it has no parent,
        ''' and has the given error attached to it. The error span is the entire span of this node.
        ''' </summary>
        ''' <param name="err">The error to attach to this node</param>
        ''' <returns>A new node, with no parent, that has this error added to it.</returns>
        ''' <remarks>Since nodes are immutable, the only way to create nodes with errors attached is to create a node without an error,
        ''' then add an error with this method to create another node.</remarks>
        Friend Function AddError(err As DiagnosticInfo) As VisualBasicSyntaxNode
            Dim errorInfos() As DiagnosticInfo

            ' If the green node already has errors, add those on.
            If GetDiagnostics() Is Nothing Then
                errorInfos = {err}
            Else
                ' Add the error to the error list.
                errorInfos = GetDiagnostics()
                Dim length As Integer = errorInfos.Length
                ReDim Preserve errorInfos(length)
                errorInfos(length) = err
            End If

            ' Get a new green node with the errors added on.
            Return DirectCast(SetDiagnostics(errorInfos), VisualBasicSyntaxNode)
        End Function

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

        Public Overrides Function IsEquivalentTo(other As GreenNode) As Boolean
            If Me Is other Then
                Return True
            End If

            If other Is Nothing Then
                Return False
            End If

            Return EquivalentToInternal(Me, other)
        End Function

        Private Shared Function EquivalentToInternal(node1 As GreenNode, node2 As GreenNode) As Boolean
            If node1.RawKind <> node2.RawKind Then
                ' A single-element list is usually represented as just a single node,
                ' but can be represented as a List node with one child. Move to that
                ' child if necessary.
                If node1.IsList AndAlso node1.SlotCount = 1 Then
                    node1 = node1.GetSlot(0)
                End If
                If node2.IsList AndAlso node2.SlotCount = 1 Then
                    node2 = node2.GetSlot(0)
                End If

                If node1.RawKind <> node2.RawKind Then
                    Return False
                End If
            End If

            If node1.FullWidth <> node2.FullWidth Then
                Return False
            End If

            Dim n = node1.SlotCount

            If n <> node2.SlotCount Then
                Return False
            End If

            For i = 0 To n - 1
                Dim node1Child = node1.GetSlot(i)
                Dim node2Child = node2.GetSlot(i)

                If node1Child IsNot Nothing AndAlso
                   node2Child IsNot Nothing AndAlso
                   Not node1Child.IsEquivalentTo(node2Child) Then

                    Return False
                End If
            Next

            Return True
        End Function

        Public Overrides Function GetSlotOffset(index As Integer) As Integer
            ' This implementation should not support arbitrary
            ' length lists since the implementation is O(n).
            Debug.Assert(index < 12) ' Max. slots 12 (DeclareStatement)

            Dim offset = 0

            For i = 0 To index - 1
                Dim child = GetSlot(i)
                If child IsNot Nothing Then
                    offset += child.FullWidth
                End If
            Next
            Return offset
        End Function

        Friend Function ChildNodesAndTokens() As ChildSyntaxList
            Return New ChildSyntaxList(Me)
        End Function

        ' Use conditional weak table so we always return same identity for structured trivia
        Private Shared ReadOnly s_structuresTable As New ConditionalWeakTable(Of SyntaxNode, Dictionary(Of Microsoft.CodeAnalysis.SyntaxTrivia, SyntaxNode))

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

        Public Overrides ReadOnly Property Navigator As AbstractSyntaxNavigator
            Get
                Return SyntaxNavigator.Instance
            End Get
        End Property

        Public Overrides Function CreateList(nodes As IEnumerable(Of GreenNode), Optional alwaysCreateListNode As Boolean = False) As GreenNode
            If nodes Is Nothing Then
                Return Nothing
            End If

            Dim list = nodes.Select(Function(n) DirectCast(n, InternalSyntax.VisualBasicSyntaxNode)).ToArray()

            Dim count = list.Length
            Select Case count
                Case 0
                    Return Nothing
                Case 1
                    If alwaysCreateListNode Then
                        Return SyntaxList.List(list)
                    Else
                        Return list(0)
                    End If
                Case 2
                    Return SyntaxList.List(list(0), list(1))
                Case 3
                    Return SyntaxList.List(list(0), list(1), list(2))
                Case Else
                    Return SyntaxList.List(list)
            End Select
        End Function

        Public Overrides Function CreateSeparator(Of TNode As SyntaxNode)(element As SyntaxNode) As CodeAnalysis.SyntaxToken
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
