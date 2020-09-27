' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.SolutionCrawler

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting

    <UseExportProvider>
    Public Class CodeCleanUpTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicRemoveImports() As Task
            Dim code = "Imports System.Collections.Generic
Imports System
Friend Class Program
    Shared Sub Main(args() As String)
        Console.WriteLine(list.Count)
    End Sub
End Class
"

            Dim expected = "Friend Class Program
    Shared Sub Main(args() As String)
        Console.WriteLine(list.Count)
    End Sub
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicSortImports() As Task
            Dim code = "Imports System.Reflection
Imports System.IO
Friend Class Program
    Shared Sub Main(args() As String)
        Dim location As ImmutableArray(Of String) = Assembly.Load(""System.Windows.Forms"").Location
        Dim SourceText As String
        Using myFileStream As FileStream = File.OpenRead(location)
            SourceText = myFileStream.GetFileTextFromStream()
        End Using
    End Sub
End Class
"

            Dim expected = "Imports System.IO
Imports System.Reflection
Friend Class Program
    Shared Sub Main(args() As String)
        Dim location As ImmutableArray(Of String) = Assembly.Load(""System.Windows.Forms"").Location
        Dim SourceText As String
        Using myFileStream As FileStream = File.OpenRead(location)
            SourceText = myFileStream.GetFileTextFromStream()
        End Using
    End Sub
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact, WorkItem(36984, "https://github.com/dotnet/roslyn/issues/36984")>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicGroupUsings() As Task
            Dim code As String = "Imports M
Imports System.IO
Friend NotInheritable Class Program
    Private Shared Sub Main(args As String())
        Dim location As ImmutableArray(Of String) = Assembly.Load(""System.Windows.Forms"").Location
        Dim SourceText As String
        Using myFileStream As FileStream = File.OpenRead(location)
            SourceText = myFileStream.GetFileTextFromStream()
        End Using

        Dim tempVar As New Goo
    End Sub
End Class

Namespace M
    Public Class Goo
    End Class
End Namespace
"

            Dim expected As String = "Imports M

Imports System.IO
Friend NotInheritable Class Program
    Private Shared Sub Main(args As String())
        Dim location As ImmutableArray(Of String) = Assembly.Load(""System.Windows.Forms"").Location
        Dim SourceText As String
        Using myFileStream As FileStream = File.OpenRead(location)
            SourceText = myFileStream.GetFileTextFromStream()
        End Using

        Dim tempVar As New Goo
    End Sub
End Class

Namespace M
    Public Class Goo
    End Class
End Namespace
"
            Return AssertCodeCleanupResultAsync(expected, code, systemImportsFirst:=False, separateImportsGroups:=True)
        End Function

        <Fact, WorkItem(36984, "https://github.com/dotnet/roslyn/issues/36984")>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicSortAndGroupUsings() As Task
            Dim code As String = "Imports M

Imports System.IO
Friend NotInheritable Class Program
    Private Shared Sub Main(args As String())
        Dim location As ImmutableArray(Of String) = Assembly.Load(""System.Windows.Forms"").Location
        Dim SourceText As String
        Using myFileStream As FileStream = File.OpenRead(location)
            SourceText = myFileStream.GetFileTextFromStream()
        End Using

        Dim tempVar As New Goo
    End Sub
End Class

Namespace M
    Public Class Goo
    End Class
End Namespace
"

            Dim expected As String = "Imports System.IO

Imports M
Friend NotInheritable Class Program
    Private Shared Sub Main(args As String())
        Dim location As ImmutableArray(Of String) = Assembly.Load(""System.Windows.Forms"").Location
        Dim SourceText As String
        Using myFileStream As FileStream = File.OpenRead(location)
            SourceText = myFileStream.GetFileTextFromStream()
        End Using

        Dim tempVar As New Goo
    End Sub
End Class

Namespace M
    Public Class Goo
    End Class
End Namespace
"
            Return AssertCodeCleanupResultAsync(expected, code, systemImportsFirst:=True, separateImportsGroups:=True)
        End Function

        <Fact(Skip:="IFixAllGetFixesService is missing")>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicRemoveUnusedVariable() As Task
            Dim code As String = "Public Class Program
    Public Shared Sub Method()
        Dim i as integer
    End Sub
End Class
"
            Dim expected As String = "Public Class Program
    Public Shared Sub Method()
    End Sub
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact(Skip:="Not implemented")>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicRemoveUnusedMember() As Task
            Dim code As String = "Friend Class Program
    Private Sub Method()
    End Sub
End Class
"
            Dim expected As String = "Friend Class Program
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact(Skip:="Not implemented")>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicFixAccessibilityModifiers() As Task
            Dim code As String = "Class Program
    Public Shared Sub Method()
        Console.WriteLine(""Hello"")
    End Sub
End Class
"
            Dim expected As String = "Friend Class Program
        Public Shared Sub Method()
            Console.WriteLine(""Hello"")
        End Sub
    End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        ''' <summary>
        ''' Assert the expected code value equals the actual processed input <paramref name="code"/>.
        ''' </summary>
        ''' <param name="expected">The actual processed code to verify against.</param>
        ''' <param name="code">The input code to be processed and tested.</param>
        ''' <param name="systemImportsFirst">Indicates whether <c><see cref="System"/>.*</c> '<c>using</c>' directives should preceded others. Default is <c>true</c>.</param>
        ''' <param name="separateImportsGroups">Indicates whether '<c>using</c>' directives should be organized into separated groups. Default is <c>true</c>.</param>
        ''' <returns>The <see cref="Task"/> to test code cleanup.</returns>
        Private Protected Shared Async Function AssertCodeCleanupResultAsync(expected As String,
                                                                             code As String,
                                                                             Optional systemImportsFirst As Boolean = True,
                                                                             Optional separateImportsGroups As Boolean = False) As Task
            Dim workspace = TestWorkspace.CreateVisualBasic(code)

            Dim solution = workspace.CurrentSolution _
            .WithOptions(workspace.Options _
            .WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic, systemImportsFirst) _
            .WithChangedOption(GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.VisualBasic, separateImportsGroups)) _
            .WithAnalyzerReferences({
            New AnalyzerFileReference(GetType(VisualBasicCompilerDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)})

            workspace.TryApplyChanges(solution)

            ' register this workspace to solution crawler so that analyzer service associate itself with given workspace
            Dim incrementalAnalyzerProvider = TryCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticAnalyzerService)(), IIncrementalAnalyzerProvider)
            incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace)

            Dim hostdoc = workspace.Documents.[Single]()
            Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

            Dim codeCleanupService = document.GetLanguageService(Of ICodeCleanupService)()

            Dim enabledDiagnostics = codeCleanupService.GetAllDiagnostics()

            Dim newDoc = Await codeCleanupService.CleanupAsync(
        document, enabledDiagnostics, New ProgressTracker, CancellationToken.None)

            Dim actual = Await newDoc.GetTextAsync()

            Assert.Equal(expected, actual.ToString())
        End Function

    End Class

End Namespace
