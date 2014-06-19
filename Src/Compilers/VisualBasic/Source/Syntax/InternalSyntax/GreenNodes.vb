
Imports System.Collections.ObjectModel
Imports System.Text
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract
Imports System.Threading


Namespace Roslyn.Compilers.VisualBasic.InternalSyntax

    <Flags()>
    Enum SyntaxFlags
        None = 0
        HasDiagnostics = &H1
        HasDirectives = &H2
        HasSkippedText = &H4
        NotMissing = &H8
        HasElasticTrivia = &H10
        All = HasDiagnostics Or HasDirectives Or NotMissing
    End Enum

    <System.Diagnostics.DebuggerDisplay("{DebugString}")>
    Partial Friend Class SyntaxNode
        Implements IBaseSyntaxNodeExt

        ' TODO: since we do not reuse nodes with errors this could be a table on the root node.
        Private Shared ReadOnly _errors As New System.Runtime.CompilerServices.ConditionalWeakTable(Of SyntaxNode, DiagnosticInfo())

        Private _attributes As Integer  ' combines Kind and Flags
        Private _fullWidth As Integer

        Friend ReadOnly Property Flags() As SyntaxFlags
            Get
                Return CType(_attributes >> 16, SyntaxFlags)
            End Get
        End Property

        Friend ReadOnly Property Kind As SyntaxKind
            Get
                Return CType(CShort(_attributes), SyntaxKind)
            End Get
        End Property

        Friend Sub SetFlags(ByVal flags As SyntaxFlags)
            Me._attributes = Me._attributes Or (flags << 16)
        End Sub

        Friend Sub ClearFlags(ByVal flags As SyntaxFlags)
            Me._attributes = Me._attributes And Not (flags << 16)
        End Sub

        Friend Sub Init(ByVal node As SyntaxNode)
            Me._attributes = _attributes Or (node._attributes And (SyntaxFlags.All << 16))
            Me._fullWidth += node.FullWidth
        End Sub

        Friend Sub Init(ByVal node As SyntaxNode, ByVal mask As SyntaxFlags)
            Me._attributes = _attributes Or (node._attributes And (mask << 16))
            Me._fullWidth += node.FullWidth
        End Sub

        Friend ReadOnly Property HasDirectives As Boolean
            Get
                Return (Flags And SyntaxFlags.HasDirectives) <> 0
            End Get
        End Property

        ''' <summary>
        ''' The width of this item, in characters. Includes any associated trivia (whitespace/comments).
        ''' </summary>
        Friend ReadOnly Property FullWidth As Integer
            Get
                Return _fullWidth
            End Get
        End Property

        Friend ReadOnly Property HasChildren As Boolean Implements IBaseSyntaxNodeExt.HasChildren
            Get
                Return SlotCount > 0
            End Get
        End Property

        Friend ReadOnly Property HasDiagnostics As Boolean Implements IBaseSyntaxNodeExt.HasDiagnostics
            Get
                Return (Flags And SyntaxFlags.HasDiagnostics) <> 0
            End Get
        End Property

        Friend ReadOnly Property HasSkippedText As Boolean Implements IBaseSyntaxNodeExt.HasSkippedText
            Get
                Return (Flags And SyntaxFlags.HasSkippedText) <> 0
            End Get
        End Property

        Friend ReadOnly Property IsList() As Boolean
            Get
                Return Kind = SyntaxKind.List
            End Get
        End Property

        Friend ReadOnly Property IsElastic As Boolean
            Get
                Return Me.HasElasticTrivia AndAlso (Me.Kind = SyntaxKind.WhitespaceTrivia OrElse Me.Kind = SyntaxKind.EndOfLineTrivia)
            End Get
        End Property

        Friend ReadOnly Property HasElasticTrivia As Boolean
            Get
                Return (Flags And SyntaxFlags.HasElasticTrivia) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Append the full text of this node including children and trivia to the given stringbuilder.
        ''' </summary>
        Friend Overridable Sub WriteTo(ByVal stringBuilder As StringBuilder)
            For i = 0 To SlotCount() - 1
                Dim green = GetSlot(i)
                If green IsNot Nothing Then
                    green.WriteTo(stringBuilder)
                End If
            Next
        End Sub

        ' The rest of this class is just a convenient place to put some helper functions that are shared by the 
        ' various subclasses.

        Friend Function GetFullText() As String
            Dim stringBuilder As New StringBuilder()
            Me.WriteTo(stringBuilder)
            Return stringBuilder.ToString()
        End Function


        ' The errors associated with this node only. Set at creation time, can never change.
        Friend Function GetDiagnostics() As DiagnosticInfo()
            If Me.HasDiagnostics Then
                Dim errArr As DiagnosticInfo() = Nothing
                If _errors.TryGetValue(Me, errArr) Then
                    Return errArr
                End If
            End If
            Return Nothing
        End Function

        Friend ReadOnly Property Width As Integer
            Get
                Return FullWidth - GetLeadingTriviaWidth() - GetTrailingTriviaWidth()
            End Get
        End Property

        Friend Function GetText() As String
            Return GetFullText.Substring(GetLeadingTriviaWidth, Width)
        End Function

        Friend Function IsStructuredTrivia() As Boolean
            Return TypeOf Me Is StructuredTriviaSyntax
        End Function

        Friend Function IsDirective() As Boolean
            Return TypeOf Me Is DirectiveSyntax
        End Function

        ' Get the count of child nodes.
        Friend MustOverride ReadOnly Property SlotCount() As Integer

        ' Get a particular child.
        Friend MustOverride Function GetSlot(ByVal index As Integer) As SyntaxNode

        ' Create a new red node referencing this node. This is done by calling the stored "_createNode" delegate,
        ' so that a red node of the correct type is created.
        Friend Overridable Function CreateRed(ByVal parent As Roslyn.Compilers.VisualBasic.SyntaxNode, ByVal startLocation As Integer) As Roslyn.Compilers.VisualBasic.SyntaxNode
            Throw New InvalidOperationException("Tokens and Trivia cannot create red nodes")
        End Function


        ' Get the leading trivia a green array, recursively to first token.
        Friend Overridable Function GetLeadingTrivia() As SyntaxNode
            Dim possibleFirstChild = GetFirstToken(True)
            If possibleFirstChild IsNot Nothing Then
                Return possibleFirstChild.GetLeadingTrivia()
            Else
                Return Nothing
            End If
        End Function

        ' And the width of that leading trivia
        Friend Overridable Function GetLeadingTriviaWidth() As Integer
            Dim possibleFirstChild = GetFirstToken(True)
            If possibleFirstChild IsNot Nothing Then
                Return possibleFirstChild.GetLeadingTriviaWidth()
            Else
                Return 0
            End If
        End Function

        ' Get the trailing trivia a green array, recursively to first token.
        Friend Overridable Function GetTrailingTrivia() As SyntaxNode
            Dim possibleLastChild = GetLastToken(True)
            If possibleLastChild IsNot Nothing Then
                Return possibleLastChild.GetTrailingTrivia()
            Else
                Return Nothing
            End If
        End Function

        ' And the width of that trailing trivia
        Friend Overridable Function GetTrailingTriviaWidth() As Integer
            Dim possibleLastChild = GetLastToken(True)
            If possibleLastChild IsNot Nothing Then
                Return possibleLastChild.GetTrailingTriviaWidth()
            Else
                Return 0
            End If
        End Function


        ' Create a new green node, replacing the error list ON THIS NODE ONLY with the passed
        ' in error list
        Friend MustOverride Function SetDiagnostics(ByVal newErrors As DiagnosticInfo()) As InternalSyntax.SyntaxNode

        Protected Sub New(ByVal kind As SyntaxKind)
            Me._attributes = kind
        End Sub

        Protected Sub New(ByVal kind As SyntaxKind, ByVal width As Integer)
            Me.New(kind)
            Me._fullWidth = width
        End Sub

        Protected Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo())
            Me.new(kind)
            If errors IsNot Nothing AndAlso errors.Length > 0 Then
                SetFlags(SyntaxFlags.HasDiagnostics)
                _errors.Add(Me, errors)
            End If
        End Sub

        Protected Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal width As Integer)
            Me.New(kind, errors)
            Me._fullWidth = width
        End Sub

        Friend Function IsMissing() As Boolean
            Return (Me.Flags And SyntaxFlags.NotMissing) = 0
        End Function

        Friend Overridable ReadOnly Property IsToken As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Add an error to the given node, creating a new node that is the same except it has no parent,
        ''' and has the given error attached to it. The error span is the entire span of this node.
        ''' </summary>
        ''' <param name="err">The error to attach to this node</param>
        ''' <returns>A new node, with no parent, that has this error added to it.</returns>
        ''' <remarks>Since nodes are immutable, the only way to create nodes with errors attached is to create a node without an error,
        ''' then add an error with this method to create another node.</remarks>
        Friend Function AddError(ByVal err As DiagnosticInfo) As SyntaxNode
            Ensures(Contract.Result(Of SyntaxNode)() IsNot Nothing)

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
            Return DirectCast(SetDiagnostics(errorInfos), SyntaxNode)
        End Function

        ''' <summary>
        ''' Get all syntax errors associated with this node, or any child nodes, grand-child nodes, etc. The errors
        ''' are not in order.
        ''' </summary>
        Friend Overridable Function GetSyntaxErrors() As System.Collections.Generic.IList(Of DiagnosticInfo)
            If Not HasDiagnostics Then
                Return Nothing
            End If

            Dim accumulatedErrors As New List(Of DiagnosticInfo)
            AddSyntaxErrors(accumulatedErrors)
            Return accumulatedErrors
        End Function


        Friend Overridable Sub AddSyntaxErrors(ByVal accumulatedErrors As List(Of DiagnosticInfo))
            If Me.GetDiagnostics IsNot Nothing Then
                accumulatedErrors.AddRange(Me.GetDiagnostics)
            End If

            Dim cnt = SlotCount()

            If cnt = 0 Then
                Return
            Else
                For i As Integer = 0 To cnt - 1
                    Dim child = GetSlot(i)
                    If child IsNot Nothing AndAlso child.HasDiagnostics Then
                        child.AddSyntaxErrors(accumulatedErrors)
                    End If
                Next
            End If
        End Sub

        Private ReadOnly Property DebugString As String
            Get
                Dim text = GetFullText()
                If text.Length > 400 Then
                    text = text.Substring(0, 400)
                End If
                Return Kind.ToString & ":" & text
            End Get
        End Property

        Friend Shared Function EquivalentTo(ByVal left As SyntaxNode, ByVal right As SyntaxNode) As Boolean
            If left Is right Then
                Return True
            End If

            If left Is Nothing OrElse right Is Nothing Then
                Return False
            End If

            Return left.EquivalentTo(right)
        End Function

        ' TODO: recursion?
        Friend Overridable Function EquivalentTo(ByVal other As SyntaxNode) As Boolean
            If Me Is other Then
                Return True
            End If

            If other Is Nothing Then
                Return False
            End If

            If Me.Kind <> other.Kind Then
                Return False
            End If

            If Me.FullWidth <> other.FullWidth Then
                Return False
            End If

            Dim n = Me.SlotCount

            If n <> other.SlotCount Then
                Return False
            End If

            For i = 0 To n - 1
                Dim thisChild = Me.GetSlot(i)
                Dim otherChild = other.GetSlot(i)

                If thisChild IsNot Nothing AndAlso
                    otherChild IsNot Nothing AndAlso
                    Not thisChild.EquivalentTo(otherChild) Then

                    Return False
                End If
            Next
            Return True
        End Function

        Friend Function GetChildOffset(ByVal index As Integer) As Integer
            Dim offset = 0

            For i = 0 To index - 1
                offset += GetChildFullWidth(i)
            Next
            Return offset
        End Function

        Friend Overridable Function GetChildFullWidth(ByVal index As Integer) As Integer
            Dim child = GetSlot(index)
            Return If(child Is Nothing, 0, child.FullWidth)
        End Function

        Friend Overridable Function HasLeadingTrivia() As Boolean
            Dim arr = Me.GetLeadingTrivia
            Return arr IsNot Nothing
        End Function

        Friend Overridable Function HasTrailingTrivia() As Boolean
            Dim arr = Me.GetTrailingTrivia
            Return arr IsNot Nothing
        End Function

        Friend ReadOnly Property Children As ChildList
            Get
                Return New ChildList(Me)
            End Get
        End Property

        Friend Function GetFirstToken(Optional ByVal includeZeroWidthTokens As Boolean = False) As SyntaxToken
            Dim node = Me
