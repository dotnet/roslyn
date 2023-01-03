' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestHelpers

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class VisualBasicSyntaxTreeTests
        <Fact>
        Public Sub Create()
            Dim root = SyntaxFactory.ParseCompilationUnit("")

            Dim tree = VisualBasicSyntaxTree.Create(root)
            Assert.Equal(SourceHashAlgorithm.Sha1, tree.GetText().ChecksumAlgorithm)
        End Sub

        ' Diagnostic options on syntax trees are now obsolete
#Disable Warning BC40000
        <Fact>
        Public Sub Create_WithDiagnosticOptions()
            Dim options = CreateImmutableDictionary(("BC000", ReportDiagnostic.Suppress))
            Dim tree = VisualBasicSyntaxTree.Create(SyntaxFactory.ParseCompilationUnit(""), options:=Nothing, path:=Nothing, encoding:=Nothing, diagnosticOptions:=options)
            Assert.Same(options, tree.DiagnosticOptions)
        End Sub

        <Fact>
        Public Sub ParseTreeWithChangesPreservesDiagnosticOptions()
            Dim options = CreateImmutableDictionary(("BC000", ReportDiagnostic.Suppress))
            Dim tree = VisualBasicSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions:=options)
            Assert.Same(options, tree.DiagnosticOptions)
            Dim newTree = tree.WithChangedText(SourceText.From("Class B : End Class"))
            Assert.Same(options, newTree.DiagnosticOptions)
        End Sub

        <Fact>
        Public Sub ParseTreeNullDiagnosticOptions()
            Dim tree = VisualBasicSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions:=Nothing)
            Assert.NotNull(tree.DiagnosticOptions)
            Assert.True(tree.DiagnosticOptions.IsEmpty)
            ' The default options are case insensitive but the default empty ImmutableDictionary is not
            Assert.NotSame(ImmutableDictionary(Of String, ReportDiagnostic).Empty, tree.DiagnosticOptions)
        End Sub

        <Fact>
        Public Sub ParseTreeEmptyDiagnosticOptions()
            Dim tree = VisualBasicSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions:=ImmutableDictionary(Of String, ReportDiagnostic).Empty)
            Assert.NotNull(tree.DiagnosticOptions)
            Assert.True(tree.DiagnosticOptions.IsEmpty)
            Assert.Same(ImmutableDictionary(Of String, ReportDiagnostic).Empty, tree.DiagnosticOptions)
        End Sub

        <Fact>
        Public Sub ParseTreeCustomDiagnosticOptions()
            Dim options = CreateImmutableDictionary(("BC000", ReportDiagnostic.Suppress))
            Dim tree = VisualBasicSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions:=options)
            Assert.Same(options, tree.DiagnosticOptions)
        End Sub

        <Fact>
        Public Sub DefaultTreeDiagnosticOptions()
            Dim tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit())
            Assert.NotNull(tree.DiagnosticOptions)
            Assert.True(tree.DiagnosticOptions.IsEmpty)
        End Sub

        <Fact>
        Public Sub WithDiagnosticOptionsNull()
            Dim tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit())
            Dim newTree = tree.WithDiagnosticOptions(Nothing)
            Assert.NotNull(newTree.DiagnosticOptions)
            Assert.True(newTree.DiagnosticOptions.IsEmpty)
            Assert.Same(tree, newTree)
        End Sub

        <Fact>
        Public Sub WithDiagnosticOptionsEmpty()
            Dim tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit())
            Dim newTree = tree.WithDiagnosticOptions(ImmutableDictionary(Of String, ReportDiagnostic).Empty)
            Assert.NotNull(tree.DiagnosticOptions)
            Assert.True(newTree.DiagnosticOptions.IsEmpty)
            ' Default empty immutable dictionary is not case insensitive
            Assert.NotSame(tree.DiagnosticOptions, newTree.DiagnosticOptions)
        End Sub

        <Fact>
        Public Sub PerTreeDiagnosticOptionsNewDict()
            Dim tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit())
            Dim map = CreateImmutableDictionary(("BC000", ReportDiagnostic.Suppress))
            Dim newTree = tree.WithDiagnosticOptions(map)
            Assert.NotNull(newTree.DiagnosticOptions)
            Assert.Same(map, newTree.DiagnosticOptions)
            Assert.NotEqual(tree, newTree)
        End Sub
#Enable Warning BC40000

        <Fact>
        Public Sub WithRootAndOptions_ParsedTree()
            Dim oldTree = SyntaxFactory.ParseSyntaxTree("Class B : End Class")
            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = oldTree.WithRootAndOptions(newRoot, newOptions)
            Dim newText = newTree.GetText()

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)

            Assert.Null(newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithRootAndOptions_ParsedTreeWithText()
            Dim oldText = SourceText.From("Class B : End Class", Encoding.Unicode, SourceHashAlgorithms.Default)
            Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText)

            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = oldTree.WithRootAndOptions(newRoot, newOptions)
            Dim newText = newTree.GetText()

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)

            Assert.Same(Encoding.Unicode, newText.Encoding)
            Assert.Equal(SourceHashAlgorithms.Default, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithRootAndOptions_DummyTree()
            Dim dummy = New VisualBasicSyntaxTree.DummySyntaxTree()
            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = dummy.WithRootAndOptions(newRoot, newOptions)
            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)
        End Sub

        <Fact>
        Public Sub WithFilePath_ParsedTree()
            Dim oldTree = SyntaxFactory.ParseSyntaxTree("Class B : End Class", path:="old.vb")
            Dim newTree = oldTree.WithFilePath("new.vb")
            Dim newText = newTree.GetText()

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())

            Assert.Null(newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithFilePath_ParsedTreeWithText()
            Dim oldText = SourceText.From("Class B : End Class", Encoding.Unicode, SourceHashAlgorithms.Default)
            Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText, path:="old.vb")
            Dim newTree = oldTree.WithFilePath("new.vb")
            Dim newText = newTree.GetText()

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())

            Assert.Same(Encoding.Unicode, newText.Encoding)
            Assert.Equal(SourceHashAlgorithms.Default, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithFilePath_DummyTree()
            Dim oldTree = New VisualBasicSyntaxTree.DummySyntaxTree()
            Dim newTree = oldTree.WithFilePath("new.vb")

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())
        End Sub

        <Fact, WorkItem(12638, "https://github.com/dotnet/roslyn/issues/12638")>
        Public Sub WithFilePath_Nothing()
            Dim oldTree As SyntaxTree = New VisualBasicSyntaxTree.DummySyntaxTree()
            Assert.Equal(String.Empty, oldTree.WithFilePath(Nothing).FilePath)
            oldTree = SyntaxFactory.ParseSyntaxTree("", path:="old.vb")
            Assert.Equal(String.Empty, oldTree.WithFilePath(Nothing).FilePath)
            Assert.Equal(String.Empty, SyntaxFactory.ParseSyntaxTree("", path:=Nothing).FilePath)
            Assert.Equal(String.Empty, VisualBasicSyntaxTree.Create(CType(oldTree.GetRoot, VisualBasicSyntaxNode), path:=Nothing).FilePath)
        End Sub
    End Class
End Namespace
