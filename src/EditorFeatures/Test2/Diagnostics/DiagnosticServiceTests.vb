﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests

    <[UseExportProvider]>
    Public Class DiagnosticServiceTests

        Private _assemblyLoader As IAnalyzerAssemblyLoader = New InMemoryAssemblyLoader()

        Public Function CreateAnalyzerFileReference(ByVal fullPath As String) As AnalyzerFileReference
            Return New AnalyzerFileReference(fullPath, _assemblyLoader)
        End Function

        Private Class FailingTextLoader
            Inherits TextLoader

            Dim _path As String

            Friend Overrides ReadOnly Property FilePath As String
                Get
                    Return _path
                End Get
            End Property

            Sub New(path As String)
                _path = path
            End Sub

            Public Overrides Function LoadTextAndVersionAsync(workspace As Workspace, documentId As DocumentId, cancellationToken As CancellationToken) As Task(Of TextAndVersion)
                Throw New InvalidDataException("Bad data!")
            End Function
        End Class

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestProjectAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Goo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()
                Dim projectDiagnosticAnalyzer1 = New TestDiagnosticAnalyzer1(1)
                Dim projectDiagnosticAnalyzer2 = New TestDiagnosticAnalyzer2(2)

                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(workspaceDiagnosticAnalyzer))
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim solution = workspace.CurrentSolution
                Dim hostAnalyzers = solution.State.Analyzers
                Dim project = solution.Projects(0)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Verify available diagnostic descriptors/analyzers
                Dim descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Count())
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)

                Dim document = project.Documents.Single()
                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                    document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan).WaitAndGetResult(CancellationToken.None)

                Assert.Equal(1, diagnostics.Count())

                ' Add a project analyzer reference
                Dim projectAnalyzers1 = ImmutableArray.Create(Of DiagnosticAnalyzer)(projectDiagnosticAnalyzer1)
                Dim projectAnalyzerReference1 = New AnalyzerImageReference(projectAnalyzers1, display:=NameOf(projectAnalyzers1))
                Dim projectAnalyzerReferences1 = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference1)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences1)

                ' Verify available diagnostic descriptors/analyzers
                descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(2, descriptorsMap.Count)
                descriptors = descriptorsMap.Values.SelectMany(Function(d) d).OrderBy(Function(d) d.Id).ToImmutableArray()
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)
                Assert.Equal(projectDiagnosticAnalyzer1.DiagDescriptor.Id, descriptors(1).Id)

                Dim project1 = project.WithAssemblyName("Mumble")
                Assert.NotSame(project, project1)

                ' Add another project analyzer
                Dim projectAnalyzers2 = ImmutableArray.Create(Of DiagnosticAnalyzer)(projectDiagnosticAnalyzer2)
                Dim projectAnalyzerReference2 = New AnalyzerImageReference(projectAnalyzers2, display:=NameOf(projectAnalyzers2))
                project = project.AddAnalyzerReference(projectAnalyzerReference2)

                ' Verify available diagnostic descriptors/analyzers
                descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(3, descriptorsMap.Count)
                descriptors = descriptorsMap.Values.SelectMany(Function(d) d).OrderBy(Function(d) d.Id).ToImmutableArray()
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)
                Assert.Equal(projectDiagnosticAnalyzer1.DiagDescriptor.Id, descriptors(1).Id)
                Assert.Equal(projectDiagnosticAnalyzer2.DiagDescriptor.Id, descriptors(2).Id)

                document = project.Documents.Single()
                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                    document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                    ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(3, diagnostics.Count())

                ' Remove a project analyzer
                project = project.RemoveAnalyzerReference(projectAnalyzerReference1)

                ' Verify available diagnostic descriptors/analyzers
                descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(2, descriptorsMap.Count)
                descriptors = descriptorsMap.Values.SelectMany(Function(d) d).OrderBy(Function(d) d.Id).ToImmutableArray()
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)
                Assert.Equal(projectDiagnosticAnalyzer2.DiagDescriptor.Id, descriptors(1).Id)

                document = project.Documents.Single()
                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                    document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                    ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(2, diagnostics.Count())

                ' Verify available diagnostic descriptors/analyzers if not project specific
                descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache)
                Assert.Equal(1, descriptorsMap.Count)
                descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Count)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)

                ' Add an existing workspace analyzer to the project, ensure no duplicate diagnostics.
                Dim duplicateProjectAnalyzersReference = hostAnalyzers.GetHostAnalyzerReferencesMap().First().Value
                project = project.WithAnalyzerReferences({duplicateProjectAnalyzersReference})

                ' Verify duplicate descriptors or diagnostics.
                descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)
                descriptors = descriptorsMap.Values.SelectMany(Function(d) d).OrderBy(Function(d) d.Id).ToImmutableArray()
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)

                document = project.Documents.Single()
                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                    document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                    ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
            End Using
        End Sub

        <Fact>
        Public Sub TestEmptyProjectAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Goo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()

                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(workspaceDiagnosticAnalyzer))
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects(0)
                Dim hostAnalyzers = solution.State.Analyzers

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Add project analyzer reference with no analyzers.
                Dim projectAnalyzersEmpty = ImmutableArray(Of DiagnosticAnalyzer).Empty
                Dim projectAnalyzerReference1 = New AnalyzerImageReference(projectAnalyzersEmpty)
                Dim projectAnalyzerReferences1 = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference1)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences1)

                ' Query descriptors twice: second query was hitting an assert in DiagnosticAnalyzersAndStates.
                Dim descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)

                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Count())
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)
            End Using
        End Sub

        <Fact>
        Public Sub TestNameCollisionOnDisplayNames()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Goo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim referenceName = "Test"

                Dim hostAnalyzerReference = New AnalyzerImageReference(
                    ImmutableArray.Create(Of DiagnosticAnalyzer)(New TestDiagnosticAnalyzer1(0)), display:=referenceName)

                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({hostAnalyzerReference}))

                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects(0)
                Dim hostAnalyzer = solution.State.Analyzers

                Dim projectAnalyzerReference = New AnalyzerImageReference(
                    ImmutableArray.Create(Of DiagnosticAnalyzer)(New TestDiagnosticAnalyzer1(1)), display:=referenceName)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                project = project.WithAnalyzerReferences(ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference))

                Dim descriptorsMap = hostAnalyzer.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)

                ' two references in the map
                Assert.Equal(1, descriptorsMap.Count)

                Dim names = New HashSet(Of String)
                names.UnionWith(descriptorsMap.Keys)

                Assert.Equal(1, names.Where(Function(n) n = referenceName).Count())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestRulesetBasedDiagnosticFiltering()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Goo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()

                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(workspaceDiagnosticAnalyzer))
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects(0)
                Dim hostAnalyzers = solution.State.Analyzers

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Verify available diagnostic descriptors/analyzers
                Dim descriptorsMap = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Count())
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)

                Dim document = project.Documents.Single()
                Dim span = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, span).WaitAndGetResult(CancellationToken.None).ToImmutableArray()
                Assert.Equal(1, diagnostics.Length)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, diagnostics(0).Id)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.DefaultSeverity, diagnostics(0).Severity)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.DefaultSeverity, diagnostics(0).DefaultSeverity)

                Dim suppressDiagOptions = New Dictionary(Of String, ReportDiagnostic)
                suppressDiagOptions.Add(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, ReportDiagnostic.Suppress)
                Dim newCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(suppressDiagOptions)
                project = project.WithCompilationOptions(newCompilationOptions)
                document = project.Documents.Single()
                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, span).WaitAndGetResult(CancellationToken.None).ToImmutableArray()
                Assert.Equal(0, diagnostics.Length)

                Dim changeSeverityDiagOptions = New Dictionary(Of String, ReportDiagnostic)
                changeSeverityDiagOptions.Add(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, ReportDiagnostic.Error)
                newCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(changeSeverityDiagOptions)
                project = project.WithCompilationOptions(newCompilationOptions)
                document = project.Documents.Single()
                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, span).WaitAndGetResult(CancellationToken.None).ToImmutableArray()
                Assert.Equal(1, diagnostics.Length)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, diagnostics(0).Id)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.DefaultSeverity, diagnostics(0).DefaultSeverity)
                Assert.Equal(DiagnosticSeverity.Error, diagnostics(0).Severity)
            End Using
        End Sub

        <Fact>
        Public Sub TestProjectAnalyzerMessages()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Alpha.cs">
                                   class Alpha { }
                               </Document>
                           </Project>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Bravo.vb">
                                   Class Bravo : End Class
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim analyzer1 As DiagnosticAnalyzer = New TestDiagnosticAnalyzer1(1)
                Dim analyzer2 As DiagnosticAnalyzer = New TestDiagnosticAnalyzer2(2)

                Dim solution = workspace.CurrentSolution

                Dim p1 = solution.Projects.Single(Function(p) p.Language = LanguageNames.CSharp)
                p1 = p1.WithAnalyzerReferences(SpecializedCollections.SingletonCollection(New AnalyzerImageReference(ImmutableArray.Create(analyzer1))))
                solution = p1.Solution

                Dim p2 = solution.Projects.Single(Function(p) p.Language = LanguageNames.VisualBasic)
                p2 = p2.WithAnalyzerReferences(SpecializedCollections.SingletonCollection(New AnalyzerImageReference(ImmutableArray.Create(analyzer2))))
                solution = p2.Solution

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                Dim hostAnalyzers = solution.State.Analyzers
                Dim workspaceDescriptors = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache)
                Assert.Equal(0, workspaceDescriptors.Count)

                Dim descriptors1 = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, p1)
                Assert.Equal("XX0001", descriptors1.Single().Value.Single().Id)
                Dim diagnostics1 = diagnosticService.GetDiagnosticsForSpanAsync(p1.Documents.Single(), New TextSpan(0, p1.Documents.Single().GetTextAsync().Result.Length)).Result
                Assert.Equal("XX0001", diagnostics1.Single().Id)

                Dim descriptors2 = hostAnalyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, p2)
                Assert.Equal("XX0002", descriptors2.Single().Value.Single().Id)
                Dim diagnostics2 = diagnosticService.GetDiagnosticsForSpanAsync(p2.Documents.Single(), New TextSpan(0, p2.Documents.Single().GetTextAsync().Result.Length)).Result
                Assert.Equal("XX0002", diagnostics2.Single().Id)
            End Using
        End Sub

        <Fact>
        Public Sub TestGlobalAnalyzerGroup()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Bravo.vb">
                                   Class Bravo : End Class
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim analyzer1 = New TestDiagnosticAnalyzer1(1)
                Dim analyzer2 = New TestDiagnosticAnalyzer2(2)

                Dim analyzersMap = New Dictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)) From
                {
                    {LanguageNames.CSharp, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer1)},
                    {LanguageNames.VisualBasic, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer2)}
                }

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(analyzersMap)
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim diagnosticService2 = New TestDiagnosticAnalyzerService()

                Dim descriptors = workspace.CurrentSolution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService2.AnalyzerInfoCache)
                Assert.Equal(1, descriptors.Count)
                Assert.Equal(2, descriptors.Single().Value.Count)
            End Using
        End Sub

        <Fact, WorkItem(923324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923324"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDuplicateFileAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzerReference1 = CreateAnalyzerFileReference("x:\temp.dll")
                Dim analyzerReference2 = CreateAnalyzerFileReference("x:\temp.dll")
                project = project.AddAnalyzerReference(analyzerReference1)
#If DEBUG Then
                Debug.Assert(project.AnalyzerReferences.Contains(analyzerReference2))
#End If
            End Using
        End Sub

        <WpfFact, WorkItem(1091877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091877"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDuplicateFileAnalyzers2()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                ' Add duplicate analyzer references: one as VSIX analyzer reference and other one as project analyzer reference.
                Dim analyzerReference1 = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location)
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference1}))

                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzerReference2 = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location)
                project = project.AddAnalyzerReference(analyzerReference2)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim descriptorsMap = workspace.CurrentSolution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)

                ' Verify no duplicate diagnostics.
                Dim document = project.Documents.Single()
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(
                        document,
                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan) _
                    .WaitAndGetResult(CancellationToken.None) _
                    .Select(Function(d) d.Id = WorkspaceDiagnosticAnalyzer.Descriptor.Id)

                Assert.Equal(1, diagnostics.Count)
            End Using
        End Sub

        <Fact, WorkItem(923324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923324"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDuplicateImageAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New TestDiagnosticAnalyzer1(0)
                Dim analyzerReference1 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                Dim analyzerReference2 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference1)
                project = project.AddAnalyzerReference(analyzerReference2)