tryAgain:
            Dim i As Integer = 0
            Dim n As Integer = node.SlotCount
            Do While (i < n)
                Dim child As SyntaxNode = node.GetSlot(i)
                If child IsNot Nothing Then
                    Dim token As SyntaxToken = TryCast(child, SyntaxToken)
                    If token IsNot Nothing Then
                        If includeZeroWidthTokens OrElse token.FullWidth > 0 Then
                            Return token
                        End If
                    Else
                        node = child
                        GoTo tryAgain
                    End If
                End If
                i += 1
            Loop
            Return Nothing
        End Function


        Friend Function GetLastToken(Optional ByVal includeZeroWidthTokens As Boolean = False) As SyntaxToken
            Dim node = Me
tryAgain:
            Dim i As Integer = node.SlotCount - 1
            Do While (i >= 0)
                Dim child As SyntaxNode = node.GetSlot(i)
                If (Not child Is Nothing) Then
                    Dim token As SyntaxToken = TryCast(child, SyntaxToken)
                    If token IsNot Nothing Then
                        If includeZeroWidthTokens OrElse token.FullWidth > 0 Then
                            Return token
                        End If
                    Else
                        node = child
                        GoTo tryAgain
                    End If
                End If
                i -= 1
            Loop
            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            Return Me.GetFullText
        End Function

