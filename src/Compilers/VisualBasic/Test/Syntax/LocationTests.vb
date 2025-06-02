' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class LocationTests
        Inherits TestBase

        Private Shared ReadOnly s_resolver As New TestSourceResolver()

        Private Class TestSourceResolver
            Inherits SourceFileResolver

            Public Sub New()
                MyBase.New(ImmutableArray(Of String).Empty, Nothing)
            End Sub

            Public Overrides Function NormalizePath(path As String, basePath As String) As String
                Return String.Format("[{0};{1}]", path, basePath)
            End Function
        End Class

        Private Sub AssertMappedSpanEqual(syntaxTree As SyntaxTree,
                                          sourceText As String,
                                          expectedPath As String,
                                          expectedStartLine As Integer,
                                          expectedStartOffset As Integer,
                                          expectedEndLine As Integer,
                                          expectedEndOffset As Integer,
                                          hasMappedPath As Boolean)

            Dim span = GetSpanIn(syntaxTree, sourceText)
            Dim mappedSpan = syntaxTree.GetMappedLineSpan(span)
            Dim actualDisplayPath = syntaxTree.GetDisplayPath(span, s_resolver)

            Assert.Equal(expectedPath, mappedSpan.Path)
            If expectedPath.IsEmpty Then
                Assert.Equal("", actualDisplayPath)
            Else
                Assert.Equal(String.Format("[{0};{1}]", expectedPath, If(hasMappedPath, syntaxTree.FilePath, Nothing)), actualDisplayPath)
            End If

            Assert.Equal(expectedStartLine, mappedSpan.StartLinePosition.Line)
            Assert.Equal(expectedStartOffset, mappedSpan.StartLinePosition.Character)
            Assert.Equal(expectedEndLine, mappedSpan.EndLinePosition.Line)
            Assert.Equal(expectedEndOffset, mappedSpan.EndLinePosition.Character)
            Assert.Equal(hasMappedPath, mappedSpan.HasMappedPath)
        End Sub

        Private Function GetSpanIn(tree As SyntaxTree, textToFind As String) As TextSpan
            Dim s = tree.GetText().ToString()
            Dim index = s.IndexOf(textToFind, StringComparison.Ordinal)
            Assert.True(index >= 0, "textToFind not found in the tree")
            Return New TextSpan(index, textToFind.Length)
        End Function

        Private Shared Function InspectLineMapping(tree As SyntaxTree) As IEnumerable(Of String)
            Dim text = tree.GetText()
            Return tree.GetLineMappings().Select(Function(mapping) $"[|{text.GetSubText(text.Lines.GetTextSpan(mapping.Span))}|] -> {If(mapping.IsHidden, "<hidden>", mapping.MappedSpan.ToString())}")
        End Function

        <Fact>
        Public Sub TestGetSourceLocationInFile()
            Dim sampleProgram = "Class X" + vbCrLf + "Public x As Integer" + vbCrLf + "End Class" + vbCrLf
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\\goo.vb")

            Dim xSpan As New TextSpan(sampleProgram.IndexOf("x As", StringComparison.Ordinal), 1)
            Dim xToEndClassSpan As New TextSpan(xSpan.Start, sampleProgram.IndexOf("End Class", StringComparison.Ordinal) - xSpan.Start + 3)
            Dim locX As New SourceLocation(tree, xSpan)
            Dim locXToEndClass As New SourceLocation(tree, xToEndClassSpan)

            Dim flpsX = locX.GetLineSpan()
            Assert.Equal("c:\\goo.vb", flpsX.Path)
            Assert.Equal(1, flpsX.StartLinePosition.Line)
            Assert.Equal(7, flpsX.StartLinePosition.Character)
            Assert.Equal(1, flpsX.EndLinePosition.Line)
            Assert.Equal(8, flpsX.EndLinePosition.Character)

            Dim flpsXToEndClass = locXToEndClass.GetLineSpan()
            Assert.Equal("c:\\goo.vb", flpsXToEndClass.Path)
            Assert.Equal(1, flpsXToEndClass.StartLinePosition.Line)
            Assert.Equal(7, flpsXToEndClass.StartLinePosition.Character)
            Assert.Equal(2, flpsXToEndClass.EndLinePosition.Line)
            Assert.Equal(3, flpsXToEndClass.EndLinePosition.Character)
        End Sub

        <Fact>
        Public Sub TestLineMapping()
            Dim sampleProgram =