#If DEBUG Then
                Debug.Assert(project.AnalyzerReferences.Contains(analyzerReference1))
#End If
            End Using
        End Sub

        <WpfFact, WorkItem(937956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937956"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticAnalyzerExceptionHandledGracefully()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true" Features="IOperation">
                               <Document FilePath="Test.cs">
                                   class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New ThrowsExceptionAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(0, descriptors.Count())

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(0, diagnostics.Count())
            End Using
        End Sub

        <WpfFact, WorkItem(937915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937915"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        <WorkItem(759, "https://github.com/dotnet/roslyn/issues/759")>
        Public Async Sub TestDiagnosticAnalyzerExceptionHandledGracefully2()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim root = Await document.GetSyntaxRootAsync().ConfigureAwait(False)
                Dim diagnostics = Await diagnosticService.GetDiagnosticsForSpanAsync(document, root.FullSpan)
                Assert.Equal(0, diagnostics.Count())

                diagnostics = Await diagnosticService.GetDiagnosticsAsync(project.Solution).ConfigureAwait(False)
                Dim diagnostic = diagnostics.First()
                Assert.True(diagnostic.Id = "AD0001")
                Assert.Contains("CodeBlockStartedAnalyzer", diagnostic.Message, StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, WorkItem(1167439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1167439"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticAnalyzerExceptionHandledNoCrash()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs"/>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)

                Dim expected = Diagnostic.Create("test", "test", "test", DiagnosticSeverity.Error, DiagnosticSeverity.Error, True, 0)
                Dim exceptionDiagnosticsSource = New TestHostDiagnosticUpdateSource(workspace)

                ' check reporting diagnostic to a project that doesn't exist
                exceptionDiagnosticsSource.ReportAnalyzerDiagnostic(analyzer, expected, ProjectId.CreateFromSerialized(Guid.NewGuid(), "dummy"))
                Dim diagnostics = exceptionDiagnosticsSource.GetTestAccessor().GetReportedDiagnostics(analyzer)
                Assert.Equal(0, diagnostics.Count())

                ' check workspace diagnostic reporting
                exceptionDiagnosticsSource.ReportAnalyzerDiagnostic(analyzer, expected, projectId:=Nothing)
                diagnostics = exceptionDiagnosticsSource.GetTestAccessor().GetReportedDiagnostics(analyzer)

                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(expected.Id, diagnostics.First().Id)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestDiagnosticAnalyzer_FileLoadFailure() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim documentId = solution.Projects.Single().DocumentIds.Single()
                solution = solution.WithDocumentTextLoader(documentId, New FailingTextLoader("Test.cs"), PreservationMode.PreserveIdentity)
                workspace.ChangeSolution(solution)

                Dim project = solution.Projects.Single()
                Dim document = project.Documents.Single()

                ' analyzer throws an exception
                Dim analyzer = New CodeBlockStartedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim span = (Await document.GetSyntaxRootAsync().ConfigureAwait(False)).FullSpan
                Dim diagnostics = Await diagnosticService.GetDiagnosticsForSpanAsync(document, span).ConfigureAwait(False)
                Assert.Equal(1, diagnostics.Count())
                Assert.True(diagnostics(0).Id = "IDE1100")
                Assert.Equal(String.Format(WorkspacesResources.Error_reading_content_of_source_file_0_1, "Test.cs", "Bad data!"), diagnostics(0).Message)
            End Using
        End Function

        <WpfFact, WorkItem(937939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937939"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestOperationAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true" Features="IOperation">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() { int x = 0; } }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                For Each actionKind As OperationAnalyzer.ActionKind In [Enum].GetValues(GetType(OperationAnalyzer.ActionKind))
                    Dim solution = workspace.CurrentSolution
                    Dim project = solution.Projects.Single
                    Dim analyzer = New OperationAnalyzer(actionKind)
                    Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                    project = project.AddAnalyzerReference(analyzerReference)

                    Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                    Assert.Equal(1, descriptorsMap.Count)

                    Dim document = project.Documents.Single()
                    Dim diagnostics = diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None)
                    Assert.Equal(1, diagnostics.Count())
                    Dim diagnostic = diagnostics.First()
                    Assert.Equal(OperationAnalyzer.Descriptor.Id, diagnostic.Id)
                    Dim expectedMessage = String.Format(OperationAnalyzer.Descriptor.MessageFormat.ToString(), actionKind)
                    Assert.Equal(diagnostic.Message, expectedMessage)
                Next actionKind
            End Using
        End Sub

        <WpfFact, WorkItem(937939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937939"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestStatelessCodeBlockEndedAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New CodeBlockEndedAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Dim diagnostic = diagnostics.First()
                Assert.Equal(CodeBlockEndedAnalyzer.Descriptor.Id, diagnostic.Id)
            End Using
        End Sub

        <WpfFact, WorkItem(937939, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937939"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestSameCodeBlockStartedAndEndedAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAndEndedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Ensure no duplicate diagnostics.
                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Dim diagnostic = diagnostics.First()
                Assert.Equal(CodeBlockEndedAnalyzer.Descriptor.Id, diagnostic.Id)
            End Using
        End Sub

        <WpfFact, WorkItem(1005568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1005568"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestCodeBlockAnalyzerForLambda()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs"><![CDATA[
using System;
class AnonymousFunctions
{
    public void SimpleLambdaFunctionWithoutBraces()
    {
        bool x = false;
        Action<string> lambda = s => x = true;
        lambda("");
        Console.WriteLine(x);
    }

    public void SimpleLambdaFunctionWithBraces()
    {
        bool x = false;
        Action<string> lambda = s =>
        {
            x = true;
        };

        lambda("");
        Console.WriteLine(x);
    }

    public void ParenthesizedLambdaFunctionWithoutBraces()
    {
        bool x = false;
        Action<string> lambda = (s) => x = true;
        lambda("");
        Console.WriteLine(x);
    }

    public void ParenthesizedLambdaFunctionWithBraces()
    {
        bool x = false;
        Action<string> lambda = (s) =>
        {
            x = true;
        };

        lambda("");
        Console.WriteLine(x);
    }
}]]>
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAndEndedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Ensure no duplicate diagnostics.
                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(4, diagnostics.Count())
                Dim diagnostic = diagnostics.First()
                Assert.Equal(CodeBlockEndedAnalyzer.Descriptor.Id, diagnostic.Id)
            End Using
        End Sub

        <WpfFact, WorkItem(937952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937952"), WorkItem(944832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/944832"), WorkItem(1112907, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112907"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestCompilationEndedAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' turn off heuristic
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(InternalDiagnosticsOptions.UseCompilationEndCodeFixHeuristic, False)))

                Dim solution = workspace.CurrentSolution
                Dim analyzer = New CompilationEndedAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                ' Ask for document diagnostics multiple times, and verify compilation diagnostics are reported.
                Dim document = project.Documents.Single()

                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(document.Id, diagnostics.First().DocumentId)

                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(document.Id, diagnostics.First().DocumentId)

                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(document.Id, diagnostics.First().DocumentId)

                ' Verify compilation diagnostics are reported with correct location info when asked for project diagnostics.
                Dim projectDiagnostics = diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(2, projectDiagnostics.Count())

                Dim noLocationDiagnostic = projectDiagnostics.First(Function(d) Not d.HasTextSpan)
                Assert.Equal(CompilationEndedAnalyzer.Descriptor.Id, noLocationDiagnostic.Id)
                Assert.Equal(False, noLocationDiagnostic.HasTextSpan)

                Dim withDocumentLocationDiagnostic = projectDiagnostics.First(Function(d) d.HasTextSpan)
                Assert.Equal(CompilationEndedAnalyzer.Descriptor.Id, withDocumentLocationDiagnostic.Id)
                Assert.Equal(True, withDocumentLocationDiagnostic.HasTextSpan)
                Assert.NotNull(withDocumentLocationDiagnostic.DocumentId)

                Dim diagnosticDocument = project.GetDocument(withDocumentLocationDiagnostic.DocumentId)
                Dim tree = diagnosticDocument.GetSyntaxTreeAsync().Result
                Dim actualLocation = withDocumentLocationDiagnostic.ToDiagnosticAsync(project, CancellationToken.None).Result.Location
                Dim expectedLocation = document.GetSyntaxRootAsync().Result.GetLocation
                Assert.Equal(expectedLocation, actualLocation)
            End Using
        End Sub

        <Fact, WorkItem(1083854, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1083854"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestStatefulCompilationAnalyzer() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New StatefulCompilationAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim projectDiagnostics = Await DiagnosticProviderTestUtilities.GetProjectDiagnosticsAsync(project)
                Assert.Equal(1, projectDiagnostics.Count())
                Dim diagnostic = projectDiagnostics.Single()
                Assert.Equal(StatefulCompilationAnalyzer.Descriptor.Id, diagnostic.Id)
                Dim expectedMessage = String.Format(StatefulCompilationAnalyzer.Descriptor.MessageFormat.ToString(), 1)
                Assert.Equal(expectedMessage, diagnostic.GetMessage)
            End Using
        End Function

        <WpfFact, WorkItem(248, "https://github.com/dotnet/roslyn/issues/248"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestStatefulCompilationAnalyzer_2() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New StatefulCompilationAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                ' Make couple of dummy invocations to GetDocumentDiagnostics.
                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                Dim documentDiagnostics = Await DiagnosticProviderTestUtilities.GetDocumentDiagnosticsAsync(document:=document, span:=fullSpan)
                documentDiagnostics = Await DiagnosticProviderTestUtilities.GetDocumentDiagnosticsAsync(document:=document, span:=fullSpan)

                ' Verify that the eventual compilation end diagnostics (and hence the analyzer state) is not affected by prior document analysis.
                Dim projectDiagnostics = Await DiagnosticProviderTestUtilities.GetProjectDiagnosticsAsync(project)
                Assert.Equal(1, projectDiagnostics.Count())
                Dim diagnostic = projectDiagnostics.Single()
                Assert.Equal(StatefulCompilationAnalyzer.Descriptor.Id, diagnostic.Id)
                Dim expectedMessage = String.Format(StatefulCompilationAnalyzer.Descriptor.MessageFormat.ToString(), 1)
                Assert.Equal(expectedMessage, diagnostic.GetMessage)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestStatefulCompilationAnalyzer_FileLoadFailure() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Goo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim documentId = solution.Projects.Single().DocumentIds.Single()
                solution = solution.WithDocumentTextLoader(documentId, New FailingTextLoader("Test.cs"), PreservationMode.PreserveIdentity)
                workspace.ChangeSolution(solution)

                Dim project = solution.Projects.Single()
                Dim document = project.Documents.Single()

                Dim analyzer = New StatefulCompilationAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim projectDiagnostics = Await DiagnosticProviderTestUtilities.GetProjectDiagnosticsAsync(project)

                ' The analyzer is invoked but the compilation does not contain a syntax tree that failed to load.
                AssertEx.Equal(
                {
                    "StatefulCompilationAnalyzerDiagnostic: Compilation NamedType Count: 0"
                }, projectDiagnostics.Select(Function(d) d.Id & ": " & d.GetMessage()))

                Dim documentDiagnostics = Await DiagnosticProviderTestUtilities.GetDocumentDiagnosticsAsync(document, TextSpan.FromBounds(0, 0))
                AssertEx.Equal(
                {
                    "IDE1100: " & String.Format(WorkspacesResources.Error_reading_content_of_source_file_0_1, "Test.cs", "Bad data!")
                }, documentDiagnostics.Select(Function(d) d.Id & ": " & d.GetMessage()))

            End Using
        End Function

        <WpfFact, WorkItem(9462, "https://github.com/dotnet/roslyn/issues/9462"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestMultiplePartialDefinitionsInAFile()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   partial class Goo { }
                                   partial class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New NamedTypeAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)
                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                ' Verify no duplicate analysis/diagnostics.
                Dim document = project.Documents.Single()
                Dim diagnostics = diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id) _
                    .WaitAndGetResult(CancellationToken.None) _
                    .Select(Function(d) d.Id = NamedTypeAnalyzer.DiagDescriptor.Id)

                Assert.Equal(1, diagnostics.Count)
            End Using
        End Sub

        <WpfFact, WorkItem(1042914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042914"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestPartialTypeInGeneratedCode()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Goo.generated.cs">
                                   public partial class Goo { }
                               </Document>
                               <Document FilePath="Test1.cs">
                                   public partial class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Test partial type diagnostic reported on user file.
                Dim analyzer = New PartialTypeDiagnosticAnalyzer(indexOfDeclToReportDiagnostic:=1)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single(Function(d) d.Name = "Test1.cs")
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(PartialTypeDiagnosticAnalyzer.DiagDescriptor.Id, diagnostics.Single().Id)
            End Using
        End Sub

        <WpfFact, WorkItem(1042914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042914"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticsReportedOnAllPartialDefinitions()
            ' Test partial type diagnostic reported on all source files.
            Dim analyzer = New PartialTypeDiagnosticAnalyzer(indexOfDeclToReportDiagnostic:=Nothing)
            TestDiagnosticsReportedOnAllPartialDefinitionsCore(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
        End Sub

        <WpfFact, WorkItem(3748, "https://github.com/dotnet/roslyn/issues/3748"), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticsReportedOnAllPartialDefinitions2()
            ' Test partial type diagnostic reported on all source files with multiple analyzers.

            ' NOTE: This test is written to guard a race condition, which originally reproed only when the driver processes 'dummyAnalyzer' before 'analyzer'.
            ' As this is non-deterministic, we execute it within a loop to increase the chance of failure if this regresses again.
            For i = 0 To 10
                Dim dummyAnalyzer = New DummySymbolAnalyzer()
                Dim analyzer = New PartialTypeDiagnosticAnalyzer(indexOfDeclToReportDiagnostic:=Nothing)
                TestDiagnosticsReportedOnAllPartialDefinitionsCore(ImmutableArray.Create(Of DiagnosticAnalyzer)(dummyAnalyzer, analyzer))
            Next i
        End Sub

        Private Shared Sub TestDiagnosticsReportedOnAllPartialDefinitionsCore(analyzers As ImmutableArray(Of DiagnosticAnalyzer))
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test1.cs">
                                   public partial class Goo { }
                               </Document>
                               <Document FilePath="Test2.cs">
                                   public partial class Goo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Test partial type diagnostic reported on all source files.
                Dim analyzerReference = New AnalyzerImageReference(analyzers)
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                ' Verify project diagnostics contains diagnostics reported on both partial definitions.
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(2, diagnostics.Count())
                Dim file1HasDiag = False, file2HasDiag = False
                For Each diagnostic In diagnostics
                    Assert.Equal(PartialTypeDiagnosticAnalyzer.DiagDescriptor.Id, diagnostic.Id)
                    Dim document = project.GetDocument(diagnostic.DocumentId)
                    If document.Name = "Test1.cs" Then
                        file1HasDiag = True
                    ElseIf document.Name = "Test2.cs" Then
                        file2HasDiag = True
                    End If
                Next

                Assert.True(file1HasDiag)
                Assert.True(file2HasDiag)
            End Using
        End Sub

        <WpfFact, WorkItem(1067286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067286")>
        Private Sub TestCodeBlockAnalyzersForExpressionBody()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
public class B
{
    public int Property => 0;
    public int Method() => 0;
    public int this[int i] => 0;
}
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Test code block analyzer
                Dim analyzer As DiagnosticAnalyzer = New CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer:=True)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(6, diagnostics.Count())
                Assert.Equal(3, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor1.Id).Count)
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor4.Id).Count)
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor5.Id).Count)
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor6.Id).Count)
            End Using
        End Sub

        <WpfFact, WorkItem(592, "https://github.com/dotnet/roslyn/issues/592")>
        Private Sub TestSyntaxNodeAnalyzersForExpressionBody()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
public class B
{
    public int Property => 0;
    public int Method() => 0;
    public int this[int i] => 0;
}
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Test syntax node analyzer
                Dim analyzer As DiagnosticAnalyzer = New CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer:=False)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)

                Assert.Equal(3, diagnostics.Count())
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor4.Id).Count)
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor5.Id).Count)
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = CodeBlockOrSyntaxNodeAnalyzer.Descriptor6.Id).Count)
            End Using
        End Sub

        <WpfFact, WorkItem(592, "https://github.com/dotnet/roslyn/issues/592")>
        Private Sub TestMethodSymbolAnalyzersForExpressionBody()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
