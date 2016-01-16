' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Public Class VisualBasicEditAndContinueAnalyzerTests

#Region "Helpers"
        Private Shared Sub TestSpans(source As String, hasLabel As Func(Of SyntaxNode, Boolean))
            Dim tree = SyntaxFactory.ParseSyntaxTree(ClearSource(source))

            For Each expected In GetExpectedPositionsAndSpans(source)
                Dim expectedSpan = expected.Value
                Dim expectedText As String = source.Substring(expectedSpan.Start, expectedSpan.Length)
                Dim node = tree.GetRoot().FindToken(expected.Key).Parent

                While Not hasLabel(node)
                    node = node.Parent
                End While

                Dim actualSpan = VisualBasicEditAndContinueAnalyzer.GetDiagnosticSpanImpl(node.Kind, node, EditKind.Update)
                Dim actualText = source.Substring(actualSpan.Start, actualSpan.Length)

                Assert.True(expectedSpan = actualSpan, vbCrLf &
                            "Expected span: '" & expectedText & "' " & expectedSpan.ToString() & vbCrLf &
                            "Actual span: '" & actualText & "' " & actualSpan.ToString())
            Next
        End Sub

        Private Const s_startTag As String = "<span>"
        Private Const s_endTag As String = "</span>"
        Private Const s_startSpanMark As String = "[|"
        Private Const s_endSpanMark As String = "|]"
        Private Const s_positionMark As Char = "$"c

        Private Shared Function ClearSource(source As String) As String
            Return source.
                Replace(s_startTag, New String(" "c, s_startTag.Length)).
                Replace(s_endTag, New String(" "c, s_endTag.Length)).
                Replace(s_startSpanMark, New String(" "c, s_startSpanMark.Length)).
                Replace(s_endSpanMark, New String(" "c, s_startSpanMark.Length)).
                Replace(s_positionMark, " "c)
        End Function

        Private Shared Iterator Function GetExpectedPositionsAndSpans(source As String) As IEnumerable(Of KeyValuePair(Of Integer, TextSpan))
            Dim i As Integer = 0

            While True
                Dim start As Integer = source.IndexOf(s_startTag, i, StringComparison.Ordinal)
                If start = -1 Then
                    Exit While
                End If

                start += s_startTag.Length
                Dim [end] As Integer = source.IndexOf(s_endTag, start + 1, StringComparison.Ordinal)

                Dim length = [end] - start
                Dim position = source.IndexOf(s_positionMark, start, length)

                Dim span As TextSpan
                If position < 0 Then
                    position = start
                    span = New TextSpan(start, length)
                Else
                    position += 1
                    span = TextSpan.FromBounds(source.IndexOf(s_startSpanMark, start, length, StringComparison.Ordinal) + s_startSpanMark.Length,
                                               source.IndexOf(s_endSpanMark, start, length, StringComparison.Ordinal))
                End If

                Yield KeyValuePair.Create(position, span)
                i = [end] + 1
            End While
        End Function

        Private Shared Sub TestErrorSpansAllKinds(hasLabel As Func(Of SyntaxKind, Boolean))
            Dim unhandledKinds As List(Of SyntaxKind) = New List(Of SyntaxKind)()

            For Each k In [Enum].GetValues(GetType(SyntaxKind)).Cast(Of SyntaxKind)().Where(hasLabel)
                Try
                    VisualBasicEditAndContinueAnalyzer.GetDiagnosticSpanImpl(k, Nothing, EditKind.Update)
                Catch e1 As NullReferenceException
                    ' expected, we passed null node
                Catch e2 As Exception
                    ' unexpected
                    unhandledKinds.Add(k)
                End Try
            Next

            AssertEx.Equal(Array.Empty(Of SyntaxKind)(), unhandledKinds)
        End Sub
