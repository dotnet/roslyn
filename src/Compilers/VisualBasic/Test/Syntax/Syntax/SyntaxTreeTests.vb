﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class VisualBasicSyntaxTreeTests
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
            Dim oldText = SourceText.From("Class B : End Class", Encoding.UTF7, SourceHashAlgorithm.Sha256)
            Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText)

            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = oldTree.WithRootAndOptions(newRoot, newOptions)
            Dim newText = newTree.GetText()

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)

            Assert.Same(Encoding.UTF7, newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm)
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
            Dim oldText = SourceText.From("Class B : End Class", Encoding.UTF7, SourceHashAlgorithm.Sha256)
            Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText, path:="old.vb")
            Dim newTree = oldTree.WithFilePath("new.vb")
            Dim newText = newTree.GetText()

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())

            Assert.Same(Encoding.UTF7, newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm)
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