public class B
{
    public int Property => 0;
    public int Method() => 0;
    public int this[int i] => 0;
}
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Test method symbol analyzer
                Dim analyzer As DiagnosticAnalyzer = New MethodSymbolAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).
                    WaitAndGetResult(CancellationToken.None).
                    OrderBy(Function(d) d.GetTextSpan().Start).ToArray

                Assert.Equal(3, diagnostics.Count)
                Assert.True(diagnostics.All(Function(d) d.Id = MethodSymbolAnalyzer.Descriptor.Id))
                Assert.Equal("B.Property.get", diagnostics(0).Message)
                Assert.Equal("B.Method()", diagnostics(1).Message)
                Assert.Equal("B.this[int].get", diagnostics(2).Message)
            End Using
        End Sub

        <WpfFact, WorkItem(1109105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109105")>
        Public Sub TestMethodSymbolAnalyzer_MustOverrideMethod()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Public MustInherit Class Class1

    Public MustOverride Function Goo(x As Integer, y As Integer) As Integer

End Class

Public Class Class2

    Public Function Goo(x As Integer, y As Integer) As Integer
        Return x + y
    End Function

End Class
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New MustOverrideMethodAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(1, diagnostics.Where(Function(d) d.Id = MustOverrideMethodAnalyzer.Descriptor1.Id).Count)
            End Using
        End Sub

        Public Class MustOverrideMethodAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor1 As New DiagnosticDescriptor("MustOverrideMethodDiagnostic", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.Method)
            End Sub

            Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                Dim method = DirectCast(context.Symbol, IMethodSymbol)
                If method.IsAbstract Then
                    Dim sourceLoc = context.Symbol.Locations.First(Function(l) l.IsInSource)
                    context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor1, sourceLoc))
                End If
            End Sub
        End Class

        <WpfFact, WorkItem(565, "https://github.com/dotnet/roslyn/issues/565")>
        Public Sub TestFieldDeclarationAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
