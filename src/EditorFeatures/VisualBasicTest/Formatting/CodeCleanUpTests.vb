' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    <UseExportProvider>
    Public Class CodeCleanUpTests
        ' Format Document tests are handled by Format Document Test

        ' TESTS NEEDED but not found in C#
        'Apply object preference initialization preferences
        'Apply file header preferences

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicRemoveUnusedImports() As Task
            Dim code = "Imports System.Collections.Generic
Imports System
Friend Class Program
    Public Shared Sub Main(args() As String)
        Console.WriteLine(list.Count)
    End Sub
End Class
"

            Dim expected = "Friend Class Program
    Public Shared Sub Main(args() As String)
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
    Public Shared Sub Main(args() As String)
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
    Public Shared Sub Main(args() As String)
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

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicGroupUsings() As Task
            'Apply imports directive placement preference
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

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicSortAndGroupUsings() As Task
            'Apply imports directive placement preference
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

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicRemoveUnusedVariable() As Task
            'Remove unused variables
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
        Public Function VisualBasicRemovePrivateMemberIfUnused() As Task
            Dim code As String = "Friend Class Program
    Private Shared Sub Method()
    End Sub
End Class
"
            Dim expected As String = "Friend Class Program
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicAddAccessibilityModifiers() As Task
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

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicRemoveUnnecessaryCast() As Task
            Dim code As String = "Public Class Program
    Public Shared Sub Method()
        Dim s as string = CStr(""Hello"")
        Console.WriteLine(s)
    End Sub
End Class
"
            Dim expected As String = "Public Class Program
    Public Shared Sub Method()
        Dim s as string = ""Hello""
        Console.WriteLine(s)
    End Sub
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Shared Function VisualBasicSortAccessibilityModifiers() As Task
            Dim code As String = "Public Class Program
    Shared Public Sub Method()
        Console.WriteLine(""Hello"")
    End Sub
End Class
"
            Dim expected As String = "Public Class Program
    Public Shared Sub Method()
        Console.WriteLine(""Hello"")
    End Sub
End Class
"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Function VisualBasicMakePrivateFieldReadOnly() As Task
            Dim code = "Friend Class Program
    Private _a() As String

    Public Sub New(args() As String)
        If _a.Length = 0 Then Throw New ArgumentException(NameOf(_a))
        _a = args
    End Sub
End Class"

            Dim expected = "Friend Class Program
    Private ReadOnly _a() As String

    Public Sub New(args() As String)
        If _a.Length = 0 Then Throw New ArgumentException(NameOf(_a))
        _a = args
    End Sub
End Class"
            Return AssertCodeCleanupResultAsync(expected, code)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
        Public Shared Function VisualBasicApplyMeQualification() As Task
            Dim code As String = "Public Class Program
    Private _value As String

    Public Sub Method()
        _value = ""Hello""
        Me.PrintHello()
        PrintHello()
    End Sub

    Private Sub PrintHello()
        Console.WriteLine(_value)
    End Sub
End Class
"
            Dim expected As String = "Public Class Program
    Private _value As String

    Public Sub Method()
        _value = ""Hello""
        PrintHello()
        PrintHello()
    End Sub

    Private Sub PrintHello()
        Console.WriteLine(_value)
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
        ''' <param name="systemImportsFirst">Indicates whether <c><see cref="System"/>.*</c> '<c>Imports</c>' directives should preceded others. Default is <c>true</c>.</param>
        ''' <param name="separateImportsGroups">Indicates whether '<c>Imports</c>' directives should be organized into separated groups. Default is <c>true</c>.</param>
        ''' <returns>The <see cref="Task"/> to test code cleanup.</returns>
        Private Protected Shared Async Function AssertCodeCleanupResultAsync(expected As String,
                                                                             code As String,
                                                                             Optional systemImportsFirst As Boolean = True,
                                                                             Optional separateImportsGroups As Boolean = False) As Task
            Using workspace = TestWorkspace.CreateVisualBasic(code, composition:=EditorTestCompositions.EditorFeaturesWpf)
                Dim options = CodeActionOptions.Default

                Dim solution = workspace.CurrentSolution _
                    .WithOptions(workspace.Options _
                    .WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst,
                                       LanguageNames.VisualBasic,
                                       systemImportsFirst) _
                    .WithChangedOption(GenerationOptions.SeparateImportDirectiveGroups,
                                       LanguageNames.VisualBasic,
                                       separateImportsGroups)) _
                    .WithAnalyzerReferences({
                        New AnalyzerFileReference(GetType(VisualBasicCompilerDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile),
                        New AnalyzerFileReference(GetType(MakeFieldReadonlyDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile),
                        New AnalyzerFileReference(GetType(VisualBasicPreferFrameworkTypeDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)
                                            })

                workspace.TryApplyChanges(solution)

                ' register this workspace to solution crawler so that analyzer service associate itself with given workspace
                Dim incrementalAnalyzerProvider = TryCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticAnalyzerService)(), IIncrementalAnalyzerProvider)
                incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace)

                Dim hostdoc = workspace.Documents.[Single]()
                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

                Dim codeCleanupService = document.GetLanguageService(Of ICodeCleanupService)()

                Dim enabledDiagnostics = codeCleanupService.GetAllDiagnostics()

                Dim newDoc = Await codeCleanupService.CleanupAsync(
                    document,
                    enabledDiagnostics,
                    New ProgressTracker,
                    options,
                    CancellationToken.None)

                Dim actual = Await newDoc.GetTextAsync()

                AssertEx.EqualOrDiff(expected, actual.ToString())
            End Using
        End Function

    End Class
End Namespace
