' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Editting
    Public Class AddImportsTests
        Private ReadOnly _ws As AdhocWorkspace = New AdhocWorkspace()
        Private ReadOnly _emptyProject As Project

        Public Sub New()
            _emptyProject = _ws.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    "test",
                    "test.dll",
                    LanguageNames.VisualBasic,
                    metadataReferences:={TestReferences.NetFx.v4_0_30319.mscorlib}))
        End Sub

        Private Function GetDocument(code As String, Optional globalImports As String() = Nothing) As Document
            Dim project = _emptyProject

            If globalImports IsNot Nothing Then
                Dim gi = GlobalImport.Parse(globalImports)
                project = project.WithCompilationOptions(DirectCast(project.CompilationOptions, VisualBasicCompilationOptions).WithGlobalImports(gi))
            End If

            Return project.AddDocument("test.cs", code)
        End Function

        Private Sub Test(initialText As String, importsAddedText As String, simplifiedText As String, Optional options As OptionSet = Nothing, Optional globalImports As String() = Nothing)

            Dim doc = GetDocument(initialText, globalImports)
            options = If(options, doc.Project.Solution.Workspace.Options)

            Dim imported = ImportAdder.AddImportsAsync(doc, options).Result

            If importsAddedText IsNot Nothing Then
                Dim formatted = Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation, options).Result
                Dim actualText = formatted.GetTextAsync().Result.ToString()
                Assert.Equal(importsAddedText, actualText)
            End If

            If simplifiedText IsNot Nothing Then
                Dim reduced = Simplifier.ReduceAsync(imported, options).Result
                Dim formatted = Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation, options).Result
                Dim actualText = formatted.GetTextAsync().Result.ToString()
                Assert.Equal(simplifiedText, actualText)
            End If
        End Sub

        <Fact>
        Public Sub TestAddImport()
            Test(
"Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class")
        End Sub

        <Fact>
        Public Sub TestAddSystemImportFirst()
            Test(
"Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic
Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic
Imports N

Class C
    Public F As List(Of Integer)
End Class")
        End Sub

        <Fact>
        Public Sub TestDontAddSystemImportFirst()
            Test(
"Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports N
Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports N
Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class",
_ws.Options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic, False))
        End Sub

        <Fact>
        Public Sub TestAddImportsInOrder()
            Test(
"Imports System.Collections
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As List(Of Integer)
End Class")
        End Sub

        <Fact>
        Public Sub TestAddMultipleImportsInOrder()
            Test(
"Imports System.Collections
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
    Public Handler As System.EventHandler
End Class",
"Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
    Public Handler As System.EventHandler
End Class",
"Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As List(Of Integer)
    Public Handler As EventHandler
End Class")
        End Sub

        <Fact>
        Public Sub TestImportNotAddedAgainIfAlreadyExists()
            Test(
"Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class")
        End Sub

        <Fact>
        Public Sub TestUnusedAddedImportIsRemovedBySimplifier()
            Test(
"Class C
    Public F As System.Int32
End Class",
"Imports System

Class C
    Public F As System.Int32
End Class",
"Class C
    Public F As Integer
End Class")
        End Sub

        <Fact>
        Public Sub TestImportNotAddedIfGloballyImported()
            Test(
    "Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
    "Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class",
    "Class C
    Public F As List(Of Integer)
End Class",
    globalImports:={"System.Collections.Generic"})

        End Sub

        <Fact>
        Public Sub TestImportNotAddedForNamespaceDeclarations()
            Test(
"Namespace N
End Namespace",
"Namespace N
End Namespace",
"Namespace N
End Namespace")
        End Sub

        <Fact>
        Public Sub TestImportAddedAndRemovedForReferencesInsideNamespaceDeclarations()
            Test(
        "Namespace N
    Class C
        Private _c As N.C
    End Class
End Namespace",
        "Imports N

Namespace N
    Class C
        Private _c As N.C
    End Class
End Namespace",
        "Namespace N
    Class C
        Private _c As C
    End Class
End Namespace")
        End Sub

        <Fact>
        Public Sub TestRemoveImportIfItMakesReferencesAmbiguous()
            ' this is not really an artifact of the AddImports feature, it is due
            ' to Simplifier not reducing the namespace reference because it would 
            ' become ambiguous, thus leaving an unused imports statement

            Test(
"Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
",
"Imports N

Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
",
"Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
")
        End Sub

        Private Sub TestPartialNamespacesNotUsed()
            Test(
"Imports System.Collections

Public Class C
    Public F1 As ArrayList
    Public F2 As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic

Public Class C
    Public F1 As ArrayList
    Public F2 As System.Collections.Generic.List(Of Integer)
End Class",
"Imports System.Collections
Imports System.Collections.Generic

Public Class C
    Public F1 As ArrayList
    Public F2 As List(Of Integer)
End Class")
        End Sub
    End Class
End Namespace