public class B
{
    public string field0;
    public string field1, field2;
    public int field3 = 0, field4 = 1;
    public int field5, field6 = 1;
}
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New FieldDeclarationAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).
                    WaitAndGetResult(CancellationToken.None).
                    OrderBy(Function(d) d.GetTextSpan().Start).
                    ToArray()
                Assert.Equal(4, diagnostics.Length)
                Assert.Equal(4, diagnostics.Where(Function(d) d.Id = FieldDeclarationAnalyzer.Descriptor1.Id).Count)

                Assert.Equal("public string field0;", diagnostics(0).Message)
                Assert.Equal("public string field1, field2;", diagnostics(1).Message)
                Assert.Equal("public int field3 = 0, field4 = 1;", diagnostics(2).Message)
                Assert.Equal("public int field5, field6 = 1;", diagnostics(3).Message)
            End Using
        End Sub

        <WpfFact, WorkItem(27703, "https://github.com/dotnet/roslyn/issues/27703")>
        Public Sub TestDiagnosticsForSpanWorksWithEmptySpan()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
public class B
{
    public string field0;
    public string field1, field2;
    public int field3 = 0, field4 = 1;
    public int field5, field6 = 1;
}
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()
                Dim analyzer = New FieldDeclarationAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).
                    WaitAndGetResult(CancellationToken.None).
                    OrderBy(Function(d) d.GetTextSpan().Start).
                    ToArray()

                For Each diagnostic In diagnostics
                    Dim spanAtCaret = New TextSpan(diagnostic.DataLocation.SourceSpan.Value.Start, 0)
                    Dim otherDiagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, spanAtCaret).
                        WaitAndGetResult(CancellationToken.None).
                        ToArray()

                    Assert.Equal(1, otherDiagnostics.Length)
                    Assert.Equal(diagnostic.Message, otherDiagnostics(0).Message)
                Next
            End Using
        End Sub

        Public Class FieldDeclarationAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor1 As New DiagnosticDescriptor("FieldDeclarationDiagnostic", "DummyDescription", "{0}", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim fieldDecl = DirectCast(context.Node, CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor1, fieldDecl.GetLocation, fieldDecl.ToString()))
            End Sub
        End Class

        <WpfFact, WorkItem(530, "https://github.com/dotnet/roslyn/issues/530")>
        Public Sub TestCompilationAnalyzerWithAnalyzerOptions()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
