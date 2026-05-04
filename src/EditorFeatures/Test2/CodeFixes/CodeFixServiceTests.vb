' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Copilot
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.ErrorLogger
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeFixes.UnitTests
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class CodeFixServiceTests

        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures

        Private ReadOnly _assemblyLoader As IAnalyzerAssemblyLoader = New InMemoryAssemblyLoader()

        Public Function CreateAnalyzerFileReference(ByVal fullPath As String) As AnalyzerFileReference
            Return New AnalyzerFileReference(fullPath, _assemblyLoader)
        End Function

        <Fact>
        Public Async Function TestProjectCodeFix() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Goo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = EditorTestWorkspace.Create(test, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()
                Dim workspaceCodeFixProvider = New WorkspaceCodeFixProvider()

                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(workspaceDiagnosticAnalyzer))
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim project = workspace.CurrentSolution.Projects(0)

                Dim diagnosticService = workspace.Services.GetRequiredService(Of IDiagnosticAnalyzerService)()
                Dim logger = SpecializedCollections.SingletonEnumerable(New Lazy(Of IErrorLoggerService)(Function() workspace.Services.GetService(Of IErrorLoggerService)))
                Dim codefixService = New CodeFixService(
                    logger,
                    {New Lazy(Of CodeFixProvider, Mef.CodeChangeProviderMetadata)(
                        Function() workspaceCodeFixProvider,
                        New Mef.CodeChangeProviderMetadata(New Dictionary(Of String, Object)() From {{"Name", "C#"}, {"Languages", {LanguageNames.CSharp}}}))},
                    SpecializedCollections.EmptyEnumerable(Of Lazy(Of IConfigurationFixProvider, Mef.CodeChangeProviderMetadata)))

                ' Verify available diagnostics
                Dim document = project.Documents.Single()
                Dim diagnostics = Await diagnosticService.GetDiagnosticsForSpanAsync(document,
                    range:=(Await document.GetSyntaxRootAsync()).FullSpan, DiagnosticKind.All, CancellationToken.None)

                Assert.Equal(1, diagnostics.Length)

                ' Verify available codefix with a global fixer
                Dim fixes = Await codefixService.GetFixesAsync(
                    document,
                    (Await document.GetSyntaxRootAsync()).FullSpan,
                    CancellationToken.None)

                Assert.Empty(fixes)

                ' Verify available codefix with a global fixer + a project fixer
                ' We will use this assembly as a project fixer provider.
                Dim _assembly = Assembly.GetExecutingAssembly()
                Dim projectAnalyzerReference = CreateAnalyzerFileReference(_assembly.Location)

                Dim projectAnalyzerReferences = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences)
                document = project.Documents.Single()
                fixes = Await codefixService.GetFixesAsync(
                    document,
                    (Await document.GetSyntaxRootAsync()).FullSpan,
                    CancellationToken.None)
                Assert.Equal(1, fixes.Length)

                ' Remove a project analyzer
                project = project.RemoveAnalyzerReference(projectAnalyzerReference)
                document = project.Documents.Single()
                fixes = Await codefixService.GetFixesAsync(
                    document,
                    (Await document.GetSyntaxRootAsync()).FullSpan,
                    CancellationToken.None)

                Assert.Empty(fixes)
            End Using
        End Function

        <Fact>
        Public Async Function TestDifferentLanguageProjectCodeFix() As Task
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Test.vb">
                                        Class Goo
                                        End Class
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = EditorTestWorkspace.Create(test, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()
                Dim workspaceCodeFixProvider = New WorkspaceCodeFixProvider()

                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(workspaceDiagnosticAnalyzer))
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim project = workspace.CurrentSolution.Projects(0)

                Dim diagnosticService = workspace.Services.GetRequiredService(Of IDiagnosticAnalyzerService)()
                Dim logger = SpecializedCollections.SingletonEnumerable(New Lazy(Of IErrorLoggerService)(Function() workspace.Services.GetService(Of IErrorLoggerService)))
                Dim codefixService = New CodeFixService(
                    logger,
                    {New Lazy(Of CodeFixProvider, Mef.CodeChangeProviderMetadata)(
                        Function() workspaceCodeFixProvider,
                        New Mef.CodeChangeProviderMetadata(New Dictionary(Of String, Object)() From {{"Name", "C#"}, {"Languages", {LanguageNames.CSharp}}}))},
                    SpecializedCollections.EmptyEnumerable(Of Lazy(Of IConfigurationFixProvider, Mef.CodeChangeProviderMetadata)))

                ' Verify available diagnostics
                Dim document = project.Documents.Single()
                Dim diagnostics = Await diagnosticService.GetDiagnosticsForSpanAsync(document,
                    range:=(Await document.GetSyntaxRootAsync()).FullSpan, DiagnosticKind.All, CancellationToken.None)

                Assert.Equal(1, diagnostics.Length)

                ' Verify no codefix with a global fixer
                Dim fixes = Await codefixService.GetFixesAsync(
                    document,
                    (Await document.GetSyntaxRootAsync()).FullSpan,
                    CancellationToken.None)

                Assert.Empty(fixes)

                ' Verify no codefix with a global fixer + a project fixer
                ' We will use this assembly as a project fixer provider.
                Dim _assembly = Assembly.GetExecutingAssembly()
                Dim projectAnalyzerReference = CreateAnalyzerFileReference(_assembly.Location)

                Dim projectAnalyzerReferences = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences)
                document = project.Documents.Single()
                fixes = Await codefixService.GetFixesAsync(
                    document,
                    (Await document.GetSyntaxRootAsync()).FullSpan,
                    CancellationToken.None)

                Assert.Empty(fixes)
            End Using
        End Function

        Private Class WorkspaceDiagnosticAnalyzer
            Inherits AbstractDiagnosticAnalyzer

            Public ReadOnly Descriptor As DiagnosticDescriptor = New DiagnosticDescriptor("TEST1111",
                                                                                          "WorkspaceDiagnosticDescription",
                                                                                          "WorkspaceDiagnosticMessage",
                                                                                          "WorkspaceDiagnosticCategory",
                                                                                          DiagnosticSeverity.Warning,
                                                                                          isEnabledByDefault:=True)

            Public Overrides ReadOnly Property DiagDescriptor As DiagnosticDescriptor
                Get
                    Return Descriptor
                End Get
            End Property
        End Class

        Private MustInherit Class AbstractDiagnosticAnalyzer
            Inherits DiagnosticAnalyzer

            Public MustOverride ReadOnly Property DiagDescriptor As DiagnosticDescriptor

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(DiagDescriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
            End Sub

            Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                context.ReportDiagnostic(Diagnostic.Create(DiagDescriptor, context.Symbol.Locations.First(), context.Symbol.Locations.Skip(1)))
            End Sub

        End Class

        <ExportCodeFixProvider(LanguageNames.CSharp, Name:="WorkspaceCodeFixProvider"), [Shared]>
        Private Class WorkspaceCodeFixProvider
            Inherits CodeFixProvider

            <ImportingConstructor>
            <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
            Public Sub New()
            End Sub

            Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
                Get
                    Return ImmutableArray.Create("TEST0000")
                End Get
            End Property

            Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                Contract.ThrowIfFalse(context.Document.Project.Language = LanguageNames.CSharp)
                Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

#Disable Warning RS0005
                context.RegisterCodeFix(CodeAction.Create("FIX_TEST0000", Function(ct) Task.FromResult(context.Document.WithSyntaxRoot(root))), context.Diagnostics)
#Enable Warning RS0005
            End Function
        End Class

        <ExportCodeFixProvider(LanguageNames.CSharp, Name:="ProjectCodeFixProvider"), [Shared]>
        Public Class ProjectCodeFixProvider
            Inherits CodeFixProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
                Get
                    Return ImmutableArray.Create("TEST1111")
                End Get
            End Property

            Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
                Contract.ThrowIfFalse(context.Document.Project.Language = LanguageNames.CSharp)
                Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

#Disable Warning RS0005
                context.RegisterCodeFix(CodeAction.Create("FIX_TEST1111", Function(ct) Task.FromResult(context.Document.WithSyntaxRoot(root))), context.Diagnostics)
#Enable Warning RS0005
            End Function
        End Class

        <Fact>
        Public Async Function TestCopilotCodeAnalysisServiceWithoutSyntaxTree() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
                    <Document>
                        var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
                    </Document>
                </Project>
            </Workspace>

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeDefinitions),
                GetType(NoCompilationContentTypeLanguageService),
                GetType(NoCompilationCopilotCodeAnalysisService))

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=composition)

                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim diagnosticsXml =
                    <Diagnostics>
                        <Error Id=<%= "TestId" %>
                            MappedFile=<%= document.Name %> MappedLine="0" MappedColumn="0"
                            OriginalFile=<%= document.Name %> OriginalLine="0" OriginalColumn="0"
                            Message=<%= "Test Message" %>/>
                    </Diagnostics>
                Dim diagnostics = DiagnosticProviderTests.GetExpectedDiagnostics(workspace, diagnosticsXml)

                Dim copilotCodeAnalysisService = document.Project.Services.GetService(Of ICopilotCodeAnalysisService)()
                Dim noCompilationCopilotCodeAnalysisService = DirectCast(copilotCodeAnalysisService, NoCompilationCopilotCodeAnalysisService)

                NoCompilationCopilotCodeAnalysisService.Diagnostics = diagnostics.SelectAsArray(Of Diagnostic)(
                        Function(d) d.ToDiagnosticAsync(document.Project, CancellationToken.None).Result)
                Dim codefixService = workspace.ExportProvider.GetExportedValue(Of ICodeFixService)

                ' Make sure we don't crash
                Dim unused = Await codefixService.GetMostSevereFixAsync(
                    document, Text.TextSpan.FromBounds(0, 0), priority:=Nothing, CancellationToken.None)
            End Using
        End Function

        <ExportLanguageService(GetType(ICopilotOptionsService), NoCompilationConstants.LanguageName, ServiceLayer.Test), [Shared], PartNotDiscoverable>
        Private Class NoCompilationCopilotOptionsService
            Implements ICopilotOptionsService

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function IsRefineOptionEnabledAsync() As Task(Of Boolean) Implements ICopilotOptionsService.IsRefineOptionEnabledAsync
                Return Task.FromResult(True)
            End Function

            Public Function IsCodeAnalysisOptionEnabledAsync() As Task(Of Boolean) Implements ICopilotOptionsService.IsCodeAnalysisOptionEnabledAsync
                Return Task.FromResult(True)
            End Function

            Public Function IsOnTheFlyDocsOptionEnabledAsync() As Task(Of Boolean) Implements ICopilotOptionsService.IsOnTheFlyDocsOptionEnabledAsync
                Return Task.FromResult(True)
            End Function

            Public Function IsGenerateDocumentationCommentOptionEnabledAsync() As Task(Of Boolean) Implements ICopilotOptionsService.IsGenerateDocumentationCommentOptionEnabledAsync
                Return Task.FromResult(True)
            End Function

            Public Function IsImplementNotImplementedExceptionEnabledAsync() As Task(Of Boolean) Implements ICopilotOptionsService.IsImplementNotImplementedExceptionEnabledAsync
                Return Task.FromResult(True)
            End Function
        End Class

        <ExportLanguageService(GetType(ICopilotCodeAnalysisService), NoCompilationConstants.LanguageName, ServiceLayer.Test), [Shared], PartNotDiscoverable>
        Private Class NoCompilationCopilotCodeAnalysisService
            Implements ICopilotCodeAnalysisService

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Shared Property Diagnostics As ImmutableArray(Of Diagnostic) = ImmutableArray(Of Diagnostic).Empty

            Public Function IsAvailableAsync(cancellationToken As CancellationToken) As Task(Of Boolean) Implements ICopilotCodeAnalysisService.IsAvailableAsync
                Return Task.FromResult(True)
            End Function

            Public Function GetAvailablePromptTitlesAsync(document As Document, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of String)) Implements ICopilotCodeAnalysisService.GetAvailablePromptTitlesAsync
                Return Task.FromResult(ImmutableArray.Create("Title"))
            End Function

            Public Function AnalyzeDocumentAsync(document As Document, span As TextSpan?, promptTitle As String, cancellationToken As CancellationToken) As Task Implements ICopilotCodeAnalysisService.AnalyzeDocumentAsync
                Return Task.CompletedTask
            End Function

            Public Function GetCachedDocumentDiagnosticsAsync(document As Document, span As TextSpan?, promptTitles As ImmutableArray(Of String), cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of Diagnostic)) Implements ICopilotCodeAnalysisService.GetCachedDocumentDiagnosticsAsync
                Return Task.FromResult(Diagnostics)
            End Function

            Public Function StartRefinementSessionAsync(oldDocument As Document, newDocument As Document, primaryDiagnostic As Diagnostic, cancellationToken As CancellationToken) As Task Implements ICopilotCodeAnalysisService.StartRefinementSessionAsync
                Return Task.CompletedTask
            End Function

            Public Function IsFileExcludedAsync(filePath As String, cancellationToken As CancellationToken) As Task(Of Boolean) Implements ICopilotCodeAnalysisService.IsFileExcludedAsync
                Return Task.FromResult(False)
            End Function

            Public Function GetDocumentationCommentAsync(proposal As DocumentationCommentProposal, cancellationToken As CancellationToken) As Task(Of (responseDictionary As Dictionary(Of String, String), isQuotaExceeded As Boolean)) Implements ICopilotCodeAnalysisService.GetDocumentationCommentAsync
                Return Task.FromResult((New Dictionary(Of String, String), False))
            End Function

            Public Function GetOnTheFlyDocsPromptAsync(onTheFlyDocsInfo As OnTheFlyDocsInfo, cancellationToken As CancellationToken) As Task(Of String) Implements ICopilotCodeAnalysisService.GetOnTheFlyDocsPromptAsync
                Return Task.FromResult(String.Empty)
            End Function

            Public Function GetOnTheFlyDocsResponseAsync(prompt As String, cancellationToken As CancellationToken) As Task(Of (responseString As String, isQuotaExceeded As Boolean)) Implements ICopilotCodeAnalysisService.GetOnTheFlyDocsResponseAsync
                Return Task.FromResult((String.Empty, False))
            End Function

            Public Function IsImplementNotImplementedExceptionsAvailableAsync(cancellationToken As CancellationToken) As Task(Of Boolean) Implements ICopilotCodeAnalysisService.IsImplementNotImplementedExceptionsAvailableAsync
                Return Task.FromResult(False)
            End Function

            Public Function ImplementNotImplementedExceptionsAsync(document As Document, methodOrProperties As ImmutableDictionary(Of SyntaxNode, ImmutableArray(Of ReferencedSymbol)), cancellationToken As CancellationToken) As Task(Of ImmutableDictionary(Of SyntaxNode, ImplementationDetails)) Implements ICopilotCodeAnalysisService.ImplementNotImplementedExceptionsAsync
                Return Task.FromResult(ImmutableDictionary(Of SyntaxNode, ImplementationDetails).Empty)
            End Function
        End Class
    End Class
End Namespace