#Region "IBaseSyntaxNode"
        Private ReadOnly Property BaseSyntaxKind As Integer Implements IBaseSyntaxNode.Kind
            Get
                Return Kind
            End Get
        End Property

        Public ReadOnly Property IsMissing1 As Boolean Implements IBaseSyntaxNode.IsMissing
            Get
                Return IsMissing()
            End Get
        End Property

        Private ReadOnly Property BaseSyntaxWidth As Integer Implements IBaseSyntaxNodeExt.Width
            Get
                Return Width
            End Get
        End Property

        Private ReadOnly Property BaseSyntaxFullWidth As Integer Implements IBaseSyntaxNodeExt.FullWidth
            Get
                Return FullWidth
            End Get
        End Property

        Private ReadOnly Property BaseSyntaxIsList As Boolean Implements IBaseSyntaxNodeExt.IsList
            Get
                Return IsList
            End Get
        End Property

        Private ReadOnly Property BaseSyntaxIsElastic As Boolean Implements IBaseSyntaxNodeExt.IsElastic
            Get
                Return IsElastic
            End Get
        End Property

        Private ReadOnly Property BaseSyntaxHasElasticTrivia As Boolean Implements IBaseSyntaxNodeExt.HasElasticTrivia
            Get
                Return HasElasticTrivia
            End Get
        End Property

        Private ReadOnly Property BaseSyntaxSlotCount As Integer Implements IBaseSyntaxNodeExt.SlotCount
            Get
                Return SlotCount
            End Get
        End Property

        Private Function BaseSyntaxGetSlot(ByVal index As Integer) As IBaseSyntaxNodeExt Implements IBaseSyntaxNodeExt.GetSlot
            Return GetSlot(index)
        End Function

        Private Function BaseSyntaxGetSlotOFfset(ByVal index As Integer) As Integer Implements IBaseSyntaxNodeExt.GetSlotOffset
            Return GetChildOffset(index)
        End Function

        Public ReadOnly Property BaseIsStructuredTrivia As Boolean Implements IBaseSyntaxNodeExt.IsStructuredTrivia
            Get
                Return IsStructuredTrivia()
            End Get
        End Property

        Public ReadOnly Property BaseIsDirective As Boolean Implements IBaseSyntaxNodeExt.IsDirective
            Get
                Return IsDirective()
            End Get
        End Property

        Private Function GetStructure(ByVal Parent As CommonSyntaxNode, ByVal position As Integer) As CommonSyntaxNode Implements IBaseSyntaxNodeExt.GetStructure
            Return DirectCast(Me, SyntaxNode).CreateRed(DirectCast(Parent, Roslyn.Compilers.VisualBasic.SyntaxNode), position)
        End Function

        Private ReadOnly Property BaseLeadingWidth As Integer Implements IBaseSyntaxNodeExt.LeadingWidth
            Get
                Return GetLeadingTriviaWidth()
            End Get
        End Property

        Private ReadOnly Property BaseTrailingWidth As Integer Implements IBaseSyntaxNodeExt.TrailingWidth
            Get
                Return GetTrailingTriviaWidth()
            End Get
        End Property

        Private ReadOnly Property Children1 As CommonChildSyntaxList Implements IBaseSyntaxNode.Children
            Get
                Return New CommonChildSyntaxList(Me)
            End Get
        End Property

        Private Function EquivalentTo1(ByVal other As IBaseSyntaxNode) As Boolean Implements IBaseSyntaxNode.EquivalentTo
            Dim otherNode As SyntaxNode = TryCast(other, SyntaxNode)
            If otherNode IsNot Nothing Then
                Return EquivalentTo(otherNode)
            Else
                Return False
            End If
        End Function

        Private ReadOnly Property HasDirectives1 As Boolean Implements IBaseSyntaxNode.HasDirectives
            Get
                Return HasDirectives
            End Get
        End Property

        Private Function ToFullText() As String Implements IBaseSyntaxNode.GetFullText
            Return Me.GetFullText
        End Function

        Private Function ToText() As String Implements IBaseSyntaxNode.GetText
            Return Me.GetText
        End Function

        Private Function GetUnderlyingGreenNode() As IBaseSyntaxNodeExt Implements IBaseSyntaxNodeExt.GetUnderlyingGreenNode
            ' As the green node, we are already the underlying node
            Return Me
        End Function