public class B
{
}
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New CompilationAnalyzerWithAnalyzerOptions()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                ' Add additional document
                Dim additionalDocText = "First"
                Dim additionalDoc = project.AddAdditionalDocument("AdditionalDoc", additionalDocText)
                project = additionalDoc.Project

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                TestCompilationAnalyzerWithAnalyzerOptionsCore(project, additionalDocText, diagnosticService)

                ' Update additional document text
                Dim newAdditionalDocText = "Second"
                additionalDoc = additionalDoc.WithText(SourceText.From(newAdditionalDocText))
                project = additionalDoc.Project

                ' Verify updated additional document text
                TestCompilationAnalyzerWithAnalyzerOptionsCore(project, newAdditionalDocText, diagnosticService)
            End Using
        End Sub

        Private Shared Sub TestCompilationAnalyzerWithAnalyzerOptionsCore(project As Project, expectedDiagnosticMessage As String, diagnosticService As TestDiagnosticAnalyzerService)
            Dim descriptorsMap = project.Solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
            Assert.Equal(1, descriptorsMap.Count)

            Dim document = project.Documents.Single()
            Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

            Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).
                WaitAndGetResult(CancellationToken.None)
            Assert.Equal(1, diagnostics.Count())
            Assert.Equal(CompilationAnalyzerWithAnalyzerOptions.Descriptor.Id, diagnostics(0).Id)
            Assert.Equal(expectedDiagnosticMessage, diagnostics(0).Message)
        End Sub

        <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
        Private Class CompilationAnalyzerWithAnalyzerOptions
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = New DiagnosticDescriptor("CompilationAnalyzerWithAnalyzerOptionsDiagnostic",
                                                                                        "CompilationAnalyzerWithAnalyzerOptionsDiagnostic",
                                                                                        "{0}",
                                                                                        "CompilationAnalyzerWithAnalyzerOptionsDiagnostic",
                                                                                        DiagnosticSeverity.Warning,
                                                                                        isEnabledByDefault:=True)
            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCompilationStartAction(Sub(compilationContext As CompilationStartAnalysisContext)
                                                           ' Cache additional file text
                                                           Dim additionalFileText = compilationContext.Options.AdditionalFiles.Single().GetText().ToString()

                                                           compilationContext.RegisterSymbolAction(Sub(symbolContext As SymbolAnalysisContext)
                                                                                                       Dim diag = Diagnostic.Create(Descriptor, symbolContext.Symbol.Locations(0), additionalFileText)
                                                                                                       symbolContext.ReportDiagnostic(diag)
                                                                                                   End Sub, SymbolKind.NamedType)
                                                       End Sub)
            End Sub
        End Class

        <DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)>
        Private Class WorkspaceDiagnosticAnalyzer
            Inherits AbstractDiagnosticAnalyzer

            Public Shared ReadOnly Descriptor As DiagnosticDescriptor = New DiagnosticDescriptor("XX0000",
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

        Private Class TestDiagnosticAnalyzer1
            Inherits TestDiagnosticAnalyzer

            Public Sub New(index As Integer)
                MyBase.New(index)
            End Sub
        End Class

        Private Class TestDiagnosticAnalyzer2
            Inherits TestDiagnosticAnalyzer

            Public Sub New(index As Integer)
                MyBase.New(index)
            End Sub
        End Class

        Private Class TestDiagnosticAnalyzer3
            Inherits TestDiagnosticAnalyzer

            Public Sub New(index As Integer)
                MyBase.New(index)
            End Sub
        End Class

        Private MustInherit Class TestDiagnosticAnalyzer
            Inherits AbstractDiagnosticAnalyzer

            Public ReadOnly Descriptor As DiagnosticDescriptor

            Public Sub New(index As Integer)
                Descriptor = New DiagnosticDescriptor(
                    "XX000" + index.ToString,
                    "ProjectDiagnosticDescription",
                    "ProjectDiagnosticMessage",
                    "ProjectDiagnosticCategory",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault:=True)
            End Sub

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

            Private Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                context.ReportDiagnostic(Diagnostic.Create(DiagDescriptor, context.Symbol.Locations.First(), context.Symbol.Locations.Skip(1)))
            End Sub
        End Class

        Private Class PartialTypeDiagnosticAnalyzer
            Inherits DiagnosticAnalyzer

            Private ReadOnly _indexOfDeclToReportDiagnostic As Integer?
            Public Sub New(indexOfDeclToReportDiagnostic As Integer?)
                Me._indexOfDeclToReportDiagnostic = indexOfDeclToReportDiagnostic
            End Sub

            Public Shared ReadOnly DiagDescriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(DiagDescriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
            End Sub

            Private Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                Dim index = 0
                For Each location In context.Symbol.Locations
                    If Not Me._indexOfDeclToReportDiagnostic.HasValue OrElse Me._indexOfDeclToReportDiagnostic.Value = index Then
                        context.ReportDiagnostic(Diagnostic.Create(DiagDescriptor, location))
                    End If

                    index += 1
                Next

            End Sub
        End Class

        Private Class NamedTypeAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared ReadOnly DiagDescriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic")
            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(DiagDescriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCompilationStartAction(Sub(compStartContext As CompilationStartAnalysisContext)
                                                           Dim symbols = New HashSet(Of ISymbol)
                                                           compStartContext.RegisterSymbolAction(Sub(sc As SymbolAnalysisContext)
                                                                                                     If (symbols.Contains(sc.Symbol)) Then
                                                                                                         Throw New Exception("Duplicate symbol callback")
                                                                                                     End If
                                                                                                     sc.ReportDiagnostic(Diagnostic.Create(DiagDescriptor, sc.Symbol.Locations.First()))
                                                                                                 End Sub, SymbolKind.NamedType)
                                                       End Sub)
            End Sub
        End Class

        Private Class DummySymbolAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared ReadOnly DiagDescriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(DiagDescriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
            End Sub

            Private Sub AnalyzeSymbol(context As SymbolAnalysisContext)
            End Sub
        End Class

        Private Class ThrowsExceptionAnalyzer
            Inherits DiagnosticAnalyzer

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKindsOfInterest)
            End Sub

            Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                Throw New NotImplementedException()
            End Sub

            Private ReadOnly Property SymbolKindsOfInterest As SymbolKind()
                Get
                    Throw New NotImplementedException()
                End Get
            End Property
        End Class

        Private Class CodeBlockStartedAnalyzer(Of TLanguageKindEnum As Structure)
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Sub CreateAnalyzerWithinCodeBlock(context As CodeBlockStartAnalysisContext(Of TLanguageKindEnum))
                context.RegisterCodeBlockEndAction(AddressOf (New CodeBlockEndedAnalyzer).AnalyzeCodeBlock)
            End Sub

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCodeBlockStartAction(Of TLanguageKindEnum)(AddressOf CreateAnalyzerWithinCodeBlock)
                ' Register a semantic model action that doesn't do anything to make sure that doesn't confuse anything.
                context.RegisterSemanticModelAction(Sub(sm) Return)
            End Sub

            Private Class CodeBlockEndedAnalyzer
                Public Sub AnalyzeCodeBlock(context As CodeBlockAnalysisContext)
                    Throw New NotImplementedException()
                End Sub
            End Class
        End Class

        Private Class CodeBlockEndedAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCodeBlockAction(AddressOf AnalyzeCodeBlock)
                ' Register a compilation start action that doesn't do anything to make sure that doesn't confuse anything.
                context.RegisterCompilationStartAction(Sub(c) Return)
                ' Register a compilation action that doesn't do anything to make sure that doesn't confuse anything.
                context.RegisterCompilationAction(Sub(c) Return)
            End Sub

            Public Sub AnalyzeCodeBlock(context As CodeBlockAnalysisContext)
                Assert.NotNull(context.CodeBlock)
                Assert.NotNull(context.OwningSymbol)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.CodeBlock.GetLocation))
            End Sub
        End Class

        Private Class CodeBlockStartedAndEndedAnalyzer(Of TLanguageKindEnum As Structure)
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("DummyDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCodeBlockStartAction(Of TLanguageKindEnum)(AddressOf CreateAnalyzerWithinCodeBlock)
                ' Register a compilation action that doesn't do anything to make sure that doesn't confuse anything.
                context.RegisterCompilationAction(Sub(c) Return)
            End Sub

            Public Sub AnalyzeCodeBlock(context As CodeBlockAnalysisContext)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.CodeBlock.GetLocation))
            End Sub

            Public Sub CreateAnalyzerWithinCodeBlock(context As CodeBlockStartAnalysisContext(Of TLanguageKindEnum))
                context.RegisterCodeBlockEndAction(AddressOf AnalyzeCodeBlock)
            End Sub
        End Class

        Private Class CompilationEndedAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("CompilationEndedAnalyzerDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                ' Register a symbol analyzer that doesn't do anything to verify that that doesn't confuse anything.
                context.RegisterSymbolAction(Sub(s) Return, SymbolKind.NamedType)
                context.RegisterCompilationAction(AddressOf AnalyzeCompilation)
            End Sub

            Private Shared Sub AnalyzeCompilation(context As CompilationAnalysisContext)
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None))
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Compilation.SyntaxTrees(0).GetRoot().GetLocation))
            End Sub
        End Class

        Private Class StatefulCompilationAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = New DiagnosticDescriptor("StatefulCompilationAnalyzerDiagnostic",
                                                                                          "",
                                                                                          "Compilation NamedType Count: {0}",
                                                                                          "",
                                                                                          DiagnosticSeverity.Warning,
                                                                                          isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCompilationStartAction(AddressOf OnCompilationStarted)
            End Sub

            Private Shared Sub OnCompilationStarted(context As CompilationStartAnalysisContext)
                Dim compilationAnalyzer = New CompilationAnalyzer
                context.RegisterSymbolAction(AddressOf compilationAnalyzer.AnalyzeSymbol, SymbolKind.NamedType)
                context.RegisterCompilationEndAction(AddressOf compilationAnalyzer.AnalyzeCompilation)
            End Sub

            Private Class CompilationAnalyzer
                Private ReadOnly _symbolNames As New List(Of String)

                Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                    _symbolNames.Add(context.Symbol.Name)
                End Sub

                Public Sub AnalyzeCompilation(context As CompilationAnalysisContext)
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.None, _symbolNames.Count))
                End Sub
            End Class
        End Class

        Private Class CodeBlockOrSyntaxNodeAnalyzer
            Inherits DiagnosticAnalyzer

            Private ReadOnly _isCodeBlockAnalyzer As Boolean

            Public Shared Descriptor1 As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("CodeBlockDiagnostic")
            Public Shared Descriptor2 As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("EqualsValueDiagnostic")
            Public Shared Descriptor3 As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("ConstructorInitializerDiagnostic")
            Public Shared Descriptor4 As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("PropertyExpressionBodyDiagnostic")
            Public Shared Descriptor5 As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("IndexerExpressionBodyDiagnostic")
            Public Shared Descriptor6 As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("MethodExpressionBodyDiagnostic")

            Public Sub New(isCodeBlockAnalyzer As Boolean)
                _isCodeBlockAnalyzer = isCodeBlockAnalyzer
            End Sub

            Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor1, Descriptor2, Descriptor3, Descriptor4, Descriptor5, Descriptor6)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                If _isCodeBlockAnalyzer Then
                    context.RegisterCodeBlockStartAction(Of CodeAnalysis.CSharp.SyntaxKind)(AddressOf OnCodeBlockStarted)
                    context.RegisterCodeBlockAction(AddressOf OnCodeBlockEnded)
                Else
                    Dim analyzer = New NodeAnalyzer
                    analyzer.Initialize(Sub(action, Kinds) context.RegisterSyntaxNodeAction(action, Kinds))
                End If
            End Sub

            Public Shared Sub OnCodeBlockEnded(context As CodeBlockAnalysisContext)
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor1, context.CodeBlock.GetLocation()))
            End Sub

            Public Shared Sub OnCodeBlockStarted(context As CodeBlockStartAnalysisContext(Of CodeAnalysis.CSharp.SyntaxKind))
                Dim analyzer = New NodeAnalyzer
                analyzer.Initialize(Sub(action, Kinds) context.RegisterSyntaxNodeAction(action, Kinds))
            End Sub

            Protected Class NodeAnalyzer
                Public Sub Initialize(registerSyntaxNodeAction As Action(Of Action(Of SyntaxNodeAnalysisContext), ImmutableArray(Of CodeAnalysis.CSharp.SyntaxKind)))
                    registerSyntaxNodeAction(Sub(context)
                                                 context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor2, context.Node.GetLocation()))
                                             End Sub, ImmutableArray.Create(CodeAnalysis.CSharp.SyntaxKind.EqualsValueClause))

                    registerSyntaxNodeAction(Sub(context)
                                                 context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor3, context.Node.GetLocation()))
                                             End Sub, ImmutableArray.Create(CodeAnalysis.CSharp.SyntaxKind.BaseConstructorInitializer))

                    registerSyntaxNodeAction(Sub(context)
                                                 Dim descriptor As DiagnosticDescriptor
                                                 Select Case CodeAnalysis.CSharp.CSharpExtensions.Kind(context.Node.Parent)
                                                     Case CodeAnalysis.CSharp.SyntaxKind.PropertyDeclaration
                                                         descriptor = Descriptor4
                                                         Exit Select

                                                     Case CodeAnalysis.CSharp.SyntaxKind.IndexerDeclaration
                                                         descriptor = Descriptor5
                                                         Exit Select
                                                     Case Else

                                                         descriptor = Descriptor6
                                                         Exit Select
                                                 End Select

                                                 context.ReportDiagnostic(Diagnostic.Create(descriptor, context.Node.GetLocation))

                                             End Sub, ImmutableArray.Create(CodeAnalysis.CSharp.SyntaxKind.ArrowExpressionClause))
                End Sub
            End Class
        End Class

        Private Class MethodSymbolAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared Descriptor As DiagnosticDescriptor = New DiagnosticDescriptor("MethodSymbolDiagnostic",
                                                                                        "MethodSymbolDiagnostic",
                                                                                        "{0}",
                                                                                        "MethodSymbolDiagnostic",
                                                                                        DiagnosticSeverity.Warning,
                                                                                        isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(Sub(ctxt)
                                                 Dim method = (DirectCast(ctxt.Symbol, IMethodSymbol))
                                                 ctxt.ReportDiagnostic(Diagnostic.Create(Descriptor, method.Locations(0), method.ToDisplayString))
                                             End Sub, SymbolKind.Method)
            End Sub
        End Class

        <WpfFact, WorkItem(1709, "https://github.com/dotnet/roslyn/issues/1709")>
        Public Sub TestCodeBlockAction()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
class C
{
    public void M() {}
}
                               </Document>
                           </Project>
                       </Workspace>

            TestCodeBlockActionCore(test)

            test = <Workspace>
                       <Project Language="Visual Basic" CommonReferences="true">
                           <Document>
Class C 
    Public Sub M()
    End Sub
End Class
                            </Document>
                       </Project>
                   </Workspace>

            TestCodeBlockActionCore(test)
        End Sub

        <WpfFact, WorkItem(1709, "https://github.com/dotnet/roslyn/issues/1709")>
        Public Sub TestCodeBlockAction_OnlyStatelessAction()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
class C
{
    public void M() {}
}
                               </Document>
                           </Project>
                       </Workspace>

            TestCodeBlockActionCore(test, onlyStatelessAction:=True)

            test = <Workspace>
                       <Project Language="Visual Basic" CommonReferences="true">
                           <Document>
Class C 
    Public Sub M()
    End Sub
End Class
                            </Document>
                       </Project>
                   </Workspace>

            TestCodeBlockActionCore(test, onlyStatelessAction:=True)
        End Sub

        Private Sub TestCodeBlockActionCore(test As XElement, Optional onlyStatelessAction As Boolean = False)
            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New CodeBlockActionAnalyzer(onlyStatelessAction)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Dim expectedCount = If(onlyStatelessAction, 1, 2)
                Assert.Equal(expectedCount, diagnostics.Count())

                Dim diagnostic = diagnostics.Single(Function(d) d.Id = CodeBlockActionAnalyzer.CodeBlockTopLevelRule.Id)
                Assert.Equal("CodeBlock : M", diagnostic.Message)

                Dim stateFullDiagnostics = diagnostics.Where(Function(d) d.Id = CodeBlockActionAnalyzer.CodeBlockPerCompilationRule.Id)
                Assert.Equal(expectedCount - 1, stateFullDiagnostics.Count)
                If Not onlyStatelessAction Then
                    Assert.Equal("CodeBlock : M", stateFullDiagnostics.Single().Message)
                End If
            End Using
        End Sub

        <WpfFact, WorkItem(2614, "https://github.com/dotnet/roslyn/issues/2614")>
        Public Sub TestGenericName()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document><![CDATA[
using System;
using System.Text;

namespace ConsoleApplication1
{
    class MyClass
    {   
        private Nullable<int> myVar = 5;
        void Method()
        {

        }
    }
}]]>
                               </Document>
                           </Project>
                       </Workspace>

            TestGenericNameCore(test, CSharpGenericNameAnalyzer.Message, CSharpGenericNameAnalyzer.DiagnosticId, New CSharpGenericNameAnalyzer)
        End Sub

        Private Sub TestGenericNameCore(test As XElement, expectedMessage As String, expectedId As String, ParamArray analyzers As DiagnosticAnalyzer())
            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Add analyzer
                Dim analyzerReference = New AnalyzerImageReference(analyzers.ToImmutableArray())
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())

                Dim diagnostic = diagnostics.Single(Function(d) d.Id = expectedId)
                Assert.Equal(expectedMessage, diagnostic.Message)
            End Using
        End Sub

        <WpfFact, WorkItem(2980, "https://github.com/dotnet/roslyn/issues/2980")>
        Public Sub TestAnalyzerWithNoActions()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document><![CDATA[
using System;
using System.Text;

namespace ConsoleApplication1
{
    class MyClass
    {   
        private Nullable<int> myVar = 5;
        void Method()
        {

        }
    }
}]]>
                               </Document>
                           </Project>
                       </Workspace>

            ' Ensure that adding a dummy analyzer with no actions doesn't bring down entire analysis.
            ' See https//github.com/dotnet/roslyn/issues/2980 for details.
            TestGenericNameCore(test, CSharpGenericNameAnalyzer.Message, CSharpGenericNameAnalyzer.DiagnosticId, New AnalyzerWithNoActions, New CSharpGenericNameAnalyzer)
        End Sub

        <WpfFact, WorkItem(4055, "https://github.com/dotnet/roslyn/issues/4055")>
        Public Sub TestAnalyzerWithNoSupportedDiagnostics()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document><![CDATA[
class MyClass
{
}]]>
                               </Document>
                           </Project>
                       </Workspace>

            ' Ensure that adding a dummy analyzer with no supported diagnostics doesn't bring down entire analysis.
            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New AnalyzerWithNoSupportedDiagnostics()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                ' Verify available diagnostic descriptors/analyzers
                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)
                Assert.Equal(0, descriptorsMap.First().Value.Length)

                Dim document = project.Documents.Single()
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                    document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                    ).WaitAndGetResult(CancellationToken.None)

                Assert.Equal(0, diagnostics.Count())
            End Using
        End Sub

        <WpfFact, WorkItem(4068, "https://github.com/dotnet/roslyn/issues/4068")>
        Public Sub TestAnalyzerWithCompilationActionReportingHiddenDiagnostics()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document><![CDATA[
class MyClass
{
}]]>
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution)))

                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New CompilationAnalyzerWithSeverity(DiagnosticSeverity.Hidden, configurable:=False)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                ' Verify available diagnostic descriptors
                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Length)
                Assert.Equal(analyzer.Descriptor.Id, descriptors.Single().Id)

                ' Force project analysis
                incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged:=True, reasons:=InvocationReasons.Empty, cancellationToken:=CancellationToken.None).Wait()

                ' Get cached project diagnostics.
                Dim diagnostics = diagnosticService.GetCachedDiagnosticsAsync(workspace, project.Id).WaitAndGetResult(CancellationToken.None)

                ' in v2, solution crawler never creates non-local hidden diagnostics.
                ' v2 still creates those for LB and explicit queries such as FixAll.
                Dim expectedCount = 0
                Assert.Equal(expectedCount, diagnostics.Count())

                ' Get diagnostics explicitly
                Dim hiddenDiagnostics = diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, hiddenDiagnostics.Count())
                Assert.Equal(analyzer.Descriptor.Id, hiddenDiagnostics.Single().Id)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestEnsureNoMergedNamespaceSymbolAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" AssemblyName="BaseAssembly" CommonReferences="true">
                               <Document>
                                   namespace N1.N2 { class C1 { } }
                               </Document>
                           </Project>
                           <Project Language="C#" AssemblyName="MainAssembly" CommonReferences="true">
                               <ProjectReference>BaseAssembly</ProjectReference>
                               <Document>
                                   namespace N1.N2 { class C2 { } }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspace.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects.Single(Function(p As Project) p.Name = "MainAssembly")

                ' Analyzer reports a diagnostic if it receives a merged namespace symbol across assemblies in compilation.
                Dim analyzer = New EnsureNoMergedNamespaceSymbolAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim mefExportProvider = DirectCast(workspace.Services.HostServices, IMefHostExportProvider)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(0, diagnostics.Count())
            End Using
        End Sub
    End Class
End Namespace
