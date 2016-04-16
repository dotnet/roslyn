' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Test.Utilities
Imports Xunit

Friend Module ParserTestUtilities

    ' TODO (tomat): only checks error codes; we should also check error span and arguments
    Public Function ParseAndVerify(code As XCData, Optional expectedErrors As XElement = Nothing) As SyntaxTree
        Return ParseAndVerify(code.Value, expectedErrors)
    End Function

    ' TODO (tomat): only checks error codes; we should also check error span and arguments
    Public Function ParseAndVerify(code As XCData, options As VisualBasicParseOptions, Optional expectedErrors As XElement = Nothing) As SyntaxTree
        Return ParseAndVerify(code.Value, options, expectedErrors)
    End Function

    ' TODO (tomat): only checks error codes; we should also check error span and arguments
    Public Function ParseAndVerify(source As String, options As VisualBasicParseOptions, Optional expectedErrors As XElement = Nothing) As SyntaxTree
        Dim expectedDiagnostics() As DiagnosticDescription = Nothing
        If expectedErrors IsNot Nothing Then
            Dim expectedXml = expectedErrors.<error>
            expectedDiagnostics = New DiagnosticDescription(expectedXml.Count - 1) {}
            For i = 0 To expectedDiagnostics.Length - 1
                Dim e = expectedXml.ElementAt(i)
                expectedDiagnostics(i) = TestBase.Diagnostic(CType(CInt(e.@id), ERRID))

                Debug.Assert(e.@line Is Nothing, "'line' attribute will be ignored")
                Debug.Assert(e.@column Is Nothing, "'column' attribute will be ignored")
            Next
        End If
        Return ParseAndVerify(source, options, expectedDiagnostics, errorCodesOnly:=True)
    End Function

    ' TODO (tomat): only checks error codes; we should also check error span and arguments
    Public Function ParseAndVerify(source As String, Optional expectedErrors As XElement = Nothing) As SyntaxTree
        Return ParseAndVerify(source, VisualBasicParseOptions.Default, expectedErrors)
    End Function

    Public Function ParseAndVerify(code As XCData, ParamArray expectedDiagnostics() As DiagnosticDescription) As SyntaxTree
        Return ParseAndVerify(code.Value, VisualBasicParseOptions.Default, expectedDiagnostics, errorCodesOnly:=False)
    End Function

    Public Function ParseAndVerify(code As XCData, options As VisualBasicParseOptions, ParamArray expectedDiagnostics() As DiagnosticDescription) As SyntaxTree
        Return ParseAndVerify(code.Value, options, expectedDiagnostics, errorCodesOnly:=False)
    End Function

    Public Function ParseAndVerify(source As String, ParamArray expectedDiagnostics() As DiagnosticDescription) As SyntaxTree
        Return ParseAndVerify(source, VisualBasicParseOptions.Default, expectedDiagnostics, errorCodesOnly:=False)
    End Function

    Public Function ParseAndVerify(source As String, options As VisualBasicParseOptions, ParamArray expectedDiagnostics() As DiagnosticDescription) As SyntaxTree
        Return ParseAndVerify(source, options, expectedDiagnostics, errorCodesOnly:=False)
    End Function

    Public Function ParseAndVerify(source As String, languageVersion As LanguageVersion, ParamArray expectedDiagnostics() As DiagnosticDescription) As SyntaxTree
        Return ParseAndVerify(source, VisualBasicParseOptions.Default.WithLanguageVersion(languageVersion), expectedDiagnostics, errorCodesOnly:=False)
    End Function

    Public Function ParseAndVerify(source As String, languageVersion As LanguageVersion, errorCodesOnly As Boolean, ParamArray expectedDiagnostics() As DiagnosticDescription) As SyntaxTree
        Return ParseAndVerify(source, VisualBasicParseOptions.Default.WithLanguageVersion(languageVersion), expectedDiagnostics, errorCodesOnly:=errorCodesOnly)
    End Function

    Private Function ParseAndVerify(source As String, options As VisualBasicParseOptions, expectedDiagnostics() As DiagnosticDescription, errorCodesOnly As Boolean) As SyntaxTree
        Dim tree = Parse(source, options:=options)
        Dim root = tree.GetRoot()

        ' Verify Errors
        If expectedDiagnostics Is Nothing Then
            Dim errors As New StringBuilder()
            AppendSyntaxErrors(tree.GetDiagnostics(), errors)
            Assert.False(root.ContainsDiagnostics, errors.ToString())
            Assert.Equal(root.ContainsDiagnostics, errors.Length > 0)
        Else
            Assert.True(root.ContainsDiagnostics, "Tree was expected to contain errors.")
            If errorCodesOnly Then
                tree.GetDiagnostics().VerifyErrorCodes(expectedDiagnostics)
            Else
                tree.GetDiagnostics().Verify(expectedDiagnostics)
            End If
        End If

        Return tree
    End Function

    Public Function Parse(code As XCData, Optional options As VisualBasicParseOptions = Nothing) As SyntaxTree
        Return Parse(code.Value, fileName:="", options:=options)
    End Function

    Public Function Parse(code As String, Optional options As VisualBasicParseOptions = Nothing) As SyntaxTree
        Return Parse(code, fileName:="", options:=options)
    End Function

    Public Function Parse(source As String, fileName As String, Optional options As VisualBasicParseOptions = Nothing) As SyntaxTree
        Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(source), options:=If(options, VisualBasicParseOptions.Default), path:=fileName)
        Dim root = tree.GetRoot()
        ' Verify FullText
        Assert.Equal(source, root.ToFullString)
        'Verify that nodes are correctly parented
        VerifyParents(root)
        Return tree
    End Function

    <WorkItem(922332, "DevDiv/Personal")>
    <WorkItem(927690, "DevDiv/Personal")>
    Private Sub VerifyParents(nodeOrToken As SyntaxNodeOrToken)
        'The only reason we calculate spans of the various nodes is
        'to make sure that the span calculation does not cause any crash.
        Dim span = nodeOrToken.Span
        If nodeOrToken.IsToken Then
            Dim token = nodeOrToken
            For Each trivia In token.GetLeadingTrivia()
                Dim parentToken = trivia.Token
                Assert.Equal(token, parentToken)
                If trivia.HasStructure Then
                    Dim triviaStructure = DirectCast(trivia.GetStructure, VisualBasicSyntaxNode)
                    Dim parent = triviaStructure.Parent
                    Assert.Equal(Nothing, parent)

                    Dim parentTrivia = DirectCast(triviaStructure, StructuredTriviaSyntax).ParentTrivia
                    Assert.Equal(trivia, parentTrivia)

                    VerifyParents(triviaStructure)
                Else
                    span = trivia.Span
                End If
            Next
            For Each trivia In token.GetTrailingTrivia()
                Dim parentToken = trivia.Token
                Assert.Equal(token, parentToken)
                If trivia.HasStructure Then
                    Dim triviaStructure = trivia.GetStructure
                    Dim parent = triviaStructure.Parent
                    Assert.Equal(Nothing, parent)

                    Dim parentTrivia = DirectCast(triviaStructure, StructuredTriviaSyntax).ParentTrivia
                    Assert.Equal(trivia, parentTrivia)

                    VerifyParents(triviaStructure)
                Else
                    span = trivia.Span
                End If
            Next
        Else
            Dim node = nodeOrToken
            For Each child In node.ChildNodesAndTokens()
                Dim parent = child.Parent
                Assert.Equal(node, parent)
                VerifyParents(child)
            Next
        End If
    End Sub

    <Extension()>
    Public Function ToFullWidth(s As String) As String
        Return New String(s.Select(AddressOf ToFullWidth).ToArray())
    End Function

    <Extension()>
    Public Function ToFullWidth(c As Char) As Char
        Return If(IsHalfWidth(c), MakeFullWidth(c), c)
    End Function

