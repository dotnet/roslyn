' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.AddFileBanner
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeCleanup)>
    Partial Public Class CodeCleanUpTests
        ' Format Document tests are handled by Format Document Test

        ' TESTS NEEDED but not found in C#
        'Apply object preference initialization preferences
        'Apply file header preferences

        <Fact>
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

        Private Const _code As String = "
Class C
    Public Sub M1(x As Integer)
        Select Case x
            Case 1, 2
                Exit Select
            Case = 10
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case = 1000
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 10 To 500
                Exit Select
            Case = 1000
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 1, 980 To 985
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case x
            Case 1 to 3, 980 To 985
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case > 100000
                Exit Select
        End Select

        Select Case x
            Case Else
                Exit Select
        End Select

        Select Case x
        End Select

        Select Case x
            Case 1
                Exit Select
            Case
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case =
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case 2 to
                Exit Select
        End Select
    End Sub
End Class
"

        Private Const _expected As String = "
Class C
    Public Sub M1(x As Integer)
        Select Case x
            Case 1, 2
                Exit Select
            Case = 10
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case = 1000
                Exit Select
        End Select

        Select Case x
            Case 10 To 500
                Exit Select
            Case = 1000
                Exit Select
        End Select

        Select Case x
            Case 1, 980 To 985
                Exit Select
        End Select

        Select Case x
            Case 1 to 3, 980 To 985
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case > 100000
                Exit Select
        End Select

        Select Case x
        End Select

        Select Case x
        End Select

        Select Case x
            Case 1
                Exit Select
            Case
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case =
                Exit Select
        End Select

        Select Case x
            Case 1
                Exit Select
            Case 2 to
                Exit Select
        End Select
    End Sub