#End Region

        <Fact>
        Public Sub ErrorSpans_TopLevel()
            Dim source = <![CDATA[
<span>Option Strict Off</span>
<span>Imports Z = Foo.Bar</span>

<<span>Assembly: A(1,2,3,4)</span>, <span>B</span>>

<span>Namespace N.M</span> 
End Namespace

<A, B>
<span>Structure S(Of T As {New, Class, I})</span>
    Inherits B
End Structure

Structure S(Of T <span>As {New, Class, I}</span>)
End Structure

Structure S(Of T As {<span>New</span>, <span>Class</span>, <span>I</span>})
End Structure

<A, B>
<span>Public MustInherit Partial Class C</span>
End Class

<span>Interface I</span>
  Implements J, K, L
End Interface

<A>
<span>Enum E1</span>
End Enum

<span>Enum E2</span> As UShort
End Enum

<span>Public Enum E3</span>
    Q
    <A>R = 3
End Enum

<A>
<span>Public Delegate Sub D1(Of T As Struct)()</span>

<span>Delegate Function D2()</span> As C(Of T)

<span>Delegate Function D2</span> As C(Of T)
<span>Delegate Sub D2</span> As C(Of T)

<<span>Attrib</span>>
<span><Attrib></span>
<span>Public MustInherit Class Z</span>
    <span>Dim f0 As Integer</span>
    <span>Dim WithEvents EClass As New EventClass</span>
    <A><span>Dim f1 = 1</span>
    <A><span>Dim f2 As Integer = 1</span>
    Private <span>f3()</span>, <span>f4?</span>, <span>f5</span> As Integer, <span>f6</span> As New C()

    <span>Public Shared ReadOnly f As Integer</span>

    <A><span>Function M1()</span> As Integer
    End Function

    <span>Function M2()</span> As Integer Implements I.Foo
    End Function

    <span>Function M3()</span> As Integer Handles I.E
    End Function

    <span>Private Function M4(Of S, T)()</span> As Integer Handles I.E
    End Function

    <span>MustOverride Function M5</span>

    Sub M6(<A><span>Optional p1 As Integer = 2131</span>, 
           <span>p2</span> As Integer, 
           <span>p3</span>, 
           <Out><span>ByRef p3</span>, 
           <span>ParamArray x() As Integer</span>)
    End Sub

    <A><span>Event E1</span> As A
    <A><span>Private Event E1</span> As A
    <A><span>Property P</span> As Integer

    <A><span>Public MustOverride Custom Event E3</span> As A 
        <A><span>AddHandler(value As Action)</span>
        End AddHandler
        <A><span>RemoveHandler(value As Action)</span>
        End RemoveHandler
        <A><span>RaiseEvent</span>
        End RaiseEvent
    End Event

    <A><span>Property P</span> As Integer
        <A><span>Get</span>
        End Get
        <A><span>Private Set(value As Integer)</span>
        End Set
    End Property

    <A><span>Public Shared Narrowing Operator CType(d As Z)</span> As Integer
    End Operator
End Class
]]>.Value
            TestSpans(source, Function(node) TopSyntaxComparer.HasLabel(node.Kind(), ignoreVariableDeclarations:=False))
        End Sub

        <Fact>
        Public Sub ErrorSpans_StatementLevel_Update()
            Dim source = <![CDATA[
Class C
    Sub M()
        <span>While expr</span>
            <span>Continue While</span>
            <span>Exit While</span>
        End While

        <span>Do</span>
            <span>Continue Do</span>
            <span>Exit Do</span>
        Loop While expr

        <span>Do</span>
        Loop

        <span>Do Until expr</span>
        Loop

        <span>Do While expr</span>
        Loop

        <span>For Each a In b</span>
            <span>Continue For</span>
            <span>Exit For</span>
        Next

        <span>For i = 1 To 10 Step 2</span>
        Next

        <span>Using expr</span>
        End Using

        <span>SyncLock expr</span>
        End SyncLock

        <span>With z</span>
            <span>.M()</span>
        End With

<span>label:</span>
        <span>F()</span>

        <span>Static a As Integer = 1</span>
        <span>Dim b = 2</span>
        <span>Const c = 4</span>
        Dim <span>c</span>, <span>d</span> As New C()

        <span>GoTo label</span>
        <span>Stop</span>
        <span>End</span>
        <span>Exit Sub</span>
        <span>Return</span>
        <span>Return 1</span>
        <span>Throw</span>
        <span>Throw expr</span>
        
        <span>Try</span>
            <span>Exit Try</span>
        <span>Catch</span>
        End Try

        <span>Try</span>
        <span>Catch e As E</span>
        End Try

        <span>Try</span>
        <span>Catch e As E When True</span>
        End Try

        <span>Try</span>
        <span>Finally</span>
        End Try

        <span>If expr Then</span>
        End If

        <span>If expr</span>
        <span>ElseIf expr</span>
        <span>Else</span>
        End If

        <span>If True Then</span> M1() <span>Else</span> M2()

        <span>Select expr</span>
          <span>Case 1</span>
        End Select

        <span>Select expr</span>
          <span>Case 1</span>
             <span>GoTo 1</span>
        End Select

        <span>Select expr</span>
          <span>Case 1</span>
             <span>GoTo label</span>
          <span>Case 4 To 9</span>
             <span>Exit Select</span>
          <span>Case = 2</span>
          <span>Case < 2</span>
          <span>Case > 2</span>
          <span>Case Else</span>
             <span>Return</span>
        End Select

        <span>On Error Resume Next</span>
        <span>On Error Goto 0</span>
        <span>On Error Goto -1</span>
        <span>On Error GoTo label</span>
        <span>Resume</span>
        <span>Resume Next</span>

        <span>AddHandler e, AddressOf M</span>
        <span>RemoveHandler e, AddressOf M</span>
        <span>RaiseEvent e()</span>

        <span>Error 1</span>
        <span>Mid(a, 1, 2) = ""</span>
        <span>a = b</span>
        <span>Call M()</span>

        <span>Dim intArray(10, 10, 10) As Integer</span>
        <span>ReDim Preserve intArray(10, 10, 20)</span>
        <span>ReDim Preserve intArray(10, 10, 15)</span>
        <span>ReDim intArray(10, 10, 10)</span>
        <span>Erase intArray</span>

        F(<span>Function(x)</span> x)
        F(<span>Sub(x)</span> M())

        F(<span>[|Aggregate|] z $In {1, 2, 3}</span> Into <span>Max(z)</span>)

        F(<span>[|From|] z $In {1, 2, 3}</span>?
          Order By <span>z Descending</span>, <span>z Ascending</span>, <span>z</span>
          <span>[|Take While|] z $> 0</span>)

        F(From z1 In {1, 2, 3}
          <span>[|Join|] z2 $In {1, 2, 3}</span> On z1 Equals z2
          <span>[|Select|] z1 $+ z2</span>)

        F(From z1 In {1, 2, 3}
          Join z2 In {1, 2, 3} On <span>$z1 [|Equals|] z2</span>
          Select z1 + z2)

        F(From z1 In {1, 2, 3}
          Join z2 In {1, 2, 3} On <span>z1 [|Equals|] $z2</span>
          Select z1 + z2)

        F(From a In b <span>[|Let|] x = $expr</span> Select expr)

        F(From a In b <span>[|Group|] $a1 By b2 Into z1</span> Select d1)
        F(From a In b <span>[|Group|] a1 By $b2 Into z2</span> Select d2)
        F(From a In b Group a1 By b2 Into <span>z3</span> Select d3)

        F(From cust In A
          <span>[|Group Join|] ord In $B On z4 Equals y4
                Into CustomerOrders = Group, OrderTotal = Sum(ord.Total + 4)</span>
          Select 1)

        F(From cust In A
          <span>Group Join ord In B On $z5 [|Equals|] y5
                Into CustomerOrders = Group, OrderTotal = Sum(ord.Total + 3)</span>
          Select 1)

        F(From cust In A
          <span>Group Join ord In B On z6 [|Equals|] $y6
                Into CustomerOrders = Group, OrderTotal = Sum(ord.Total + 2)</span>
          Select 1)

        F(From cust In A
          Group Join ord In B On z7 Equals y7
                Into CustomerOrders = Group, OrderTotal = <span>Sum(ord.Total + 1)</span>
          Select 1)
    End Sub
End Class
]]>.Value
            TestSpans(source, AddressOf StatementSyntaxComparer.HasLabel)

            source = <![CDATA[
Class C
    Iterator Function M() As IEnumerable(Of Integer)
        <span>Yield 1</span>
    End Function
End Class
]]>.Value
            TestSpans(source, AddressOf StatementSyntaxComparer.HasLabel)

            source = <![CDATA[
Class C
    Async Function M() As Task(Of Integer)
        <span>Await expr</span>
    End Function
End Class
]]>.Value
            TestSpans(source, AddressOf StatementSyntaxComparer.HasLabel)

        End Sub

        ''' <summary>
        ''' Verifies that <see cref="CSharpEditAndContinueAnalyzer.GetDiagnosticSpanImpl"/> handles all <see cref="SyntaxKind"/> s.
        ''' </summary>
        <Fact>
        Public Sub ErrorSpansAllKinds()
            TestErrorSpansAllKinds(AddressOf StatementSyntaxComparer.IgnoreLabeledChild)
            TestErrorSpansAllKinds(Function(kind) TopSyntaxComparer.HasLabel(kind, ignoreVariableDeclarations:=False))
        End Sub

        <Fact>
        Public Async Function AnalyzeDocumentAsync_InsignificantChangesInMethodBody() As Threading.Tasks.Task
            Dim source1 = "
Class C
    Sub Main()

        ' comment
        System.Console.WriteLine(1)
    End Sub
End Class
"
            Dim source2 = "
Class C
    Sub Main()
        System.Console.WriteLine(1)
    End Sub
End Class
"
            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(source1)
                Dim documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id
                Dim oldSolution = workspace.CurrentSolution
                Dim newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2))
                Dim oldDocument = oldSolution.GetDocument(documentId)
                Dim oldText = Await oldDocument.GetTextAsync()
                Dim oldSyntaxRoot = Await oldDocument.GetSyntaxRootAsync()
                Dim newDocument = newSolution.GetDocument(documentId)
                Dim newText = Await newDocument.GetTextAsync()
                Dim newSyntaxRoot = Await newDocument.GetSyntaxRootAsync()

                Dim oldStatementSource = "System.Console.WriteLine(1)"
                Dim oldStatementPosition = source1.IndexOf(oldStatementSource, StringComparison.Ordinal)
                Dim oldStatementTextSpan = New TextSpan(oldStatementPosition, oldStatementSource.Length)
                Dim oldStatementSpan = oldText.Lines.GetLinePositionSpan(oldStatementTextSpan)
                Dim oldStatementSyntax = oldSyntaxRoot.FindNode(oldStatementTextSpan)

                Dim baseActiveStatements = ImmutableArray.Create(New ActiveStatementSpan(ActiveStatementFlags.LeafFrame, oldStatementSpan))
                Dim result = Await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newDocument, Nothing)

                Assert.True(result.HasChanges)
                Assert.True(result.SemanticEdits(0).PreserveLocalVariables)
                Dim syntaxMap = result.SemanticEdits(0).SyntaxMap

                Dim newStatementSpan = result.ActiveStatements(0)
                Dim newStatementTextSpan = newText.Lines.GetTextSpan(newStatementSpan)
                Dim newStatementSyntax = newSyntaxRoot.FindNode(newStatementTextSpan)

                Dim oldStatementSyntaxMapped = syntaxMap(newStatementSyntax)
                Assert.Same(oldStatementSyntax, oldStatementSyntaxMapped)
            End Using
        End Function

        <Fact>
        Public Async Function AnalyzeDocumentAsync_SyntaxError_NoChange1() As Threading.Tasks.Task
            Dim source = "
Class C
    Public Shared Sub Main()
        System.Console.WriteLine(1  ' syntax error
    End Sub
End Class
"

            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(source)
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim baseActiveStatements = ImmutableArray.Create(Of ActiveStatementSpan)()
                Dim result = Await analyzer.AnalyzeDocumentAsync(workspace.CurrentSolution, baseActiveStatements, document, Nothing)

                Assert.False(result.HasChanges)
                Assert.False(result.HasChangesAndErrors)
                Assert.False(result.HasChangesAndCompilationErrors)
            End Using
        End Function

        <Fact>
        Public Async Function AnalyzeDocumentAsync_SyntaxError_NoChange2() As Threading.Tasks.Task
            Dim source1 = "
Class C
    Public Shared Sub Main()
        System.Console.WriteLine(1  ' syntax error
    End Sub
End Class
"
            Dim source2 = "
Class C
    Public Shared Sub Main()
        System.Console.WriteLine(1  ' syntax error
    End Sub
End Class
"

            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(source1)
                Dim documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id
                Dim oldSolution = workspace.CurrentSolution
                Dim newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2))

                Dim baseActiveStatements = ImmutableArray.Create(Of ActiveStatementSpan)()
                Dim result = Await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newSolution.GetDocument(documentId), Nothing)

                Assert.False(result.HasChanges)
                Assert.False(result.HasChangesAndErrors)
                Assert.False(result.HasChangesAndCompilationErrors)
            End Using
        End Function

        <Fact>
        Public Async Function AnalyzeDocumentAsync_SemanticError_NoChange() As Threading.Tasks.Task
            Dim source = "
Class C
    Public Shared Sub Main()
        System.Console.WriteLine(1)
        Bar()
    End Sub
End Class
"

            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(source)
                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim baseActiveStatements = ImmutableArray.Create(Of ActiveStatementSpan)()
                Dim result = Await analyzer.AnalyzeDocumentAsync(workspace.CurrentSolution, baseActiveStatements, document, Nothing)

                Assert.False(result.HasChanges)
                Assert.False(result.HasChangesAndErrors)
                Assert.False(result.HasChangesAndCompilationErrors)
            End Using
        End Function

        <Fact>
        Public Async Function AnalyzeDocumentAsync_SemanticError_Change() As Threading.Tasks.Task
            Dim source1 = "
Class C
    Public Shared Sub Main()
        System.Console.WriteLine(1)
        Bar()
    End Sub
End Class
"
            Dim source2 = "
Class C
    Public Shared Sub Main()
        System.Console.WriteLine(2)
        Bar()
    End Sub
End Class
"

            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(source1)
                Dim documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id
                Dim oldSolution = workspace.CurrentSolution
                Dim newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2))

                Dim baseActiveStatements = ImmutableArray.Create(Of ActiveStatementSpan)()
                Dim result = Await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newSolution.GetDocument(documentId), Nothing)

                Assert.True(result.HasChanges)
                Assert.True(result.HasChangesAndErrors)
                Assert.True(result.HasChangesAndCompilationErrors)
            End Using
        End Function

        <Fact>
        Public Sub FindMemberDeclaration1()
            Dim source = <text>
Class C 
    Inherits D

    Public Sub New()
        MyBase.New()
        Foo()
    End Sub

    Public Sub Foo
    End Sub
End Class
</text>.Value

            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()
            Dim root = SyntaxFactory.ParseCompilationUnit(source)

            Assert.Null(analyzer.FindMemberDeclaration(root, Int32.MaxValue))
            Assert.Null(analyzer.FindMemberDeclaration(root, Int32.MinValue))
        End Sub

        <Fact>
        Public Async Function AnalyzeDocumentAsync_Adding_A_New_File() As Threading.Tasks.Task
            Dim source1 = "
Class C
    Public Shared Sub Main()
    End Sub
End Class
"
            Dim source2 = "
Private Class D
End Class
"
            Dim analyzer = New VisualBasicEditAndContinueAnalyzer()

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(source1)
                ' fork the solution to introduce a change
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim newDocId = Microsoft.CodeAnalysis.DocumentId.CreateNewId(project.Id)
                Dim oldSolution = workspace.CurrentSolution
                Dim newSolution = oldSolution.AddDocument(newDocId, "foo.vb", SourceText.From(source2))

                workspace.TryApplyChanges(newSolution)

                Dim newProject = newSolution.Projects.Single()
                Dim changes = newProject.GetChanges(project)

                Assert.Equal(2, newProject.Documents.Count())
                Assert.Equal(0, changes.GetChangedDocuments().Count())
                Assert.Equal(1, changes.GetAddedDocuments().Count())

                Dim changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments())

                Dim result = New List(Of DocumentAnalysisResults)()
                Dim baseActiveStatements = ImmutableArray.Create(Of ActiveStatementSpan)()
                For Each changedDocumentId In changedDocuments
                    result.Add(Await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newProject.GetDocument(changedDocumentId), Nothing))
                Next

                Assert.True(result.IsSingle())
                Assert.Equal(1, result.Single().RudeEditErrors.Count())
                Assert.Equal(RudeEditKind.InsertFile, result.Single().RudeEditErrors.Single().Kind)
            End Using
        End Function
    End Class
End Namespace