#End Region

    End Class

    Friend Enum SyntaxSubKind
        None
        BeginDocTypeToken
        LessThanExclamationToken
        OpenBracketToken
        CloseBracketToken
    End Enum

    Partial Friend NotInheritable Class BadTokenSyntax
        Inherits PunctuationSyntax

        Private ReadOnly _subKind As SyntaxSubKind

        Friend Sub New(ByVal kind As SyntaxKind, ByVal subKind As SyntaxSubKind, ByVal errors As DiagnosticInfo(), ByVal text As String, ByVal leadingTrivia As SyntaxNode, ByVal trailingTrivia As SyntaxNode)
            MyBase.New(kind, errors, text, leadingTrivia, trailingTrivia)

            _subKind = subKind
        End Sub

        Friend ReadOnly Property SubKind As SyntaxSubKind
            Get
                Return _subKind
            End Get
        End Property

        Friend Overrides Function WithLeadingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken
            Return New BadTokenSyntax(Kind, SubKind, GetDiagnostics, Text, trivia, GetTrailingTrivia)
        End Function

        Friend Overrides Function WithTrailingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken
            Return New BadTokenSyntax(Kind, SubKind, GetDiagnostics, Text, GetLeadingTrivia, trivia)
        End Function

        Friend Overrides Function SetDiagnostics(ByVal newErrors As DiagnosticInfo()) As SyntaxNode
            Return New BadTokenSyntax(Kind, SubKind, newErrors, Text, GetLeadingTrivia, GetTrailingTrivia)
        End Function
    End Class

    Partial Friend Class SyntaxToken
        Implements ISyntaxToken

        Private ReadOnly _text As String
        Private ReadOnly _trailingTriviaOrTriviaInfo As Object

        Private Class TriviaInfo
            Public Sub New(ByVal leadingTrivia As SyntaxNode, ByVal trailingTrivia As SyntaxNode)
                Me._leadingTrivia = leadingTrivia
                Me._trailingTrivia = trailingTrivia
            End Sub

            Public _leadingTrivia As SyntaxNode
            Public _trailingTrivia As SyntaxNode
        End Class

        Protected Sub New(ByVal kind As SyntaxKind, ByVal text As String, ByVal precedingTrivia As SyntaxNode, ByVal followingTrivia As SyntaxNode)
            MyBase.New(kind, text.Length)

            Me._text = text
            If followingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                Init(followingTrivia, SyntaxFlags.HasDiagnostics Or SyntaxFlags.HasDirectives)
                Me._trailingTriviaOrTriviaInfo = followingTrivia
            End If

            If precedingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                Init(precedingTrivia, SyntaxFlags.HasDiagnostics Or SyntaxFlags.HasDirectives)
                Me._trailingTriviaOrTriviaInfo = New TriviaInfo(precedingTrivia, DirectCast(Me._trailingTriviaOrTriviaInfo, SyntaxNode))
            End If

            If text.Length > 0 OrElse kind = SyntaxKind.EndOfFileToken OrElse kind = SyntaxKind.EmptyToken Then
                ' If a token has text then it is not missing.  The only 0 length tokens that are not considered missing are the end of file token because no text exists for this token 
                ' and the empty token which exists solely so that the empty statement has a token.
                Me.SetFlags(SyntaxFlags.NotMissing)
            End If
        End Sub

        Protected Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal text As String, ByVal precedingTrivia As SyntaxNode, ByVal followingTrivia As SyntaxNode)
            MyBase.New(kind, errors, text.Length)

            Me._text = text
            If followingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                Init(followingTrivia, SyntaxFlags.HasDiagnostics Or SyntaxFlags.HasDirectives)
                Me._trailingTriviaOrTriviaInfo = followingTrivia
            End If

            If precedingTrivia IsNot Nothing Then
                ' Don't propagate NoMissing in Init
                Init(precedingTrivia, SyntaxFlags.HasDiagnostics Or SyntaxFlags.HasDirectives)
                Me._trailingTriviaOrTriviaInfo = New TriviaInfo(precedingTrivia, DirectCast(Me._trailingTriviaOrTriviaInfo, SyntaxNode))
            End If

            If text.Length > 0 OrElse kind = SyntaxKind.EndOfFileToken OrElse kind = SyntaxKind.EmptyToken Then
                ' If a token has text then it is not missing.  The only 0 length tokens that are not considered missing are the end of file token because no text exists for this token 
                ' and the empty token which exists solely so that the empty statement has a token.
                Me.SetFlags(SyntaxFlags.NotMissing)
            End If
        End Sub

        Friend ReadOnly Property Text As String
            Get
                Return Me._text
            End Get
        End Property

        Friend Overrides Function GetSlot(ByVal index As Integer) As SyntaxNode
            Throw New IndexOutOfRangeException
        End Function

        Friend Overrides ReadOnly Property SlotCount As Integer
            Get
                Return 0
            End Get
        End Property

        ' Get the leading trivia as GreenNode array.
        Friend Overrides Function GetLeadingTrivia() As SyntaxNode
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
        Friend Overrides Function GetLeadingTriviaWidth() As Integer
            Return _leadingTriviaWidth()
        End Function

        ' Get the following trivia as GreenNode array.
        Friend Overrides Function GetTrailingTrivia() As SyntaxNode
            Dim arr = TryCast(_trailingTriviaOrTriviaInfo, SyntaxNode)
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
        Friend Overrides Function GetTrailingTriviaWidth() As Integer
            Return FullWidth - _text.Length - _leadingTriviaWidth()
        End Function

        Friend Overrides Sub AddSyntaxErrors(ByVal accumulatedErrors As List(Of DiagnosticInfo))
            If Me.GetDiagnostics IsNot Nothing Then
                accumulatedErrors.AddRange(Me.GetDiagnostics)
            End If

            Dim leadingTrivia = GetLeadingTrivia()
            If leadingTrivia IsNot Nothing Then
                Dim triviaList = New SyntaxList(Of SyntaxNode)(leadingTrivia)
                For i = 0 To triviaList.Count - 1
                    triviaList.ItemUntyped(i).AddSyntaxErrors(accumulatedErrors)
                Next
            End If
            Dim trailingTrivia = GetTrailingTrivia()
            If trailingTrivia IsNot Nothing Then
                Dim triviaList = New SyntaxList(Of SyntaxNode)(trailingTrivia)
                For i = 0 To triviaList.Count - 1
                    triviaList.ItemUntyped(i).AddSyntaxErrors(accumulatedErrors)
                Next
            End If
        End Sub

        ' Append full text of this token, including trivia, to a StringBuilder.
        Friend Overrides Sub WriteTo(ByVal stringBuilder As StringBuilder)
            Dim leadingTrivia = GetLeadingTrivia()
            If leadingTrivia IsNot Nothing Then
                leadingTrivia.WriteTo(stringBuilder) 'Append leading trivia
            End If

            stringBuilder.Append(_text) 'Append text of token itself

            Dim trailingTrivia = GetTrailingTrivia()
            If trailingTrivia IsNot Nothing Then
                trailingTrivia.WriteTo(stringBuilder) ' Append trailing trivia
            End If
        End Sub

        Friend MustOverride Function WithLeadingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken

        Friend MustOverride Function WithTrailingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken

        Friend Overrides Function Accept(Of TArgument)(ByVal visitor As SyntaxVisitor(Of TArgument), ByVal argument As TArgument) As SyntaxNode
            Return visitor.VisitSyntaxToken(Me, argument)
        End Function