"Imports System
Class X
#ExternalSource(""banana.vb"", 20)
public x as integer
public y as integer
#End ExternalSource
#ExternalSource(""banana.vb"", 44)
public z as integer
#End ExternalSource
public w as integer
public q as integer
#If False Then
#ExternalSource(""apple.vb"", 101)
#End If
public a as integer
#If False Then
#End ExternalSource
#End If
End Class
".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            AssertMappedSpanEqual(tree, "ports Sy", "c:\goo.vb", 0, 2, 0, 10, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "x as", "banana.vb", 19, 7, 19, 11, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "y as", "banana.vb", 20, 7, 20, 11, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "z as", "banana.vb", 43, 7, 43, 11, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "w as", "c:\goo.vb", 9, 7, 9, 11, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "q as", "c:\goo.vb", 10, 7, 10, 11, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "a as", "c:\goo.vb", 14, 7, 14, 11, hasMappedPath:=False)

            AssertEx.Equal(
            {
                "[|Imports System" & vbCrLf & "Class X" & vbCrLf & "|] -> <hidden>",
                "[|public x as integer" & vbCrLf & "public y as integer" & vbCrLf & "|] -> banana.vb: (19,0)-(20,21)",
                "[|public z as integer" & vbCrLf & "|] -> banana.vb: (43,0)-(43,21)",
                "[|public w as integer" & vbCrLf &
                "public q as integer" & vbCrLf &
                "#If False Then" & vbCrLf &
                "#ExternalSource(""apple.vb"", 101)" & vbCrLf &
                "#End If" & vbCrLf &
                "public a as integer" & vbCrLf &
                "#If False Then" & vbCrLf &
                "#End ExternalSource" & vbCrLf &
                "#End If" & vbCrLf &
                "End Class" & vbCrLf & "|] -> <hidden>"
            }, InspectLineMapping(tree))
        End Sub

        <Fact>
        Public Sub TestLineMapping_Invalid_MissingStartDirective1()
            Dim sampleProgram =
"Class X
#End ExternalSource
End Class
".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            AssertMappedSpanEqual(tree, "Class X", "c:\goo.vb", 0, 0, 0, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "End Class", "c:\goo.vb", 2, 0, 2, 9, hasMappedPath:=False)

            AssertEx.Equal(
            {
                "[|Class X" & vbCrLf & "|] -> : (0,0)-(0,9)",
                "[|End Class" & vbCrLf & "|] -> : (2,0)-(3,0)"
            }, InspectLineMapping(tree))
        End Sub

        <Fact>
        Public Sub TestLineMapping_Invalid_MissingStartDirective3()
            Dim sampleProgram =
"Class X
#End ExternalSource
Class Y
#End ExternalSource
Class Z
End Class
End Class
End Class
".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            AssertMappedSpanEqual(tree, "Class X", "c:\goo.vb", 0, 0, 0, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "Class Y", "c:\goo.vb", 2, 0, 2, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "Class Z", "c:\goo.vb", 4, 0, 4, 7, hasMappedPath:=False)

            AssertEx.Equal(
            {
                "[|Class X" & vbCrLf & "|] -> : (0,0)-(0,9)",
                "[|Class Y" & vbCrLf & "|] -> : (2,0)-(2,9)",
                "[|Class Z" & vbCrLf & "End Class" & vbCrLf & "End Class" & vbCrLf & "End Class" & vbCrLf & "|] -> : (4,0)-(8,0)"
            }, InspectLineMapping(tree))
        End Sub

        <Fact>
        Public Sub TestLineMapping_Invalid_MissingEndDirective1()
            Dim sampleProgram =
"Class X
#ExternalSource(""a.vb"", 20)
End Class
".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            AssertMappedSpanEqual(tree, "Class X", "c:\goo.vb", 0, 0, 0, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "End Class", "a.vb", 19, 0, 19, 9, hasMappedPath:=True)

            AssertEx.Equal(
            {
                "[|Class X" & vbCrLf & "|] -> : (0,0)-(0,9)",
                "[|End Class" & vbCrLf & "|] -> a.vb: (19,0)-(20,0)"
            }, InspectLineMapping(tree))
        End Sub

        <Fact>
        Public Sub TestLineMapping_Invalid_MissingEndDirective2()
            Dim sampleProgram =
