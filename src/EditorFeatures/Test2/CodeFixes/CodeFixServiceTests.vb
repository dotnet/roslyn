' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.ErrorLogger
Imports Microsoft.CodeAnalysis.Host
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeFixes.UnitTests

    Public Class CodeFixServiceTests

        Private _assemblyLoader As IAnalyzerAssemblyLoader = New InMemoryAssemblyLoader()

        Public Function CreateAnalyzerFileReference(ByVal fullPath As String) As AnalyzerFileReference
            Return New AnalyzerFileReference(fullPath, _assemblyLoader)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestProjectCodeFix() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(test)
                Dim project = workspace.CurrentSolution.Projects(0)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()
                Dim workspaceCodeFixProvider = New WorkspaceCodeFixProvider()

                Dim diagnosticService = New TestDiagnosticAnalyzerService(LanguageNames.CSharp, workspaceDiagnosticAnalyzer)
                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim logger = SpecializedCollections.SingletonEnumerable(New Lazy(Of IErrorLoggerService)(Function() workspace.Services.GetService(Of IErrorLoggerService)))
                Dim codefixService = New CodeFixService(
                                        diagnosticService,
                                        logger,
                                        {New Lazy(Of CodeFixProvider, Mef.CodeChangeProviderMetadata)(
                                            Function() workspaceCodeFixProvider,
                                            New Mef.CodeChangeProviderMetadata(New Dictionary(Of String, Object)() From {{"Name", "C#"}, {"Languages", {LanguageNames.CSharp}}}))},
                                        SpecializedCollections.EmptyEnumerable(Of Lazy(Of ISuppressionFixProvider, Mef.CodeChangeProviderMetadata)))

                ' Verify available diagnostics
                Dim document = project.Documents.Single()
                Dim diagnostics = Await diagnosticService.GetDiagnosticsForSpanAsync(document,
                    (Await document.GetSyntaxRootAsync()).FullSpan)

                Assert.Equal(1, diagnostics.Count())

                ' Verify available codefix with a global fixer
                Dim fixes = Await codefixService.GetFixesAsync(document,
                    (Await document.GetSyntaxRootAsync()).FullSpan,
                    includeSuppressionFixes:=True, cancellationToken:=CancellationToken.None)
                Assert.Equal(0, fixes.Count())

                ' Verify available codefix with a global fixer + a project fixer
                ' We will use this assembly as a project fixer provider.
                Dim _assembly = Assembly.GetExecutingAssembly()
                Dim projectAnalyzerReference = CreateAnalyzerFileReference(_assembly.Location)

                Dim projectAnalyzerReferences = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences)
                document = project.Documents.Single()
                fixes = Await codefixService.GetFixesAsync(document,
                                                     (Await document.GetSyntaxRootAsync()).FullSpan,
                                                     includeSuppressionFixes:=True, cancellationToken:=CancellationToken.None)
                Assert.Equal(1, fixes.Count())

                ' Remove a project analyzer
                project = project.RemoveAnalyzerReference(projectAnalyzerReference)
                document = project.Documents.Single()
                fixes = Await codefixService.GetFixesAsync(document,
                                                     (Await document.GetSyntaxRootAsync()).FullSpan,
                                                     includeSuppressionFixes:=True, cancellationToken:=CancellationToken.None)
                Assert.Equal(0, fixes.Count())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestDifferentLanguageProjectCodeFix() As Task
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Test.vb">
                                        Class Foo
                                        End Class
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(test)
                Dim project = workspace.CurrentSolution.Projects(0)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()
                Dim workspaceCodeFixProvider = New WorkspaceCodeFixProvider()

                Dim diagnosticService = New TestDiagnosticAnalyzerService(LanguageNames.VisualBasic, workspaceDiagnosticAnalyzer)
                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim logger = SpecializedCollections.SingletonEnumerable(New Lazy(Of IErrorLoggerService)(Function() workspace.Services.GetService(Of IErrorLoggerService)))
                Dim codefixService = New CodeFixService(
                                        diagnosticService,
                                        logger,
                                        {New Lazy(Of CodeFixProvider, Mef.CodeChangeProviderMetadata)(
                                            Function() workspaceCodeFixProvider,
                                            New Mef.CodeChangeProviderMetadata(New Dictionary(Of String, Object)() From {{"Name", "C#"}, {"Languages", {LanguageNames.CSharp}}}))},
                                        SpecializedCollections.EmptyEnumerable(Of Lazy(Of ISuppressionFixProvider, Mef.CodeChangeProviderMetadata)))

                ' Verify available diagnostics
                Dim document = project.Documents.Single()
                Dim diagnostics = Await diagnosticService.GetDiagnosticsForSpanAsync(document,
                    (Await document.GetSyntaxRootAsync()).FullSpan)

                Assert.Equal(1, diagnostics.Count())

                ' Verify no codefix with a global fixer
                Dim fixes = Await codefixService.GetFixesAsync(document,
                                                         (Await document.GetSyntaxRootAsync()).FullSpan,
                                                         includeSuppressionFixes:=True, cancellationToken:=CancellationToken.None)
                Assert.Equal(0, fixes.Count())

                ' Verify no codefix with a global fixer + a project fixer
                ' We will use this assembly as a project fixer provider.
                Dim _assembly = Assembly.GetExecutingAssembly()
                Dim projectAnalyzerReference = CreateAnalyzerFileReference(_assembly.Location)

                Dim projectAnalyzerReferences = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences)
                document = project.Documents.Single()
                fixes = Await codefixService.GetFixesAsync(document,
                                                     (Await document.GetSyntaxRootAsync()).FullSpan,
                                                     includeSuppressionFixes:=True, cancellationToken:=CancellationToken.None)
                Assert.Equal(0, fixes.Count())
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
    End Class
End Namespace