#Region "ISyntaxToken"
        Private ReadOnly Property Text1 As String Implements ISyntaxToken.Text
            Get
                Return Me.GetText
            End Get
        End Property

        Private ReadOnly Property Value1 As Object Implements ISyntaxToken.Value
            Get
                Return Me.ObjectValue
            End Get
        End Property

        Private ReadOnly Property ValueText1 As String Implements ISyntaxToken.ValueText
            Get
                Return Me.ValueText
            End Get
        End Property

        Private Function GetLeadingTrivia1() As IBaseSyntaxNodeExt Implements ISyntaxToken.GetLeadingTrivia
            Return Me.GetLeadingTrivia
        End Function

        Private Function GetTrailingTrivia1() As IBaseSyntaxNodeExt Implements ISyntaxToken.GetTrailingTrivia
            Return GetTrailingTrivia()
        End Function

        Private Function GetNextToken(ByVal current As CommonSyntaxToken, ByVal predicate As Func(Of CommonSyntaxToken, Boolean)) As CommonSyntaxToken Implements ISyntaxToken.GetNextToken
            Return SyntaxNavigation.GetNextToken(CType(current, VisualBasic.SyntaxToken), predicate.ToLanguageSpecific(), Nothing)
        End Function

        Private Function GetPreviousToken(ByVal current As CommonSyntaxToken, ByVal predicate As Func(Of CommonSyntaxToken, Boolean)) As CommonSyntaxToken Implements ISyntaxToken.GetPreviousToken
            Return SyntaxNavigation.GetPreviousToken(CType(current, VisualBasic.SyntaxToken), predicate.ToLanguageSpecific(), Nothing)
        End Function