End Class
"

        <Fact>
        Public Shared Async Function RunThirdPartyFixer() As Task
            Await TestThirdPartyCodeFixer(Of TestThirdPartyCodeFixWithFixAll, CaseTestAnalyzer)(_expected, _code)
        End Function

        <Theory>
        <InlineData(DiagnosticSeverity.Warning)>
        <InlineData(DiagnosticSeverity.Error)>
        Public Shared Async Function RunThirdPartyFixerWithSeverityOfWarningOrHigher(severity As DiagnosticSeverity) As Task
            Await TestThirdPartyCodeFixer(Of TestThirdPartyCodeFixWithFixAll, CaseTestAnalyzer)(_expected, _code, severity)
        End Function

        <Theory>
        <InlineData(DiagnosticSeverity.Hidden)>
        <InlineData(DiagnosticSeverity.Info)>
        Public Shared Async Function DoNotRunThirdPartyFixerWithSeverityLessThanWarning(severity As DiagnosticSeverity) As Task
            Await TestThirdPartyCodeFixer(Of TestThirdPartyCodeFixWithFixAll, CaseTestAnalyzer)(_code, _code, severity)
        End Function

        <Fact>
        Public Shared Async Function DoNotRunThirdPartyFixerIfItDoesNotSupportDocumentScope() As Task
            Await TestThirdPartyCodeFixer(Of TestThirdPartyCodeFixDoesNotSupportDocumentScope, CaseTestAnalyzer)(_code, _code)
        End Function

        <Fact>
        Public Shared Async Function DoNotApplyFixerIfChangesAreMadeOutsideDocument() As Task
            Await TestThirdPartyCodeFixer(Of TestThirdPartyCodeFixModifiesSolution, CaseTestAnalyzer)(_code, _code)
        End Function

        <Fact>
        Public Shared Async Function DoNotRunThirdPartyFixerWithNoFixAll() As Task
            Await TestThirdPartyCodeFixer(Of TestThirdPartyCodeFixWithOutFixAll, CaseTestAnalyzer)(_code, _code)
        End Function

        Private Shared Async Function TestThirdPartyCodeFixer(Of TCodefix As {CodeFixProvider, New}, TAnalyzer As {DiagnosticAnalyzer, New})(expected As String, code As String, Optional severity As DiagnosticSeverity = DiagnosticSeverity.Warning) As Task
            Using workspace = TestWorkspace.CreateVisualBasic(code, composition:=EditorTestCompositions.EditorFeaturesWpf.AddParts(GetType(TCodefix)))
                Dim options = CodeActionOptions.DefaultProvider
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim map = New Dictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)) From
                    {
                        {LanguageNames.VisualBasic, ImmutableArray.Create(Of DiagnosticAnalyzer)(New TAnalyzer())}
                    }
                Dim analyzer As DiagnosticAnalyzer = New TAnalyzer()
                Dim diagnosticIds = analyzer.SupportedDiagnostics.SelectAsArray(Function(d) d.Id)

                Dim editorconfigText = "is_global = true"
                For Each diagnosticId In diagnosticIds
                    editorconfigText += $"{Environment.NewLine}dotnet_diagnostic.{diagnosticId}.severity = {severity.ToEditorConfigString()}"
                Next

                project = project.AddAnalyzerReference(New TestAnalyzerReferenceByLanguage(map))
                project = project.Solution.WithProjectFilePath(project.Id, $"z:\\{project.FilePath}").GetProject(project.Id)
                project = project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(editorconfigText), filePath:="z:\\.editorconfig").Project
                workspace.TryApplyChanges(project.Solution)

                Dim hostdoc = workspace.Documents.[Single]()
                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

                Dim codeCleanupService = document.GetLanguageService(Of ICodeCleanupService)()

                Dim enabledDiagnostics = codeCleanupService.GetAllDiagnostics()

                Dim newDoc = Await codeCleanupService.CleanupAsync(
                    document,
                    enabledDiagnostics,
                    CodeAnalysisProgress.None,
                    options,
                    CancellationToken.None)

                Dim actual = Await newDoc.GetTextAsync()

                AssertEx.EqualOrDiff(expected, actual.ToString())
            End Using
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

                workspace.SetAnalyzerFallbackOptions(New OptionsCollection(LanguageNames.VisualBasic) From {
                    {GenerationOptions.SeparateImportDirectiveGroups, separateImportsGroups},
                    {GenerationOptions.PlaceSystemNamespaceFirst, systemImportsFirst}
                })

                Dim solution = workspace.CurrentSolution.WithAnalyzerReferences(
                {
                    New AnalyzerFileReference(GetType(VisualBasicCompilerDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile),
                    New AnalyzerFileReference(GetType(AbstractAddFileBannerCodeRefactoringProvider).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile),
                    New AnalyzerFileReference(GetType(VisualBasicPreferFrameworkTypeDiagnosticAnalyzer).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile)
                })

                workspace.TryApplyChanges(solution)

                Dim hostdoc = workspace.Documents.[Single]()
                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)

                Dim codeCleanupService = document.GetLanguageService(Of ICodeCleanupService)()

                Dim enabledDiagnostics = codeCleanupService.GetAllDiagnostics()

                Dim newDoc = Await codeCleanupService.CleanupAsync(
                    document,
                    enabledDiagnostics,
                    CodeAnalysisProgress.None,
                    workspace.GlobalOptions.CreateProvider(),
                    CancellationToken.None)

                Dim actual = Await newDoc.GetTextAsync()

                AssertEx.EqualOrDiff(expected, actual.ToString())
            End Using
        End Function

        <PartNotDiscoverable, [Shared], ExportCodeFixProvider(LanguageNames.VisualBasic)>
        Private Class TestThirdPartyCodeFixWithFixAll : Inherits TestThirdPartyCodeFix

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function GetFixAllProvider() As FixAllProvider
                Return BatchFixAllProvider.Instance
            End Function
        End Class

        <PartNotDiscoverable, [Shared], ExportCodeFixProvider(LanguageNames.VisualBasic)>
        Private Class TestThirdPartyCodeFixWithOutFixAll : Inherits TestThirdPartyCodeFix

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub
        End Class

        <PartNotDiscoverable, [Shared], ExportCodeFixProvider(LanguageNames.VisualBasic)>
        Private Class TestThirdPartyCodeFixModifiesSolution : Inherits TestThirdPartyCodeFix

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function GetFixAllProvider() As FixAllProvider
                Return New ModifySolutionFixAll
            End Function

            Private Class ModifySolutionFixAll : Inherits FixAllProvider

                Public Overrides Function GetFixAsync(fixAllContext As FixAllContext) As Task(Of CodeAction)
                    Dim solution = fixAllContext.Solution
                    Return Task.FromResult(CodeAction.Create(
                                           "Remove default case",
                                           Async Function(cancellationToken)
                                               Dim toFix = Await fixAllContext.GetDocumentDiagnosticsToFixAsync()
                                               Dim project As Project = Nothing
                                               For Each kvp In toFix
                                                   Dim document = kvp.Key
                                                   project = document.Project
                                                   Dim diagnostics = kvp.Value
                                                   Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
                                                   For Each diagnostic In diagnostics
                                                       Dim node = (Await diagnostic.Location.SourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan)
                                                       document = document.WithSyntaxRoot(root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia))
                                                   Next

                                                   solution = solution.WithDocumentText(document.Id, Await document.GetTextAsync())
                                               Next

                                               Return solution.AddDocument(DocumentId.CreateNewId(project.Id), "new.vb", SourceText.From(""))
                                           End Function,
                                           NameOf(TestThirdPartyCodeFix)))
                End Function
            End Class
        End Class

        <PartNotDiscoverable, [Shared], ExportCodeFixProvider(LanguageNames.VisualBasic)>
        Private Class TestThirdPartyCodeFixDoesNotSupportDocumentScope : Inherits TestThirdPartyCodeFix

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function GetFixAllProvider() As FixAllProvider
                Return New ModifySolutionFixAll
            End Function

            Private Class ModifySolutionFixAll : Inherits FixAllProvider

                Public Overrides Function GetSupportedFixAllScopes() As IEnumerable(Of FixAllScope)
                    Return {FixAllScope.Project, FixAllScope.Solution, FixAllScope.Custom}
                End Function

                Public Overrides Function GetFixAsync(fixAllContext As FixAllContext) As Task(Of CodeAction)
                    Dim solution = fixAllContext.Solution
                    Return Task.FromResult(CodeAction.Create(
                                           "Remove default case",
                                           Async Function(cancellationToken)
                                               Dim toFix = Await fixAllContext.GetDocumentDiagnosticsToFixAsync()
                                               Dim project As Project = Nothing
                                               For Each kvp In toFix
                                                   Dim document = kvp.Key
                                                   project = document.Project
                                                   Dim diagnostics = kvp.Value
                                                   Dim root = Await document.GetSyntaxRootAsync(cancellationToken)
                                                   For Each diagnostic In diagnostics
                                                       Dim node = (Await diagnostic.Location.SourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan)
                                                       document = document.WithSyntaxRoot(root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia))
                                                   Next

                                                   solution = solution.WithDocumentText(document.Id, Await document.GetTextAsync())
                                               Next

                                               Return solution.AddDocument(DocumentId.CreateNewId(project.Id), "new.vb", SourceText.From(""))
                                           End Function,
                                           NameOf(TestThirdPartyCodeFix)))
                End Function
            End Class
        End Class

        Private Class TestThirdPartyCodeFix : Inherits CodeFixProvider

            Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
                Get
                    Return ImmutableArray.Create("HasDefaultCase")
                End Get
            End Property

            Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                For Each diagnostic In context.Diagnostics
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Remove default case",
                            Async Function(cancellationToken)
                                Dim root = Await context.Document.GetSyntaxRootAsync(cancellationToken)
                                Dim node = (Await diagnostic.Location.SourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan)
                                Return context.Document.WithSyntaxRoot(root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia))
                            End Function,
                            NameOf(TestThirdPartyCodeFix)),
                    diagnostic)
                Next

                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace
