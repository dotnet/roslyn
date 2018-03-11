Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Commands
Imports Roslyn.Services.Internal.Extensions
Imports Roslyn.Services.Internal.Utilities
Imports Roslyn.Services.VisualBasic.Commands
Imports Roslyn.Services.VisualBasic.Utilities
Imports System.Text
Imports Roslyn.Services.Workspaces
Imports Roslyn.Services.VisualBasic.Extensions

Namespace Roslyn.Services.VisualBasic.DocumentationComments
    <Export(GetType(ICommandHandler))>
    <Name(VisualBasicCommandHandlerNames.DocumentationComments)>
    <Order(After:=VisualBasicCommandHandlerNames.IntelliSense)>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    Friend NotInheritable Class DocumentationCommentCommandHandler
        Implements ICommandHandler(Of TypeCharCommandArgs)
        Implements ICommandHandler(Of ReturnKeyCommandArgs)
        Implements ICommandHandler(Of InsertCommentCommandArgs)

        Private ReadOnly _workspace As Workspace

        <ImportingConstructor()>
        Public Sub New(ByVal workspace As Workspace)
            Contract.ThrowIfNull(workspace)

            _workspace = workspace
        End Sub

        Public Function GetCommandState_InsertCommandCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of TypeCharCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_InsertCommandCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of InsertCommentCommandArgs).ExecuteCommand
            If Not InsertCommentOnContainingMember(args.TextView, args.SubjectBuffer) Then
                nextHandler()
            End If
        End Sub

        Public Function GetCommandState_TypeCharCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_TypeCharCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of TypeCharCommandArgs).ExecuteCommand
            ' Ensure the character is actually typed in the editor
            nextHandler()

            If args.TypedChar = "'"c Then
                InsertCommentAfterTripleApostrophes(args.TextView, args.SubjectBuffer)
            End If
        End Sub

        Public Function GetCommandState_ReturnKeyCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of InsertCommentCommandArgs).GetCommandState
            Return nextHandler()
        End Function

        Public Sub ExecuteCommand_ReturnKeyCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            ' There are three interesting cases when pressing ENTER in an XML doc comment.
            '
            '     1. If pressing enter at the end of an XML doc comment that contains only ''' we should generate
            '        the doc comment as if the user had typed '''. This situation can happen after an undo.
            '
            '     2. If pressing enter after the ''' of a single-line doc comment that has an incorrect target produces
            '        a doc comment that has a correct target, we should generate. This happens when
            '        typing '''' at the start of a method and pressing ENTER. E.g. |Sub M()
            '
            '    3. If pressing enter inside of an XML doc comment in any other situation, we should automatically insert
            '        a ''' at the appropriate indent on the next line.

            Dim subjectBufferCaretPosition = New SubjectBufferCaretPosition(args.TextView, args.SubjectBuffer)
            Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            If caretPosition < 0 Then
                nextHandler()
                Return
            End If

            Dim snapshot = args.SubjectBuffer.CurrentSnapshot
            Dim tree As SyntaxTree = Nothing
            If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
                nextHandler()
                Return
            End If

            ' Note that the doc comment span starts *after* the first exterior trivia
            Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            If documentationComment Is Nothing OrElse
                Not ExteriorTriviaStartsLine(tree, documentationComment, caretPosition) OrElse
                caretPosition < documentationComment.Span.Start Then

                nextHandler()
                Return
            End If

            If Not SpansSingleLine(documentationComment, snapshot) Then
                ' The documentation comment is valid, but does not meet the criteria for cases #1 or #2.
                ' So, it must be case #3
                InsertLineBreakAndTripleApostrophesAtCaret(subjectBufferCaretPosition)
                Return
            End If

            Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            If targetMember.SupportsDocumentationComments() AndAlso
                targetMember.Span.Start > documentationComment.Span.Start Then
                If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
                    ' Since there is text to the right, this must be cast #3 (e.g. /// <summary>|</summary>)
                    InsertLineBreakAndTripleApostrophesAtCaret(subjectBufferCaretPosition)
                Else
                    ' At this point, we know it's case #1
                    InsertCommentAfterTripleApostrophesCore(targetMember, tree, caretPosition, subjectBufferCaretPosition)
                End If

                Return
            End If

            ' Now, determine whether pressing ENTER produced a valid doc comment with an appropriate 
            ' target member. If so, it is case #2. Otherwise, it's case #3.

            ' Let the ENTER key pass through to the editor
            nextHandler()

            Dim postSnapshot = args.SubjectBuffer.CurrentSnapshot
            Dim postTree As SyntaxTree = Nothing
            If Not _workspace.TryGetSyntaxTree(postSnapshot, postTree) Then
                Return
            End If

            ' Note that we use the same caret position as we did before ENTER was passed to the editor.
            ' This should give us back the same doc comment.

            Dim postDocumentationComment = GetDocumentationComment(postTree, caretPosition)
            If postDocumentationComment Is Nothing Then
                Return
            End If

            If Not SpansSingleLine(postDocumentationComment, postSnapshot) OrElse
                Not IsExteriorTriviaLeftOfPosition(postDocumentationComment, caretPosition) Then
                Return
            End If

            Dim postTargetMember = GetDocumentationCommentTargetMember(postDocumentationComment)
            If Not postTargetMember.SupportsDocumentationComments() OrElse
                caretPosition > postTargetMember.Span.Start Then
                Return
            End If

            ' If we got here, it's case #2

            Dim indent = postSnapshot.GetLeadingWhitespaceOfLineAtPosition(caretPosition)
            Dim replaceSpan = Span.FromBounds(caretPosition, postTargetMember.GetFirstToken().Span.Start)

            Dim pair = GenerateDocumentationCommentText(
                postTargetMember, postTree, indent,
                prependExteriorTrivia:=False,
                appendLineBreakAndIndent:=True)

            ' Note that we replace the inserted line break and any indent that was added to ensure that
            ' everything lines up properly.

            ReplaceWithCommentText(
                replaceSpan,
                commentText:=pair.Item1,
                caretOffset:=pair.Item2,
                subjectBufferCaretPosition:=subjectBufferCaretPosition)
        End Sub

        Private Function InsertCommentAfterTripleApostrophes(ByVal textView As ITextView, ByVal subjectBuffer As ITextBuffer) As Boolean
            ' We generate a documentation comment when typing ''' under very specific circumstances:
            '     * The caret is immediately after the ''' of a documentation comment
            '     * After the caret, the line only contains only whitespace
            '     * The documentation comment only spans a single line
            '     * The documentation comment is attached to an appropriate member

            Dim subjectBufferCaretPosition As New subjectBufferCaretPosition(textView, subjectBuffer)

            Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            If caretPosition < 0 Then
                Return False
            End If

            Dim snapshot = subjectBuffer.CurrentSnapshot

            If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
                Return False
            End If

            Dim tree As SyntaxTree = Nothing
            If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
                Return False
            End If

            Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            If documentationComment Is Nothing OrElse
                Not IsExteriorTriviaLeftOfPosition(documentationComment, caretPosition) OrElse
                Not SpansSingleLine(documentationComment, snapshot) Then

                Return False
            End If

            Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            If Not targetMember.SupportsDocumentationComments() Then
                Return False
            End If

            If caretPosition > targetMember.Span.Start Then
                Return False
            End If

            InsertCommentAfterTripleApostrophesCore(targetMember, tree, caretPosition, subjectBufferCaretPosition)

            Return True
        End Function

        Private Sub InsertCommentAfterTripleApostrophesCore(
            ByVal targetMember As StatementSyntax,
            ByVal tree As SyntaxTree,
            ByVal position As Integer,
            ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)

            Dim indent = tree.Text.GetLeadingWhitespaceOfLineAtPosition(position)

            Dim pair = GenerateDocumentationCommentText(
                targetMember, tree, indent,
                prependExteriorTrivia:=False,
                appendLineBreakAndIndent:=False)

            InsertCommentText(
                position:=position,
                commentText:=pair.Item1,
                caretOffset:=pair.Item2,
                subjectBufferCaretPosition:=subjectBufferCaretPosition)
        End Sub

        Private Function InsertCommentOnContainingMember(ByVal textView As ITextView, ByVal subjectBuffer As ITextBuffer) As Boolean
            ' The strategy here is simple. Retrieve the member declaration that contains the caret.
            ' If it supports documentation comments and doesn't already have one, generate and 
            ' insert a documentation comment.

            Dim subjectBufferCaretPosition As New subjectBufferCaretPosition(textView, subjectBuffer)

            Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            If caretPosition < 0 Then
                Return False
            End If

            Dim snapshot = subjectBuffer.CurrentSnapshot
            Dim tree As SyntaxTree = Nothing
            If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
                Return False
            End If

            Dim token = tree.Root.FindToken(caretPosition)

            Dim targetMember = token.GetContainingMember()
            If Not targetMember.SupportsDocumentationComments() OrElse
                targetMember.Span.Start > caretPosition OrElse
                targetMember.Span.End < caretPosition Then

                Return False
            End If

            If targetMember.HasDocumentationComment() Then
                Return False
            End If

            Dim indent = tree.Text.GetLeadingWhitespaceOfLineAtPosition(targetMember.GetFirstToken().Span.Start)

            Dim pair = GenerateDocumentationCommentText(
                targetMember, tree, indent,
                prependExteriorTrivia:=True,
                appendLineBreakAndIndent:=True)

            InsertCommentText(
                position:=targetMember.Span.Start,
                commentText:=pair.Item1,
                caretOffset:=pair.Item2,
                subjectBufferCaretPosition:=subjectBufferCaretPosition)

            Return True
        End Function

        Private Sub InsertLineBreakAndTripleApostrophesAtCaret(ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)
            Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            If caretPosition < 0 Then
                Return
            End If

            Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer

            Dim snapshot = subjectBuffer.CurrentSnapshot
            Dim lineNumber = snapshot.GetLineNumberFromPosition(caretPosition)

            Dim indent = String.Empty
            If lineNumber >= 0 Then
                Dim line = snapshot.GetLineFromLineNumber(lineNumber)
                Dim lineText = line.GetText()
                Dim slashesIndex = lineText.IndexOf("'''")
                If slashesIndex >= 0 Then
                    indent = New String(" "c, slashesIndex)
                End If
            End If

            Dim text = vbCrLf & indent & "''' "

            Dim newSnapshot = subjectBuffer.Insert(caretPosition, text)
            Dim caretPoint = New SnapshotPoint(newSnapshot, caretPosition + text.Length)

            subjectBufferCaretPosition.TryMoveTo(caretPoint)
        End Sub

        Private Sub InsertCommentText(
            ByVal position As Integer,
            ByVal commentText As String,
            ByVal caretOffset As Integer,
            ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)

            If String.IsNullOrWhiteSpace(commentText) Then
                Return
            End If

            Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            Dim newSnapshot = subjectBuffer.Insert(position, commentText)
            Dim caretPoint = New SnapshotPoint(newSnapshot, position + caretOffset)
            subjectBufferCaretPosition.TryMoveTo(caretPoint)
        End Sub

        Private Sub ReplaceWithCommentText(
            ByVal replaceSpan As Span,
            ByVal commentText As String,
            ByVal caretOffset As Integer,
            ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)

            If String.IsNullOrWhiteSpace(commentText) Then
                Return
            End If

            Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            Dim newSnapshot = subjectBuffer.Replace(replaceSpan, commentText)
            Dim caretPoint = New SnapshotPoint(newSnapshot, replaceSpan.Start + caretOffset)
            subjectBufferCaretPosition.TryMoveTo(caretPoint)
        End Sub

        Private Function IsRestOfLineWhitespace(ByVal snapshot As ITextSnapshot, ByVal position As Integer) As Boolean
            Dim line = snapshot.GetLineFromPosition(position)
            Dim lineTextToEnd = line.GetText().Substring(position - line.Start.Position)
            Return String.IsNullOrWhiteSpace(lineTextToEnd)
        End Function

        Private Function GetDocumentationComment(ByVal tree As SyntaxTree, ByVal position As Integer) As DocumentationCommentSyntax
            Dim trivia = tree.Root.FindTrivia(position)
            If (trivia.Kind = SyntaxKind.DocumentationComment) Then
                Return DirectCast(trivia.GetStructure(), DocumentationCommentSyntax)
            Else
                Return Nothing
            End If
        End Function

        Private Function ExteriorTriviaStartsLine(ByVal tree As SyntaxTree, ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            Dim line = tree.Text.GetLineFromPosition(position)
            Dim firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition()
            If Not firstNonWhitespacePosition.HasValue Then
                Return False
            End If

            Dim token = documentationComment.FindToken(firstNonWhitespacePosition.Value)
            Dim trivia = token.LeadingTrivia.FirstOrDefault()
            Return trivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia
        End Function

        Private Function IsExteriorTriviaLeftOfPosition(ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            Dim token = documentationComment.FindToken(position)
            Dim trivia = token.LeadingTrivia.FirstOrDefault()
            Return trivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia AndAlso
                trivia.Span.End = position
        End Function

        Private Function GetDocumentationCommentTargetMember(ByVal documentationComment As DocumentationCommentSyntax) As StatementSyntax
            Dim parentTrivia = documentationComment.ParentTrivia
            Return parentTrivia.Token.GetAncestor(Of StatementSyntax)()
        End Function

        Private Function SpansSingleLine(ByVal documentationComment As DocumentationCommentSyntax, ByVal snapshot As ITextSnapshot) As Boolean
            ' Use full span to include leading exterior trivia
            Dim startLine = snapshot.GetLineNumberFromPosition(documentationComment.FullSpan.Start)

            ' The last token *should* be an XmlTextLiteralNewLineToken and we'll use the start of its span
            ' to ensure that we aren't getting the line number after the line break.
            Dim lastToken = documentationComment.GetLastToken()
            Dim endLine = snapshot.GetLineNumberFromPosition(lastToken.Span.Start)

            Return startLine = endLine
        End Function

        ''' <summary>
        ''' Calculates the documentation comment for a target member and returns a tuple containing the text of the comment and
        ''' the offset of the expected caret position.
        ''' </summary>
        Private Function GenerateDocumentationCommentText(
            ByVal targetMember As StatementSyntax,
            ByVal tree As SyntaxTree,
            ByVal indent As String,
            Optional ByVal prependExteriorTrivia As Boolean = True,
            Optional ByVal appendLineBreakAndIndent As Boolean = False) As Tuple(Of String, Integer)

            Dim builder As New StringBuilder

            If prependExteriorTrivia Then
                builder.Append("'''")
            End If

            ' Append summary
            builder.AppendLine(" <summary>")
            builder.Append(indent + "''' ")
            Dim offset = builder.Length
            builder.AppendLine()
            builder.Append(indent + "''' </summary>")

            ' Append any type parameters
            Dim typeParameterList = targetMember.GetTypeParameterList()
            If typeParameterList IsNot Nothing Then
                For Each typeParameter In typeParameterList.Parameters
                    builder.AppendLine()

                    builder.Append(indent + "''' <typeparam name=""")

                    Dim typeParameterName = typeParameter.Name.GetText()
                    If Not String.IsNullOrWhiteSpace(typeParameterName) Then
                        builder.Append(typeParameterName)
                    Else
                        builder.Append("?")
                    End If

                    builder.Append("""></typeparam>")
                Next
            End If

            ' Append any parameters
            Dim parameterList = targetMember.GetParameterList()
            If parameterList IsNot Nothing Then
                For Each parameter In parameterList.Parameters
                    builder.AppendLine()

                    builder.Append(indent + "''' <param name=""")
                    builder.Append(parameter.Name.GetText())
                    builder.Append("""></param>")
                Next
            End If

            ' Append return type
            Dim returnType = targetMember.GetReturnType()
            If returnType IsNot Nothing Then
                builder.AppendLine()
                builder.Append(indent + "''' <returns></returns>")
            End If

            If appendLineBreakAndIndent Then
                builder.AppendLine()
                builder.Append(indent)
            End If

            Return Tuple.Create(builder.ToString(), offset)
        End Function
    End Class
End Namespace