#End Region
    End Class

    Partial Friend Class SyntaxTrivia
        Inherits SyntaxNode

        Private ReadOnly _text As String

        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal text As String)
            MyBase.New(kind, errors, text.Length)
            Me._text = text
            If text.Length > 0 Then
                Me.SetFlags(SyntaxFlags.NotMissing)
            End If
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal text As String, ByVal elastic As Boolean)
            Me.New(kind, errors, text)

            System.Diagnostics.Debug.Assert(Not elastic OrElse (kind = SyntaxKind.WhitespaceTrivia OrElse kind = SyntaxKind.EndOfLineTrivia))
            If elastic Then
                Me.SetFlags(SyntaxFlags.HasElasticTrivia)
            End If
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, ByVal text As String)
            MyBase.New(kind, text.Length)
            Me._text = text
            If text.Length > 0 Then
                Me.SetFlags(SyntaxFlags.NotMissing)
            End If
        End Sub

        Friend Sub New(ByVal kind As SyntaxKind, ByVal text As String, ByVal elastic As Boolean)
            Me.New(kind, text)

            System.Diagnostics.Debug.Assert(Not elastic OrElse (kind = SyntaxKind.WhitespaceTrivia OrElse kind = SyntaxKind.EndOfLineTrivia))
            If elastic Then
                Me.SetFlags(SyntaxFlags.HasElasticTrivia)
            End If
        End Sub

        Friend ReadOnly Property Text As String
            Get
                Return Me._text
            End Get
        End Property


        Friend Overrides Function GetSlot(ByVal index As Integer) As SyntaxNode
            Throw New IndexOutOfRangeException
        End Function

        Friend Overrides ReadOnly Property SlotCount() As Integer
            Get
                Return 0
            End Get
        End Property

        Friend Overrides Function GetTrailingTrivia() As SyntaxNode
            Return Nothing
        End Function

        Friend Overrides Function GetTrailingTriviaWidth() As Integer
            Return 0
        End Function

        Friend Overrides Function GetLeadingTrivia() As SyntaxNode
            Return Nothing
        End Function

        Friend Overrides Function GetLeadingTriviaWidth() As Integer
            Return 0
        End Function

        ' Append full text of this token, including trivia, to a StringBuilder.
        Friend Overrides Sub WriteTo(ByVal stringBuilder As StringBuilder)
            stringBuilder.Append(Text) 'Append text of token itself
        End Sub


        Friend Overrides Sub AddSyntaxErrors(ByVal accumulatedErrors As List(Of DiagnosticInfo))
            If Me.GetDiagnostics IsNot Nothing Then
                accumulatedErrors.AddRange(Me.GetDiagnostics)
            End If
        End Sub

        Friend Overrides Function Accept(Of TArgument)(ByVal visitor As SyntaxVisitor(Of TArgument), ByVal argument As TArgument) As SyntaxNode
            Return visitor.VisitSyntaxTrivia(Me, argument)
        End Function
    End Class

    Partial Friend Class DocumentationCommentSyntax

        Friend Function GetInteriorXml() As String
            Dim sb As New StringBuilder
            WriteInteriorXml(Me, sb)
            Return sb.ToString
        End Function

        Private Shared Sub WriteInteriorXml(ByVal node As SyntaxNode, ByVal sb As StringBuilder)
            If node Is Nothing Then
                Return
            End If

            Dim childCnt = node.SlotCount
            If childCnt > 0 Then
                For i = 0 To childCnt - 1
                    Dim child = node.GetSlot(i)
                    WriteInteriorXml(child, sb)
                Next
            Else
                Dim tk = DirectCast(node, SyntaxToken)
                WriteInteriorXml(New SyntaxList(Of SyntaxNode)(tk.GetLeadingTrivia), sb)
                WriteInteriorXml(tk, sb)
                WriteInteriorXml(New SyntaxList(Of SyntaxNode)(tk.GetTrailingTrivia), sb)
            End If
        End Sub

        Private Shared Sub WriteInteriorXml(ByVal node As SyntaxToken, ByVal sb As StringBuilder)
            sb.Append(node.Text)
        End Sub

        Private Shared Sub WriteInteriorXml(ByVal node As SyntaxList(Of SyntaxNode), ByVal sb As StringBuilder)
            For i = 0 To node.Count - 1
                Dim t = node(i)
                If t.Kind <> SyntaxKind.DocumentationCommentExteriorTrivia Then
                    sb.Append(t.GetText)
                End If
            Next
        End Sub

    End Class


    ''' <summary>
    ''' Represents an identifier token. This might include brackets around the name,
    ''' and a type character.
    ''' </summary>
    Friend MustInherit Class IdentifierTokenSyntax
        Inherits SyntaxToken

        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal text As String, ByVal precedingTrivia As SyntaxNode, ByVal followingTrivia As SyntaxNode)
            MyBase.New(kind, errors, text, precedingTrivia, followingTrivia)
        End Sub

        ''' <summary>
        ''' Contextual Nodekind
        ''' </summary>
        Friend MustOverride ReadOnly Property PossibleKeywordKind As SyntaxKind

        ''' <summary>
        ''' If true, the identifier was enclosed in brackets, such as "[End]".
        ''' </summary>
        Friend MustOverride ReadOnly Property IsBracketed As Boolean

        ''' <summary>
        ''' The text of the identifier, not including the brackets or type character.
        ''' </summary>
        Friend MustOverride ReadOnly Property IdentifierText As String

        ' TODO: do we need IdentifierText?
        Friend Overrides ReadOnly Property ValueText As String
            Get
                Return IdentifierText
            End Get
        End Property

        ''' <summary>
        ''' The type character suffix, if present. Returns TypeCharacter.None if no type
        ''' character was present. The only allowed values are None, Integer, Long,
        ''' Decimal, Single, Double, and String.
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeCharacter As TypeCharacter
    End Class

    Friend NotInheritable Class SimpleIdentifierSyntax
        Inherits IdentifierTokenSyntax

        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal text As String, ByVal precedingTrivia As SyntaxNode, ByVal followingTrivia As SyntaxNode)
            MyBase.New(kind, errors, text, precedingTrivia, followingTrivia)
        End Sub

        ''' <summary>
        ''' Contextual Nodekind
        ''' </summary>
        Friend Overrides ReadOnly Property PossibleKeywordKind As SyntaxKind
            Get
                Return SyntaxKind.IdentifierToken
            End Get
        End Property

        ''' <summary>
        ''' If true, the identifier was enclosed in brackets, such as "[End]".
        ''' </summary>
        Friend Overrides ReadOnly Property IsBracketed As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' The text of the identifier, not including the brackets or type character.
        ''' </summary>
        Friend Overrides ReadOnly Property IdentifierText As String
            Get
                Return Me.Text
            End Get
        End Property

        ''' <summary>
        ''' The type character suffix, if present. Returns TypeCharacter.None if no type
        ''' character was present. The only allowed values are None, Integer, Long,
        ''' Decimal, Single, Double, and String.
        ''' </summary>
        Friend Overrides ReadOnly Property TypeCharacter As TypeCharacter
            Get
                Return TypeCharacter.None
            End Get
        End Property

        Friend Overrides Function WithLeadingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken
            Return New SimpleIdentifierSyntax(Kind, GetDiagnostics, Text, trivia, GetTrailingTrivia)
        End Function

        Friend Overrides Function WithTrailingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken
            Return New SimpleIdentifierSyntax(Kind, GetDiagnostics, Text, GetLeadingTrivia, trivia)
        End Function

        Friend Overrides Function SetDiagnostics(ByVal newErrors As DiagnosticInfo()) As SyntaxNode
            Return New SimpleIdentifierSyntax(Kind, newErrors, Text, GetLeadingTrivia, GetTrailingTrivia)
        End Function

    End Class

    Friend NotInheritable Class ComplexIdentifierSyntax
        Inherits IdentifierTokenSyntax

        Private ReadOnly _possibleKeywordKind As SyntaxKind
        Private ReadOnly _isBracketed As Boolean
        Private ReadOnly _identifierText As String
        Private ReadOnly _typeCharacter As TypeCharacter


        Friend Sub New(ByVal kind As SyntaxKind, ByVal errors As DiagnosticInfo(), ByVal text As String, ByVal precedingTrivia As SyntaxNode, ByVal followingTrivia As SyntaxNode, ByVal possibleKeywordKind As SyntaxKind, ByVal isBracketed As Boolean, ByVal identifierText As String, ByVal typeCharacter As TypeCharacter)
            MyBase.New(kind, errors, text, precedingTrivia, followingTrivia)

            Me._possibleKeywordKind = possibleKeywordKind
            Me._isBracketed = isBracketed
            Me._identifierText = identifierText
            Me._typeCharacter = typeCharacter

        End Sub

        ''' <summary>
        ''' Contextual Nodekind
        ''' </summary>
        Friend Overrides ReadOnly Property PossibleKeywordKind As SyntaxKind
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

        Friend Overrides Function WithLeadingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken
            Return New ComplexIdentifierSyntax(Kind, GetDiagnostics, Text, trivia, GetTrailingTrivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function

        Friend Overrides Function WithTrailingTrivia(ByVal trivia As SyntaxNode) As SyntaxToken
            Return New ComplexIdentifierSyntax(Kind, GetDiagnostics, Text, GetLeadingTrivia, trivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function

        Friend Overrides Function SetDiagnostics(ByVal newErrors As DiagnosticInfo()) As SyntaxNode
            Return New ComplexIdentifierSyntax(Kind, newErrors, Text, GetLeadingTrivia, GetTrailingTrivia, PossibleKeywordKind, IsBracketed, IdentifierText, TypeCharacter)
        End Function
    End Class

End Namespace