#Region "Debugging Helpers"

    'If we migrate tests to xUnit, this can go away, because the Assert.Equal provides this...
    Private Function MismatchPosition(s1 As String, s2 As String) As Integer

        Dim count As Integer = If(s1.Length < s2.Length, s1.Length, s2.Length)
        For i = 0 To count - 1
            If s1(i) <> s2(i) Then
                Return i
            End If
        Next
        Return count
    End Function

#End Region

#Region "Incremental Parse Verification"
    Public Enum ChangeType
        Insert
        InsertBefore
        Remove
        Replace
    End Enum

    ''' <summary>
    ''' Represents the incremental parser change
    ''' </summary>
    ''' <field cref="IncParseNode.oldText">Old text on which the incremental parse is applied</field>
    ''' <field cref="IncParseNode.changeText">The new text that is added/removed/replaced</field>
    ''' <field cref="IncParseNode.changeSpan">OF type TextSpan. The start and length of the change</field>
    ''' <field cref="IncParseNode.changeType">Whether text was added, removed or replaced</field>
    Public Structure IncParseNode
        Public oldText As String
        Public changeText As String
        Public changeSpan As TextSpan
        Public changeType As ChangeType
    End Structure

    Public Sub IncParseAndVerify(oldIText As SourceText, newIText As SourceText)
        Dim newText = newIText.ToString
        Dim oldText = oldIText.ToString

        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldIText)
        Dim incTreeRoot = oldTree.GetRoot()
        Dim newTree = VisualBasicSyntaxTree.ParseText(newIText)
        Dim newTreeRoot = newTree.GetRoot()
        Dim incTree = oldTree.WithChangedText(newIText)
        incTreeRoot = incTree.GetRoot()

        ' IsEquivalentTo should be a bit faster than comparing Xml
        If Not newTreeRoot.IsEquivalentTo(incTreeRoot) Then
            ' init
            If (NodeHelpers.KindProvider Is Nothing) Then
                NodeHelpers.KindProvider = New VBKindProvider()
            End If
            Dim x1 = newTreeRoot.ToXml(newTree)
            Dim x2 = incTreeRoot.ToXml(incTree)
            ' Verify Incremental parse
            Assert.Equal(x1.ToString, x2.ToString)

            ' in case if Xml was same for some reason.
            Assert.Equal(True, False)
        Else
            'Verify that nodes are correctly parented
            VerifyParents(oldTree.GetRoot())
            VerifyParents(newTree.GetRoot())
            VerifyParents(incTree.GetRoot())
        End If
    End Sub

    Public Sub IncParseAndVerify(ParamArray IncParseNodes As IncParseNode())
        For Each node In IncParseNodes
            Dim oldText = SourceText.From(node.oldText)
            Dim newText As SourceText = oldText

            Select Case node.changeType
                Case ChangeType.Insert
                    newText = oldText.WithChanges(New TextChange(node.changeSpan, node.changeText))
                Case ChangeType.InsertBefore
                    newText = oldText.WithChanges(New TextChange(New TextSpan(0, 0), node.changeText))
                Case ChangeType.Remove
                    newText = oldText.WithChanges(New TextChange(node.changeSpan, ""))
                Case ChangeType.Replace
                    newText = oldText.WithChanges(New TextChange(node.changeSpan, node.changeText))
                Case Else
                    Throw New NotImplementedException
            End Select

            IncParseAndVerify(oldText, newText)
        Next
    End Sub
#End Region

End Module

<Extension()>
Public Module VerificationHelpers

    ' Verification helpers added to this file should comply with one of the following conventions in order to ensure
    ' that they are composable with other verifications:
    '
    ' <Extension()>
    ' Public Function FunctionName(node As SyntaxTree, ... other parameters ...) As SyntaxTree
    '     ...
    ' End Function
    '
    ' <Extension()>
    ' Public Function FunctionName(node As SyntaxNodeOrToken, ... other parameters ...) As SyntaxNodeOrToken
    '     ...
    ' End Function

    <Extension()>
    Public Function VerifySyntaxKinds(tree As SyntaxTree, ParamArray expected As SyntaxKind()) As SyntaxTree
        VerifySyntaxKinds(tree.GetRoot(), expected)
        Return tree
    End Function

    <Extension()>
    Public Function VerifySyntaxKinds(node As SyntaxNodeOrToken, ParamArray expected As SyntaxKind()) As SyntaxNodeOrToken
        VerifySyntaxKinds(node, 0, expected)
        Return node
    End Function

    Private Function VerifySyntaxKinds(node As SyntaxNodeOrToken, ByRef i As Integer, expected As SyntaxKind()) As SyntaxNodeOrToken
        Assert.InRange(i, 0, expected.Length - 1)
        Assert.Equal(node.Kind(), expected(i))
        i += 1
        Dim children = node.ChildNodesAndTokens
        For j = 0 To children.Count - 1
            VerifySyntaxKinds(children(j), i, expected)
        Next
        Return node
    End Function

    <Extension()>
    Public Function VerifyOccurrenceCount(tree As SyntaxTree, kind As SyntaxKind, expectedCount As Integer) As SyntaxTree
        Dim actualCount = 0
        GetOccurrenceCount(kind, tree.GetRoot(), actualCount)
        Assert.Equal(expectedCount, actualCount)
        Return tree
    End Function

    <Extension()>
    Public Function TraverseAllNodes(tree As SyntaxTree) As SyntaxTree
        InternalTraverseAllNodes(tree.GetRoot())
        Return tree
    End Function

    <Extension()>
    Public Function FindNodeOrTokenByKind(tree As SyntaxTree, kind As SyntaxKind, Optional occurrence As Integer = 1) As SyntaxNodeOrToken
        If Not occurrence > 0 Then
            Throw New ArgumentException("Specified value must be greater than zero.", NameOf(occurrence))
        End If
        Dim foundNode As SyntaxNodeOrToken = Nothing
        If TryFindNodeOrToken(tree.GetRoot(), kind, occurrence, foundNode) Then
            Return foundNode
        End If
        Return Nothing
    End Function

    <Extension()>
    Public Function VerifyPrecedingCommentIsTrivia(node As SyntaxNodeOrToken) As SyntaxNodeOrToken
        Assert.NotEqual(node.Kind(), SyntaxKind.None)
        Dim trivia = node.GetLeadingTrivia()
        Assert.InRange(trivia.Count, 1, 2)
        Dim ticktickticknode As SyntaxTrivia = Nothing
        If trivia.Count = 1 Then
            ticktickticknode = trivia(0)
        ElseIf trivia.Count = 2 Then
            ticktickticknode = trivia(1)
        End If
        Assert.Equal(SyntaxKind.DocumentationCommentExteriorTrivia, ticktickticknode.Kind)
        Return node
    End Function

    <Extension()>
    Public Function VerifyNoWhitespaceInKeywords(nodeOrToken As SyntaxNodeOrToken) As SyntaxNodeOrToken
        InternalVerifyNoWhitespaceInKeywords(nodeOrToken)
        Return nodeOrToken
    End Function

    <Extension()>
    Public Function VerifyNoWhitespaceInKeywords(node As SyntaxNode) As SyntaxNode
        InternalVerifyNoWhitespaceInKeywords(node)
        Return node
    End Function

    <Extension()>
    Public Function VerifyNoWhitespaceInKeywords(tree As SyntaxTree) As SyntaxTree
        InternalVerifyNoWhitespaceInKeywords(tree.GetRoot())
        Return tree
    End Function

    <Extension()>
    Public Function VerifyNoMissingChildren(tree As SyntaxTree) As SyntaxTree
        Dim node = tree.GetRoot()
        Assert.False(node.IsMissing, "Unexpected missing node: " & node.Kind.ToString & node.Span.ToString)
        For Each child In node.ChildNodesAndTokens()
            InternalVerifyNoMissingChildren(child)
        Next
        Return tree
    End Function

    <Extension()>
    Public Function VerifyNoZeroWidthNodes(tree As SyntaxTree) As SyntaxTree
        Dim node = tree.GetRoot()
        Assert.True(0 <> node.Span.Length OrElse node.Kind = SyntaxKind.CompilationUnit, "Unexpected 0 width node: " & node.Kind.ToString & node.Span.ToString)
        For Each child In node.ChildNodesAndTokens()
            InternalVerifyNoZeroWidthNodes(child)
        Next
        Return tree
    End Function

    <Extension()>
    Public Function VerifyErrorsOnChildrenAlsoPresentOnParent(tree As SyntaxTree) As SyntaxTree
        Dim node = tree.GetRoot()
        For Each child In node.ChildNodesAndTokens()
            InternalVerifyErrorsOnChildrenAlsoPresentOnParent(child, tree)
        Next
        If tree.GetDiagnostics(node).Any Then
            If node.Parent IsNot Nothing Then
                VerifyContainsErrors(node.Parent, tree, tree.GetDiagnostics(node).ToXml)
            End If
        End If
        Return tree
    End Function

    Public Sub InternalVerifyErrorsOnChildrenAlsoPresentOnParent(node As SyntaxNodeOrToken, tree As SyntaxTree)
        If node.IsNode Then
            For Each child In node.AsNode.ChildNodesAndTokens()
                InternalVerifyErrorsOnChildrenAlsoPresentOnParent(child, tree)
            Next
        Else
            For Each tr In node.AsToken.LeadingTrivia
                If tr.HasStructure Then
                    InternalVerifyErrorsOnChildrenAlsoPresentOnParent(tr.GetStructure, tree)
                ElseIf tree.GetDiagnostics(tr).Any Then
                    VerifyContainsErrors(node, tree, tree.GetDiagnostics(tr).ToXml)
                End If
            Next
            For Each tr In node.AsToken.TrailingTrivia
                If tr.HasStructure Then
                    InternalVerifyErrorsOnChildrenAlsoPresentOnParent(tr.GetStructure, tree)
                ElseIf tree.GetDiagnostics(tr).Any Then
                    VerifyContainsErrors(node, tree, tree.GetDiagnostics(tr).ToXml)
                End If
            Next
        End If
        If tree.GetDiagnostics(node).Any Then
            If node.Parent IsNot Nothing Then
                VerifyContainsErrors(node.Parent, tree, tree.GetDiagnostics(node).ToXml)
            End If
        End If
    End Sub

    <Extension()>
    Public Function VerifyNoAdjacentTriviaHaveSameKind(tree As SyntaxTree) As SyntaxTree
        For Each child In tree.GetRoot().ChildNodesAndTokens()
            InternalVerifyNoAdjacentTriviaHaveSameKind(child)
        Next
        Return tree
    End Function

    <Extension()>
    Public Function VerifySpanOfChildWithinSpanOfParent(tree As SyntaxTree) As SyntaxTree
        Dim node = tree.GetRoot()
        For Each child In node.ChildNodesAndTokens()
            InternalVerifySpanOfChildWithinSpanOfParent(child)
        Next
        If node.Parent IsNot Nothing Then
            Assert.True(node.SpanStart >= node.Parent.SpanStart AndAlso
                        node.Span.End <= node.Parent.Span.End, "Span of child (" &
                        node.Kind.ToString & node.Span.ToString &
                        ") is not within span of parent (" &
                        node.Parent.Kind.ToString & node.Parent.Span.ToString & ")")
        End If
        Return tree
    End Function

    <Extension()>
    Public Function ToXml(errors As IEnumerable(Of Diagnostic)) As XElement
        Return <errors><%= From e In errors
                           Select <error id=<%= e.Code %>
                                      <%= If(e.Location.IsInSource, New XAttribute("start", e.Location.SourceSpan.Start), Nothing) %>
                                      <%= If(e.Location.IsInSource, New XAttribute("end", e.Location.SourceSpan.End), Nothing) %>
                                      <%= If(e.Location.IsInSource, New XAttribute("length", e.Location.SourceSpan.Length), Nothing) %>
                                  /> %>
               </errors>
    End Function

    <Extension()>
    Public Function GetSyntaxErrorsNoTree(t As SyntaxToken) As IEnumerable(Of Diagnostic)
        Return t.GetSyntaxErrors(GetMockTree())
    End Function

    <Extension()>
    Public Function GetSyntaxErrorsNoTree(n As SyntaxNode) As IEnumerable(Of Diagnostic)
        Return DirectCast(n, VisualBasicSyntaxNode).GetSyntaxErrors(GetMockTree())
    End Function

    Public Function GetMockTree() As SyntaxTree
        Return New MockSyntaxTree()
    End Function

    Private Class MockSyntaxTree
        Inherits VisualBasicSyntaxTree

        Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
            Throw New NotImplementedException()
        End Function

        Public Overrides ReadOnly Property FilePath As String
            Get
                Return ""
            End Get
        End Property

        Public Overrides ReadOnly Property Options As VisualBasicParseOptions
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean
            Throw New NotImplementedException()
        End Function

        Public Overrides ReadOnly Property HasCompilationUnitRoot As Boolean
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
            Throw New NotImplementedException()
        End Function

        Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
            Throw New NotImplementedException()
        End Function

        Public Overrides ReadOnly Property Encoding As Encoding
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides Function WithChangedText(newText As SourceText) As SyntaxTree
            Throw New NotImplementedException()
        End Function

        Public Overrides Function WithRootAndOptions(root As SyntaxNode, options As ParseOptions) As SyntaxTree
            Throw New NotImplementedException()
        End Function

        Public Overrides Function WithFilePath(path As String) As SyntaxTree
            Throw New NotImplementedException()
        End Function
    End Class

    Friend Sub AppendSyntaxErrors(errors As IEnumerable(Of Diagnostic), output As StringBuilder)
        For Each e In errors
            Dim span = e.Location.SourceSpan
            output.AppendLine(GetErrorString(e.Code, e.GetMessage(EnsureEnglishUICulture.PreferredOrNull), span.Start.ToString(), span.End.ToString()))
        Next
    End Sub

#Region "Private Helpers"

    Private Function GetErrorString(id As Integer, message As String, start As String, [end] As String) As String
        Dim errorString As New StringBuilder()
        errorString.Append(vbTab)
        errorString.Append("<error id=""")
        errorString.Append(id)
        errorString.Append("""")
        If message IsNot Nothing Then
            errorString.Append(" message=""")
            errorString.Append(message)
            errorString.Append("""")
        End If
        If start IsNot Nothing Then
            errorString.Append(" start=""")
            errorString.Append(start)
            errorString.Append("""")
        End If
        If [end] IsNot Nothing Then
            errorString.Append(" end=""")
            errorString.Append([end])
            errorString.Append("""")
        End If
        errorString.Append("/>")
        Return errorString.ToString()
    End Function

    Private Function AreErrorsEquivalent(syntaxError As Diagnostic, xmlError As XElement) As Boolean
        Dim areEquivalent As Boolean = True

        Dim id = xmlError.@id
        If id IsNot Nothing Then
            If CInt(id) <> syntaxError.Code Then
                areEquivalent = False
            End If
        Else
            Throw New ArgumentException("The 'id' attribute is required for all errors")
        End If
        Dim message = xmlError.@message
        If message IsNot Nothing AndAlso message <> syntaxError.GetMessage(EnsureEnglishUICulture.PreferredOrNull) Then
            areEquivalent = False
        End If

        Dim syntaxErrorSpan = syntaxError.Location.SourceSpan

        Dim spanStart = xmlError.@start
        If spanStart IsNot Nothing AndAlso CInt(spanStart) <> syntaxErrorSpan.Start Then
            areEquivalent = False
        End If
        Dim spanEnd = xmlError.@end
        If spanEnd IsNot Nothing AndAlso CInt(spanEnd) <> syntaxErrorSpan.End Then
            areEquivalent = False
        End If
        Dim spanLength = xmlError.@length
        If spanLength IsNot Nothing AndAlso CInt(spanLength) <> syntaxErrorSpan.Length Then
            areEquivalent = False
        End If

        Return areEquivalent
    End Function

    Private Sub VerifyContainsErrors(node As SyntaxNodeOrToken, tree As SyntaxTree,
                                  expectedErrors As XElement)
        Dim errorScenarioFailed As Boolean = False
        Dim unmatchedErrorList As New List(Of Diagnostic)(tree.GetDiagnostics(node))
        For Each xmlError In expectedErrors.<error>
            Dim matched As Boolean = False
            Dim index As Integer = 0
            While index < unmatchedErrorList.Count AndAlso Not matched
                Dim syntaxError = unmatchedErrorList(index)
                If AreErrorsEquivalent(syntaxError, xmlError) Then
                    unmatchedErrorList.RemoveAt(index)
                    matched = True
                Else
                    index += 1
                End If
            End While
            If Not matched Then
                errorScenarioFailed = True
            End If
        Next

        If errorScenarioFailed Then
            Dim errorMessage As New StringBuilder()
            errorMessage.AppendLine()
            errorMessage.AppendLine("Expected Subset:")
            For Each e In expectedErrors.<error>
                errorMessage.AppendLine(GetErrorString(CInt(e.@id), If(e.@message, "?"), If(e.@start, "?"), If(e.@end, "?")))
            Next
            errorMessage.AppendLine("Actual Errors (on " & node.Kind().ToString & node.Span.ToString & ")")
            AppendSyntaxErrors(tree.GetDiagnostics(node), errorMessage)
            Assert.False(errorScenarioFailed, errorMessage.ToString())
        End If
    End Sub

    Private Sub GetOccurrenceCount(kind As SyntaxKind, node As SyntaxNodeOrToken,
                                      ByRef actualCount As Integer)
        If node.IsKind(kind) Then
            actualCount += 1
        End If
        If node.IsToken Then
            Dim tk = node
            For Each leadingTrivia In tk.GetLeadingTrivia()
                If leadingTrivia.Kind = kind Then
                    actualCount += 1
                End If
                If leadingTrivia.HasStructure Then
                    Dim leadingTriviaStructure = leadingTrivia.GetStructure
                    GetOccurrenceCount(kind, leadingTriviaStructure, actualCount)
                End If
            Next
            For Each trailingTrivia In tk.GetTrailingTrivia()
                If trailingTrivia.Kind = kind Then
                    actualCount += 1
                End If
                If trailingTrivia.HasStructure Then
                    Dim trailingTriviaStructure = trailingTrivia.GetStructure
                    GetOccurrenceCount(kind, trailingTriviaStructure, actualCount)
                End If
            Next
        End If
        For Each child In node.ChildNodesAndTokens()
            GetOccurrenceCount(kind, child, actualCount)
        Next
    End Sub

    Private Sub InternalTraverseAllNodes(node As SyntaxNodeOrToken)
        'Traverse children
        For Each nd In node.ChildNodesAndTokens()
            InternalTraverseAllNodes(nd)
        Next

        For Each tr In node.GetLeadingTrivia()
            If tr.HasStructure Then
                Dim trStructure = tr.GetStructure
                InternalTraverseAllNodes(trStructure)
            End If
        Next
        For Each tr In node.GetTrailingTrivia()
            If tr.HasStructure Then
                Dim trStructure = tr.GetStructure
                InternalTraverseAllNodes(trStructure)
            End If
        Next
    End Sub

    Private Function TryFindNodeOrToken(node As SyntaxNodeOrToken, kind As SyntaxKind, ByRef occurrence As Integer, ByRef foundNode As SyntaxNodeOrToken) As Boolean
        If node.IsKind(kind) Then
            occurrence -= 1
            If occurrence = 0 Then
                foundNode = node
                Return True
            End If
        End If

        If node.IsToken Then
            Dim tk = node
            If TryFindNodeOrTokenInTrivia(tk.GetLeadingTrivia(), kind, occurrence, foundNode) Then
                Return True
            End If
            If TryFindNodeOrTokenInTrivia(tk.GetTrailingTrivia(), kind, occurrence, foundNode) Then
                Return True
            End If
        End If

        For Each child In node.ChildNodesAndTokens()
            If TryFindNodeOrToken(child, kind, occurrence, foundNode) Then
                Return True
            End If
        Next

        Return False
    End Function

    Private Function TryFindNodeOrTokenInTrivia(triviaList As SyntaxTriviaList, kind As SyntaxKind, ByRef occurrence As Integer, ByRef foundNode As SyntaxNodeOrToken) As Boolean
        For Each trivia In triviaList
            If trivia.HasStructure Then
                Dim triviaStructure = trivia.GetStructure
                If TryFindNodeOrToken(triviaStructure, kind, occurrence, foundNode) Then
                    Return True
                End If
            End If
        Next

        Return False
    End Function

    Private Sub InternalVerifyNoWhitespaceInKeywords(node As SyntaxNodeOrToken)
        If node.IsToken Then
            Dim tk = node.AsToken
            If tk.IsReservedKeyword() Then
                Assert.Equal(tk.ToString().Trim(), tk.ToString())
            End If
        End If
        For Each child In node.ChildNodesAndTokens()
            VerifyNoWhitespaceInKeywords(child)
        Next
    End Sub

    Private Sub InternalVerifyNoMissingChildren(node As SyntaxNodeOrToken)
        If node.IsNode Then
            Assert.False(node.IsMissing, "Unexpected missing node: " & node.Kind().ToString & node.Span.ToString)
            For Each child In node.AsNode.ChildNodesAndTokens()
                InternalVerifyNoMissingChildren(child)
            Next
        Else
            Assert.False(node.IsMissing AndAlso Not node.IsKind(SyntaxKind.StatementTerminatorToken) AndAlso
                         Not node.IsKind(SyntaxKind.ColonToken), "Unexpected missing token: " & node.Kind().ToString & node.Span.ToString)
            For Each tr In node.AsToken.LeadingTrivia
                If tr.HasStructure Then
                    InternalVerifyNoMissingChildren(tr.GetStructure)
                End If
            Next
            For Each tr In node.AsToken.LeadingTrivia
                If tr.HasStructure Then
                    InternalVerifyNoMissingChildren(tr.GetStructure)
                End If
            Next
        End If
    End Sub

    Private Sub InternalVerifyNoZeroWidthNodes(node As SyntaxNodeOrToken)
        If node.IsNode Then
            Assert.True(0 <> node.Span.Length, "Unexpected 0 width node: " & node.Kind().ToString & node.Span.ToString)
            For Each child In node.AsNode.ChildNodesAndTokens()
                InternalVerifyNoZeroWidthNodes(child)
            Next
        Else
            Assert.True(0 <> node.Span.Length OrElse node.IsKind(SyntaxKind.EndOfFileToken) OrElse node.IsKind(SyntaxKind.StatementTerminatorToken) OrElse node.IsKind(SyntaxKind.ColonToken), "Unexpected 0 width token: " & node.Kind().ToString & node.Span.ToString)
            For Each tr In node.AsToken.LeadingTrivia
                Assert.True(0 <> tr.Span.Length, "Unexpected 0 width trivia: " & node.Kind().ToString & node.Span.ToString)
                If tr.HasStructure Then
                    InternalVerifyNoZeroWidthNodes(tr.GetStructure)
                End If
            Next
            For Each tr In node.AsToken.LeadingTrivia
                Assert.True(0 <> tr.Span.Length, "Unexpected 0 width trivia: " & node.Kind().ToString & node.Span.ToString)
                If tr.HasStructure Then
                    InternalVerifyNoZeroWidthNodes(tr.GetStructure)
                End If
            Next
        End If
    End Sub

    Private Sub InternalVerifyNoAdjacentTriviaHaveSameKind(node As SyntaxNodeOrToken)
        If node.IsNode Then
            For Each child In node.AsNode.ChildNodesAndTokens()
                InternalVerifyNoAdjacentTriviaHaveSameKind(child)
            Next
        Else
            InternalVerifyNoAdjacentTriviaHaveSameKind(node, node.AsToken.LeadingTrivia)
            InternalVerifyNoAdjacentTriviaHaveSameKind(node, node.AsToken.TrailingTrivia)
        End If
    End Sub

    Private Sub InternalVerifyNoAdjacentTriviaHaveSameKind(node As SyntaxNodeOrToken, triviaList As SyntaxTriviaList)
        Dim prev As SyntaxTrivia? = Nothing
        For Each tr In triviaList
            If tr.HasStructure Then
                InternalVerifyNoAdjacentTriviaHaveSameKind(tr.GetStructure)
            End If

            ' Based on http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=527553
            ' it is Ok to have adjacent SkippedTokensTrivias
            If tr.Kind <> SyntaxKind.SkippedTokensTrivia AndAlso prev IsNot Nothing Then
                Assert.True(prev.Value.Kind <> tr.Kind,
                            "Both current and previous trivia have Kind=" & tr.Kind.ToString &
                            " [See under TokenKind=" & node.Kind().ToString & ", NonTerminalKind=" & node.Parent.Kind.ToString & "]")
            End If
            prev = tr
        Next
    End Sub

    Private Sub InternalVerifySpanOfChildWithinSpanOfParent(node As SyntaxNodeOrToken)
        If node.IsNode Then
            For Each child In node.AsNode.ChildNodesAndTokens()
                InternalVerifySpanOfChildWithinSpanOfParent(child)
            Next
        End If
        If node.Parent IsNot Nothing Then
            Assert.True(node.SpanStart >= node.Parent.SpanStart AndAlso
                        node.Span.End <= node.Parent.Span.End, "Span of child (" &
                        node.Kind().ToString & node.Span.ToString &
                        ") is not within span of parent (" &
                        node.Parent.Kind.ToString & node.Parent.Span.ToString & ")")
        End If
    End Sub

#End Region

    Public Class SyntaxWalkerVerifier
        Inherits VisualBasicSyntaxWalker

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(depth As SyntaxWalkerDepth)
            ' Required for accessing Trivia and Directive Nodes
            MyBase.New(depth)
        End Sub

        Public _Dict As New Dictionary(Of String, Integer)
        Public ReadOnly _Items As New List(Of VisualBasicSyntaxNode)

        Public Overrides Sub VisitForBlock(node As ForBlockSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitForBlock(node)
        End Sub

        Public Overrides Sub VisitForEachBlock(node As ForEachBlockSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitForEachBlock(node)
        End Sub

        Public Overrides Sub VisitConstDirectiveTrivia(node As ConstDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitConstDirectiveTrivia(node)
        End Sub
        Public Overrides Sub VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitIfDirectiveTrivia(node)
        End Sub
        Public Overrides Sub VisitElseDirectiveTrivia(node As ElseDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitElseDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitEndIfDirectiveTrivia(node As EndIfDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitEndIfDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitExternalSourceDirectiveTrivia(node As ExternalSourceDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitExternalSourceDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitEndExternalSourceDirectiveTrivia(node As EndExternalSourceDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitEndExternalSourceDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitReferenceDirectiveTrivia(node As ReferenceDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitReferenceDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitExternalChecksumDirectiveTrivia(node As ExternalChecksumDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitExternalChecksumDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitRegionDirectiveTrivia(node As RegionDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitRegionDirectiveTrivia(node)
        End Sub
        Public Overrides Sub VisitEndRegionDirectiveTrivia(node As EndRegionDirectiveTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitEndRegionDirectiveTrivia(node)
        End Sub

        Public Overrides Sub VisitAggregateClause(node As AggregateClauseSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitAggregateClause(node)
        End Sub

        Public Overrides Sub VisitCatchFilterClause(node As CatchFilterClauseSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitCatchFilterClause(node)
        End Sub

        Public Overrides Sub VisitDistinctClause(node As DistinctClauseSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitDistinctClause(node)
        End Sub
        Public Overrides Sub VisitGroupByClause(node As GroupByClauseSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitGroupByClause(node)
        End Sub

        Public Overrides Sub VisitMidExpression(node As MidExpressionSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitMidExpression(node)
        End Sub

        Public Overrides Sub VisitIncompleteMember(ByVal node As IncompleteMemberSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitIncompleteMember(node)
        End Sub

        Public Overrides Sub VisitInferredFieldInitializer(ByVal node As InferredFieldInitializerSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitInferredFieldInitializer(node)
        End Sub

        Public Overrides Sub VisitPartitionClause(node As PartitionClauseSyntax)
            IncrementTypeCounter(node, "PartitionClauseSyntax")
            MyBase.VisitPartitionClause(node)
        End Sub
        Public Overrides Sub VisitPartitionWhileClause(node As PartitionWhileClauseSyntax)
            IncrementTypeCounter(node, "PartitionWhileClauseSyntax")
            MyBase.VisitPartitionWhileClause(node)
        End Sub

        Public Overrides Sub VisitRangeCaseClause(node As RangeCaseClauseSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitRangeCaseClause(node)
        End Sub

        Public Overrides Sub VisitRangeArgument(node As RangeArgumentSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitRangeArgument(node)
        End Sub

        Public Overrides Sub VisitHandlesClause(node As HandlesClauseSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitHandlesClause(node)
        End Sub
        Public Overrides Sub VisitHandlesClauseItem(node As HandlesClauseItemSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitHandlesClauseItem(node)
        End Sub

        Public Overrides Sub VisitWithEventsEventContainer(node As WithEventsEventContainerSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitWithEventsEventContainer(node)
        End Sub

        Public Overrides Sub VisitKeywordEventContainer(node As KeywordEventContainerSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitKeywordEventContainer(node)
        End Sub

        Public Overrides Sub VisitOmittedArgument(node As OmittedArgumentSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitOmittedArgument(node)
        End Sub

        Public Overrides Sub VisitSkippedTokensTrivia(node As SkippedTokensTriviaSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitSkippedTokensTrivia(node)
        End Sub

        Public Overrides Sub VisitXmlBracketedName(node As XmlBracketedNameSyntax)
            IncrementTypeCounter(node, node.Kind.ToString)
            MyBase.VisitXmlBracketedName(node)
        End Sub

        Public Sub IncrementTypeCounter(Node As VisualBasicSyntaxNode, NodeKey As String)
            _Items.Add(Node)
            If _Dict.ContainsKey(NodeKey) Then
                _Dict(NodeKey) = _Dict(NodeKey) + 1 'Increment Count
            Else
                _Dict.Add(NodeKey, 1) ' New Item
            End If
        End Sub

        Public Function GetCount(Node As String) As Integer
            If _Dict.ContainsKey(Node) Then
                Return _Dict(Node)
            Else
                Return 0
            End If
        End Function

        Public Function GetItem() As List(Of VisualBasicSyntaxNode)
            Return _Items
        End Function
    End Class
End Module
