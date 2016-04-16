' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <Fact>
        Public Sub TestGetSourceLocationInFile()
            Dim sampleProgram = "Class X" + vbCrLf + "Public x As Integer" + vbCrLf + "End Class" + vbCrLf
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\\foo.vb")

            Dim xSpan As New TextSpan(sampleProgram.IndexOf("x As", StringComparison.Ordinal), 1)
            Dim xToEndClassSpan As New TextSpan(xSpan.Start, sampleProgram.IndexOf("End Class", StringComparison.Ordinal) - xSpan.Start + 3)
            Dim locX As New SourceLocation(tree, xSpan)
            Dim locXToEndClass As New SourceLocation(tree, xToEndClassSpan)

            Dim flpsX = locX.GetLineSpan()
            Assert.Equal("c:\\foo.vb", flpsX.Path)
            Assert.Equal(1, flpsX.StartLinePosition.Line)
            Assert.Equal(7, flpsX.StartLinePosition.Character)
            Assert.Equal(1, flpsX.EndLinePosition.Line)
            Assert.Equal(8, flpsX.EndLinePosition.Character)

            Dim flpsXToEndClass = locXToEndClass.GetLineSpan()
            Assert.Equal("c:\\foo.vb", flpsXToEndClass.Path)
            Assert.Equal(1, flpsXToEndClass.StartLinePosition.Line)
            Assert.Equal(7, flpsXToEndClass.StartLinePosition.Character)
            Assert.Equal(2, flpsXToEndClass.EndLinePosition.Line)
            Assert.Equal(3, flpsXToEndClass.EndLinePosition.Character)
        End Sub

        <Fact>
        Public Sub TestLineMapping()
            Dim sampleProgram As String =
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
"
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\foo.vb")

            AssertMappedSpanEqual(tree, "ports Sy", "c:\foo.vb", 0, 2, 0, 10, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "x as", "banana.vb", 19, 7, 19, 11, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "y as", "banana.vb", 20, 7, 20, 11, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "z as", "banana.vb", 43, 7, 43, 11, hasMappedPath:=True)
            AssertMappedSpanEqual(tree, "w as", "c:\foo.vb", 9, 7, 9, 11, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "q as", "c:\foo.vb", 10, 7, 10, 11, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "a as", "c:\foo.vb", 14, 7, 14, 11, hasMappedPath:=False)
        End Sub

        <Fact()>
        Public Sub TestLineMappingNoDirectives()
            Dim sampleProgram As String =
<value>Imports System
Class X
public x as integer
public y as integer
End Class
</value>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram, path:="c:\foo.vb")

            AssertMappedSpanEqual(tree, "ports Sy", "c:\foo.vb", 0, 2, 0, 10, hasMappedPath:=False)
            AssertMappedSpanEqual(tree, "x as", "c:\foo.vb", 2, 7, 2, 11, hasMappedPath:=False)

        End Sub

        <Fact>
        Public Sub TestLineMapping_NoSyntaxTreePath()
            Dim sampleProgram As String = "
Class X
End Class
"
            Dim resolver = New TestSourceResolver()
            AssertMappedSpanEqual(SyntaxFactory.ParseSyntaxTree(sampleProgram, path:=""), "Class X", "", 1, 0, 1, 7, hasMappedPath:=False)
            AssertMappedSpanEqual(SyntaxFactory.ParseSyntaxTree(sampleProgram, path:="    "), "Class X", "    ", 1, 0, 1, 7, hasMappedPath:=False)
        End Sub


        <Fact()>
        Public Sub TestEqualSourceLocations()
            Dim sampleProgram As String = <text> class
end class </text>.Value
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
            Dim sampleProgram As String = <text>Imports System
Class Test
#ExternalSource("d:\banana.vb", 20)
    public x As integer
#End ExternalSource
    public y As String
End Class</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(sampleProgram)
            Dim span1 As New TextSpan(sampleProgram.IndexOf("x As", StringComparison.Ordinal), 1)
            Dim span2 As New TextSpan(sampleProgram.IndexOf("y As", StringComparison.Ordinal), 1)

            Dim loc1 = New SourceLocation(tree, span1)
            Dim loc2 = New SourceLocation(tree, span2)

            'Assert.Equal("SourceLocation(@4:12)""x""", loc1.DebugView)
            Assert.Equal("SourceFile([73..74))", loc1.ToString) 'use the override in Location
            'Assert.Equal("SourceLocation(@6:12)""y""", loc2.DebugView)
            Assert.Equal("SourceFile([117..118))", loc2.ToString)
        End Sub

    End Class
End Namespace

