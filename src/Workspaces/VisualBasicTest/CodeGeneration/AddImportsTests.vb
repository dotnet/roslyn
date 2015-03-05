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
            code = code.Replace(vbLf, vbCrLf)

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
                importsAddedText = importsAddedText.Replace(vbLf, vbCrLf)
                Dim formatted = Formatter.FormatAsync(imported, SyntaxAnnotation.ElasticAnnotation, options).Result
                Dim actualText = formatted.GetTextAsync().Result.ToString()
                Assert.Equal(importsAddedText, actualText)
            End If

            If simplifiedText IsNot Nothing Then
                simplifiedText = simplifiedText.Replace(vbLf, vbCrLf)
                Dim reduced = Simplifier.ReduceAsync(imported, options).Result
                Dim formatted = Formatter.FormatAsync(reduced, SyntaxAnnotation.ElasticAnnotation, options).Result
                Dim actualText = formatted.GetTextAsync().Result.ToString()
                Assert.Equal(simplifiedText, actualText)
            End If
        End Sub

        <Fact>
        Public Sub TestAddImport()
            Test(
<x>Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAddSystemImportFirst()
            Test(
<x>Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections.Generic
Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections.Generic
Imports N

Class C
    Public F As List(Of Integer)
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestDontAddSystemImportFirst()
            Test(
<x>Imports N

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports N
Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports N
Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class</x>.Value,
_ws.Options.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic, False))
        End Sub

        <Fact>
        Public Sub TestAddImportsInOrder()
            Test(
<x>Imports System.Collections
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As List(Of Integer)
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestAddMultipleImportsInOrder()
            Test(
<x>Imports System.Collections
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
    Public Handler As System.EventHandler
End Class</x>.Value,
<x>Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As System.Collections.Generic.List(Of Integer)
    Public Handler As System.EventHandler
End Class</x>.Value,
<x>Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics

Class C
    Public F As List(Of Integer)
    Public Handler As EventHandler
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestImportNotAddedAgainIfAlreadyExists()
            Test(
<x>Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections.Generic

Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Imports System.Collections.Generic

Class C
    Public F As List(Of Integer)
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestUnusedAddedImportIsRemovedBySimplifier()
            Test(
<x>Class C
    Public F As System.Int32
End Class</x>.Value,
<x>Imports System

Class C
    Public F As System.Int32
End Class</x>.Value,
<x>Class C
    Public F As Integer
End Class</x>.Value)
        End Sub

        <Fact>
        Public Sub TestImportNotAddedIfGloballyImported()
            Test(
<x>Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Class C
    Public F As System.Collections.Generic.List(Of Integer)
End Class</x>.Value,
<x>Class C
    Public F As List(Of Integer)
End Class</x>.Value,
globalImports:={"System.Collections.Generic"})

        End Sub

        <Fact>
        Public Sub TestImportNotAddedForNamespaceDeclarations()
            Test(
<x>Namespace N
End Namespace</x>.Value,
<x>Namespace N
End Namespace</x>.Value,
<x>Namespace N
End Namespace</x>.Value)
        End Sub

        <Fact>
        Public Sub TestImportAddedAndRemovedForReferencesInsideNamespaceDeclarations()
            Test(
<x>Namespace N
    Class C
        Private _c As N.C
    End Class
End Namespace</x>.Value,
<x>Imports N

Namespace N
    Class C
        Private _c As N.C
    End Class
End Namespace</x>.Value,
<x>Namespace N
    Class C
        Private _c As C
    End Class
End Namespace</x>.Value)
        End Sub

        <Fact>
        Public Sub TestRemoveImportIfItMakesReferencesAmbiguous()
            ' this Is not really an artifact of the AddImports feature, it is due
            ' to Simplifier not reducing the namespace reference because it would 
            ' become ambiguous, thus leaving an unused imports statement

            Test(
<x>Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
</x>.Value,
<x>Imports N

Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
</x>.Value,
<x>Namespace N
    Class C
    End Class
End Namespace

Class C
    Private F As N.C
End Class
</x>.Value)
        End Sub

    End Class
End Namespace