"Class X
#ExternalSource(""a.vb"", 20)
Class Y
End Class
#ExternalSource(""b.vb"", 20)
Class Z
End Class
End Class
".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            AssertMappedSpanEqual(tree, "Class X", "c:\goo.vb", 0, 0, 0, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "Class Y", "a.vb", 19, 0, 19, 7, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "Class Z", "b.vb", 19, 0, 19, 7, hasMappedPath:=True)

            AssertEx.Equal(
            {
                "[|Class X" & vbCrLf & "|] -> : (0,0)-(0,9)",
                "[|Class Y" & vbCrLf & "End Class" & vbCrLf & "|] -> a.vb: (19,0)-(20,11)",
                "[|Class Z" & vbCrLf & "End Class" & vbCrLf & "End Class" & vbCrLf & "|] -> b.vb: (19,0)-(22,0)"
            }, InspectLineMapping(tree))
        End Sub

        <Fact>
        Public Sub TestLineMapping_Invalid_MissingEndDirective_LastLine()
            Dim sampleProgram = "#End ExternalSource"
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            Assert.Empty(InspectLineMapping(tree))
        End Sub

        <Fact>
        Public Sub TestLineMappingNoDirectives()
            Dim sampleProgram =
"Imports System
Class X
public x as integer
public y as integer
End Class
".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\goo.vb")

            AssertMappedSpanEqual(tree, "ports Sy", "c:\goo.vb", 0, 2, 0, 10, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "x as", "c:\goo.vb", 2, 7, 2, 11, hasMappedPath:=False)

            Dim text = tree.GetText()
            Assert.Empty(tree.GetLineMappings())
        End Sub

        <Fact>
        Public Sub TestLineMapping_NoSyntaxTreePath()
            Dim sampleProgram = "
Class X
End Class
".NormalizeLineEndings()
            AssertMappedSpanEqual(SyntaxFactory.ParseSyntaxTree(sampleProgram, path:=""), "Class X", "", 1, 0, 1, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(SyntaxFactory.ParseSyntaxTree(sampleProgram, path:="    "), "Class X", "    ", 1, 0, 1, 7, hasMappedPath:=False)
        End Sub

        <Fact()>
        Public Sub TestEqualSourceLocations()
            Dim sampleProgram = "class
end class".NormalizeLineEndings()
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(sampleProgram)
            Dim loc1 As SourceLocation = New SourceLocation(tree, New TextSpan(3, 4))
            Dim loc2 As SourceLocation = New SourceLocation(tree, New TextSpan(3, 4))
            Dim loc3 As SourceLocation = New SourceLocation(tree, New TextSpan(3, 7))
            Dim loc4 As SourceLocation = New SourceLocation(tree2, New TextSpan(3, 4))
            Assert.Equal(loc1, loc2)
            Assert.Equal(loc1.GetHashCode(), loc2.GetHashCode())
            Assert.NotEqual(loc1, loc3)
            Assert.NotEqual(loc3, loc4)
        End Sub

        <Fact(), WorkItem(537926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537926"), WorkItem(545223, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545223")>
        Public Sub TestSourceLocationToString()
            Dim sampleProgram = "Imports System
Class Test
#ExternalSource(""d:\banana.vb"", 20)
    public x As integer
#End ExternalSource
    public y As String
End Class".NormalizeLineEndings()

            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram)
            Dim span1 As New TextSpan(sampleProgram.IndexOf("x As", StringComparison.Ordinal), 1)
            Dim span2 As New TextSpan(sampleProgram.IndexOf("y As", StringComparison.Ordinal), 1)

            Dim loc1 = New SourceLocation(tree, span1)
            Dim loc2 = New SourceLocation(tree, span2)

            'Assert.Equal("SourceLocation(@4:12)""x""", loc1.DebugView)
            Assert.Equal("SourceFile([76..77))", loc1.ToString) 'use the override in Location
            'Assert.Equal("SourceLocation(@6:12)""y""", loc2.DebugView)
            Assert.Equal("SourceFile([122..123))", loc2.ToString)
        End Sub

    End Class
End Namespace

