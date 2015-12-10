' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests

    Public Class DiagnosticServiceTests

        Private _assemblyLoader As IAnalyzerAssemblyLoader = New InMemoryAssemblyLoader()

        Public Function CreateAnalyzerFileReference(ByVal fullPath As String) As AnalyzerFileReference
            Return New AnalyzerFileReference(fullPath, _assemblyLoader)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestProjectAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim solution = workspace.CurrentSolution
                Dim project = solution.Projects(0)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()
                Dim projectDiagnosticAnalyzer1 = New ProjectDiagnosticAnalyzer(1)
                Dim projectDiagnosticAnalyzer2 = New ProjectDiagnosticAnalyzer2(2)

                Dim diagnosticService = New TestDiagnosticAnalyzerService(LanguageNames.CSharp, workspaceDiagnosticAnalyzer)

                ' Verify available diagnostic descriptors/analyzers
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
                descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
                descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
                descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
                descriptorsMap = diagnosticService.GetDiagnosticDescriptors(projectOpt:=Nothing)
                Assert.Equal(1, descriptorsMap.Count)
                descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Count)
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)

                ' Add an existing workspace analyzer to the project, ensure no duplicate diagnostics.
                Dim duplicateProjectAnalyzers = ImmutableArray.Create(Of DiagnosticAnalyzer)(workspaceDiagnosticAnalyzer)
                Dim duplicateProjectAnalyzersReference = New AnalyzerImageReference(duplicateProjectAnalyzers)
                project = project.WithAnalyzerReferences({duplicateProjectAnalyzersReference})

                ' Verify duplicate descriptors or diagnostics.
                ' We don't do de-duplication of analyzer that belong to different layer (host and project)
                descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                Assert.Equal(2, descriptorsMap.Count)
                descriptors = descriptorsMap.Values.SelectMany(Function(d) d).OrderBy(Function(d) d.Id).ToImmutableArray()
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)

                document = project.Documents.Single()
                diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                    document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                    ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(2, diagnostics.Count())
            End Using
        End Sub

        <WpfFact>
        Public Sub TestEmptyProjectAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects(0)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()

                Dim diagnosticService = New TestDiagnosticAnalyzerService(LanguageNames.CSharp, workspaceDiagnosticAnalyzer)

                ' Add project analyzer reference with no analyzers.
                Dim projectAnalyzersEmpty = ImmutableArray(Of DiagnosticAnalyzer).Empty
                Dim projectAnalyzerReference1 = New AnalyzerImageReference(projectAnalyzersEmpty)
                Dim projectAnalyzerReferences1 = ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference1)
                project = project.WithAnalyzerReferences(projectAnalyzerReferences1)

                ' Query descriptors twice: second query was hitting an assert in DiagnosticAnalyzersAndStates.
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)

                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Count())
                Assert.Equal(workspaceDiagnosticAnalyzer.DiagDescriptor.Id, descriptors(0).Id)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestNameCollisionOnDisplayNames()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects(0)

                Dim referenceName = "Test"

                Dim hostAnalyzerReference = New AnalyzerImageReference(
                    ImmutableArray.Create(Of DiagnosticAnalyzer)(New ProjectDiagnosticAnalyzer(0)), display:=referenceName)

                Dim projectAnalyzerReference = New AnalyzerImageReference(
                    ImmutableArray.Create(Of DiagnosticAnalyzer)(New ProjectDiagnosticAnalyzer(1)), display:=referenceName)

                Dim diagnosticService = New TestDiagnosticAnalyzerService(
                    ImmutableArray.Create(Of AnalyzerReference)(hostAnalyzerReference))

                project = project.WithAnalyzerReferences(ImmutableArray.Create(Of AnalyzerReference)(projectAnalyzerReference))

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)

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
                                        class Foo { }
                                    </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects(0)
                Dim workspaceDiagnosticAnalyzer = New WorkspaceDiagnosticAnalyzer()

                Dim diagnosticService = New TestDiagnosticAnalyzerService(LanguageNames.CSharp, workspaceDiagnosticAnalyzer)

                ' Verify available diagnostic descriptors/analyzers
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim projectDiagnosticAnalyzer1 = New ProjectDiagnosticAnalyzer(1)
                Dim projectDiagnosticAnalyzer2 = New ProjectDiagnosticAnalyzer2(2)

                Dim solution = workspace.CurrentSolution

                Dim alpha = solution.Projects.Single(Function(p) p.Language = LanguageNames.CSharp)
                alpha = alpha.WithAnalyzerReferences(SpecializedCollections.SingletonCollection(New AnalyzerImageReference(ImmutableArray(Of DiagnosticAnalyzer).Empty.Add(projectDiagnosticAnalyzer1))))
                solution = alpha.Solution

                Dim bravo = solution.Projects.Single(Function(p) p.Language = LanguageNames.VisualBasic)
                bravo = bravo.WithAnalyzerReferences(SpecializedCollections.SingletonCollection(New AnalyzerImageReference(ImmutableArray(Of DiagnosticAnalyzer).Empty.Add(projectDiagnosticAnalyzer2))))
                solution = bravo.Solution

                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                Dim workspaceDescriptors = diagnosticService.GetDiagnosticDescriptors(projectOpt:=Nothing)
                Assert.Equal(0, workspaceDescriptors.Count)

                Dim alphaDescriptors = diagnosticService.GetDiagnosticDescriptors(alpha)
                Assert.Equal("XX0001", alphaDescriptors.Single().Value.Single().Id)
                Dim alphaDiagnostics = diagnosticService.GetDiagnosticsForSpanAsync(alpha.Documents.Single(), New TextSpan(0, alpha.Documents.Single().GetTextAsync().Result.Length)).Result
                Assert.Equal("XX0001", alphaDiagnostics.Single().Id)

                Dim bravoDescriptors = diagnosticService.GetDiagnosticDescriptors(bravo)
                Assert.Equal("XX0002", bravoDescriptors.Single().Value.Single().Id)
                Dim bravoDiagnostics = diagnosticService.GetDiagnosticsForSpanAsync(bravo.Documents.Single(), New TextSpan(0, bravo.Documents.Single().GetTextAsync().Result.Length)).Result
                Assert.Equal("XX0002", bravoDiagnostics.Single().Id)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGlobalAnalyzerGroup()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Bravo.vb">
                                   Class Bravo : End Class
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim analyzer1 = New ProjectDiagnosticAnalyzer(1)
                Dim analyzer2 = New ProjectDiagnosticAnalyzer2(2)

                Dim analyzersMap = New Dictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer))
                analyzersMap.Add(LanguageNames.CSharp, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer1))
                analyzersMap.Add(LanguageNames.VisualBasic, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer2))
                Dim diagnosticService2 = New TestDiagnosticAnalyzerService(analyzersMap.ToImmutableDictionary())

                Dim descriptors = diagnosticService2.GetDiagnosticDescriptors(projectOpt:=Nothing)
                Assert.Equal(1, descriptors.Count)
                Assert.Equal(2, descriptors.Single().Value.Count)
            End Using
        End Sub

        <WpfFact, WorkItem(923324), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDuplicateFileAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzerReference1 = CreateAnalyzerFileReference("x:\temp.dll")
                Dim analyzerReference2 = CreateAnalyzerFileReference("x:\temp.dll")
                project = project.AddAnalyzerReference(analyzerReference1)
#If DEBUG Then
                Debug.Assert(project.AnalyzerReferences.Contains(analyzerReference2))
#End If
            End Using
        End Sub

        <WpfFact, WorkItem(1091877), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDuplicateFileAnalyzers2()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                ' Add duplicate analyzer references: one as VSIX analyzer reference and other one as project analyzer reference.
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzerReference1 = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location)
                project = project.AddAnalyzerReference(analyzerReference1)

                Dim analyzerReference2 = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location)
                Dim diagnosticService = New TestDiagnosticAnalyzerService(ImmutableArray.Create(Of AnalyzerReference)(analyzerReference2))

                Dim analyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)

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

        <WpfFact, WorkItem(923324), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDuplicateImageAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New ProjectDiagnosticAnalyzer(0)
                Dim analyzerReference1 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                Dim analyzerReference2 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference1)
                project = project.AddAnalyzerReference(analyzerReference2)
#If DEBUG Then
                Debug.Assert(project.AnalyzerReferences.Contains(analyzerReference1))
#End If
            End Using
        End Sub

        <WpfFact, WorkItem(937956), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticAnalyzerExceptionHandledGracefully()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New ThrowsExceptionAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(937915), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        <WorkItem(759)>
        Public Sub TestDiagnosticAnalyzerExceptionHandledGracefully2()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim exceptionDiagnosticsSource = New TestHostDiagnosticUpdateSource(workspace)
                Dim diagnosticService = New TestDiagnosticAnalyzerService(hostDiagnosticUpdateSource:=exceptionDiagnosticsSource)

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document,
                                                                        document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                                                                        ).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(0, diagnostics.Count())

                diagnostics = exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics(analyzer)
                Assert.Equal(1, diagnostics.Count())
                Dim diagnostic = diagnostics.First()
                Assert.True(diagnostic.Id = "AD0001")
                Assert.Contains("CodeBlockStartedAnalyzer", diagnostic.Message, StringComparison.Ordinal)
            End Using
        End Sub

        <WpfFact, WorkItem(1167439), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticAnalyzerExceptionHandledNoCrash()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs"/>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)

                Dim expected = Diagnostic.Create("test", "test", "test", DiagnosticSeverity.Error, DiagnosticSeverity.Error, True, 0)
                Dim exceptionDiagnosticsSource = New TestHostDiagnosticUpdateSource(workspace)

                ' check reporting diagnostic to a project that doesn't exist
                exceptionDiagnosticsSource.ReportAnalyzerDiagnostic(analyzer, expected, workspace, New ProjectId(Guid.NewGuid(), "dummy"))
                Dim diagnostics = exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics(analyzer)
                Assert.Equal(0, diagnostics.Count())

                ' check workspace diagnostic reporting
                exceptionDiagnosticsSource.ReportAnalyzerDiagnostic(analyzer, expected, workspace, Nothing)
                diagnostics = exceptionDiagnosticsSource.TestOnly_GetReportedDiagnostics(analyzer)

                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(expected.Id, diagnostics.First().Id)
            End Using
        End Sub

        <WpfFact, WorkItem(937939), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestOperationAnalyzers()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { void M() { int x = 0; } }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                For Each actionKind As OperationAnalyzer.ActionKind In [Enum].GetValues(GetType(OperationAnalyzer.ActionKind))
                    Dim project = workspace.CurrentSolution.Projects.Single
                    Dim analyzer = New OperationAnalyzer(actionKind)
                    Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                    project = project.AddAnalyzerReference(analyzerReference)

                    Dim descriptorsMap = DiagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(937939), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestStatelessCodeBlockEndedAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New CodeBlockEndedAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(937939), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestSameCodeBlockStartedAndEndedAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAndEndedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Ensure no duplicate diagnostics.
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(1005568), Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New CodeBlockStartedAndEndedAnalyzer(Of Microsoft.CodeAnalysis.CSharp.SyntaxKind)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                ' Ensure no duplicate diagnostics.
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(937952), WorkItem(944832), WorkItem(1112907), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestCompilationEndedAnalyzer()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' turn off heuristic
                Dim options = workspace.Services.GetService(Of IOptionService)()
                options.SetOptions(options.GetOptions.WithChangedOption(InternalDiagnosticsOptions.UseCompilationEndCodeFixHeuristic, False))

                Dim analyzer = New CompilationEndedAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
                Dim projectDiagnostics = diagnosticService.GetProjectDiagnosticsForIdsAsync(project.Solution, project.Id).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(2, projectDiagnostics.Count())

                Dim noLocationDiagnostic = projectDiagnostics.First()
                Assert.Equal(CompilationEndedAnalyzer.Descriptor.Id, noLocationDiagnostic.Id)
                Assert.Equal(False, noLocationDiagnostic.HasTextSpan)

                Dim withDocumentLocationDiagnostic = projectDiagnostics.Last()
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

        <WpfFact, WorkItem(1083854), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function TestStatefulCompilationAnalyzer() As Task
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                   class Foo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New StatefulCompilationAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim projectDiagnostics = Await DiagnosticProviderTestUtilities.GetProjectDiagnosticsAsync(workspaceAnalyzerOpt:=Nothing, project:=project)
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
                                   class Foo { void M() {} }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New StatefulCompilationAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                ' Make couple of dummy invocations to GetDocumentDiagnostics.
                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan
                Dim documentDiagnostics = Await DiagnosticProviderTestUtilities.GetDocumentDiagnosticsAsync(workspaceAnalyzerOpt:=Nothing, document:=document, span:=fullSpan)
                documentDiagnostics = Await DiagnosticProviderTestUtilities.GetDocumentDiagnosticsAsync(workspaceAnalyzerOpt:=Nothing, document:=document, span:=fullSpan)

                ' Verify that the eventual compilation end diagnostics (and hence the analyzer state) is not affected by prior document analysis.
                Dim projectDiagnostics = Await DiagnosticProviderTestUtilities.GetProjectDiagnosticsAsync(workspaceAnalyzerOpt:=Nothing, project:=project)
                Assert.Equal(1, projectDiagnostics.Count())
                Dim diagnostic = projectDiagnostics.Single()
                Assert.Equal(StatefulCompilationAnalyzer.Descriptor.Id, diagnostic.Id)
                Dim expectedMessage = String.Format(StatefulCompilationAnalyzer.Descriptor.MessageFormat.ToString(), 1)
                Assert.Equal(expectedMessage, diagnostic.GetMessage)
            End Using
        End Function

        <WpfFact, WorkItem(1042914), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestPartialTypeInGeneratedCode()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Foo.generated.cs">
                                   public partial class Foo { }
                               </Document>
                               <Document FilePath="Test1.cs">
                                   public partial class Foo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Test partial type diagnostic reported on user file.
                Dim analyzer = New PartialTypeDiagnosticAnalyzer(indexOfDeclToReportDiagnostic:=1)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single(Function(d) d.Name = "Test1.cs")
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).WaitAndGetResult(CancellationToken.None)
                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(PartialTypeDiagnosticAnalyzer.DiagDescriptor.Id, diagnostics.Single().Id)
            End Using
        End Sub

        <WpfFact, WorkItem(1042914), Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticsReportedOnAllPartialDefinitions()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test1.cs">
                                   public partial class Foo { }
                               </Document>
                               <Document FilePath="Test2.cs">
                                   public partial class Foo { }
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Test partial type diagnostic reported on all source files.
                Dim analyzer = New PartialTypeDiagnosticAnalyzer(indexOfDeclToReportDiagnostic:=Nothing)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
                    ElseIf document.Name = "Test2.cs"
                        file2HasDiag = True
                    End If
                Next

                Assert.True(file1HasDiag)
                Assert.True(file2HasDiag)
            End Using
        End Sub

        <WpfFact, WorkItem(1067286)>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Test code block analyzer
                Dim analyzer As DiagnosticAnalyzer = New CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer:=True)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(592)>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Test syntax node analyzer
                Dim analyzer As DiagnosticAnalyzer = New CodeBlockOrSyntaxNodeAnalyzer(isCodeBlockAnalyzer:=False)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(592)>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Test method symbol analyzer
                Dim analyzer As DiagnosticAnalyzer = New MethodSymbolAnalyzer
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).
                    WaitAndGetResult(CancellationToken.None).
                    OrderBy(Function(d) d.TextSpan.Start).ToArray

                Assert.Equal(3, diagnostics.Count)
                Assert.True(diagnostics.All(Function(d) d.Id = MethodSymbolAnalyzer.Descriptor.Id))
                Assert.Equal("B.Property.get", diagnostics(0).Message)
                Assert.Equal("B.Method()", diagnostics(1).Message)
                Assert.Equal("B.this[int].get", diagnostics(2).Message)
            End Using
        End Sub

        <WpfFact, WorkItem(1109105)>
        Public Sub TestMethodSymbolAnalyzer_MustOverrideMethod()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Public MustInherit Class Class1

    Public MustOverride Function Foo(x As Integer, y As Integer) As Integer

End Class

Public Class Class2

    Public Function Foo(x As Integer, y As Integer) As Integer
        Return x + y
    End Function

End Class
                               </Document>
                           </Project>
                       </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New MustOverrideMethodAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        <WpfFact, WorkItem(565)>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()
                Dim analyzer = New FieldDeclarationAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                Assert.Equal(1, descriptorsMap.Count)

                Dim document = project.Documents.Single()
                Dim fullSpan = document.GetSyntaxRootAsync().WaitAndGetResult(CancellationToken.None).FullSpan

                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)
                Dim diagnostics = diagnosticService.GetDiagnosticsForSpanAsync(document, fullSpan).
                    WaitAndGetResult(CancellationToken.None).
                    OrderBy(Function(d) d.TextSpan.Start).
                    ToArray()
                Assert.Equal(4, diagnostics.Length)
                Assert.Equal(4, diagnostics.Where(Function(d) d.Id = FieldDeclarationAnalyzer.Descriptor1.Id).Count)

                Assert.Equal("public string field0;", diagnostics(0).Message)
                Assert.Equal("public string field1, field2;", diagnostics(1).Message)
                Assert.Equal("public int field3 = 0, field4 = 1;", diagnostics(2).Message)
                Assert.Equal("public int field5, field6 = 1;", diagnostics(3).Message)
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

        <WpfFact, WorkItem(530)>
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New CompilationAnalyzerWithAnalyzerOptions()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                ' Add additional document
                Dim additionalDocText = "First"
                Dim additionalDoc = project.AddAdditionalDocument("AdditionalDoc", additionalDocText)
                project = additionalDoc.Project

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

        Private Shared Sub TestCompilationAnalyzerWithAnalyzerOptionsCore(project As Project, expectedDiagnosticMessage As String, diagnosticService As DiagnosticAnalyzerService)
            Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

        Private Class ProjectDiagnosticAnalyzer2
            Inherits ProjectDiagnosticAnalyzer

            Public Sub New(index As Integer)
                MyBase.New(index)
            End Sub
        End Class

        Private Class ProjectDiagnosticAnalyzer
            Inherits AbstractDiagnosticAnalyzer

            Private ReadOnly _index As Integer
            Public ReadOnly Descriptor As DiagnosticDescriptor

            Public Sub New(index As Integer)
                Me._index = index
                Me.Descriptor = New DiagnosticDescriptor("XX000" + index.ToString,
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
            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New CodeBlockActionAnalyzer(onlyStatelessAction)
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Add analyzer
                Dim analyzerReference = New AnalyzerImageReference(analyzers.ToImmutableArray())
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()

                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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
            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New AnalyzerWithNoSupportedDiagnostics()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                ' Verify available diagnostic descriptors/analyzers
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
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

            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' Add analyzer
                Dim analyzer = New HiddenDiagnosticsCompilationAnalyzer()
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                project = project.AddAnalyzerReference(analyzerReference)

                Dim diagnosticService = New TestDiagnosticAnalyzerService()
                Dim incrementalAnalyzer = diagnosticService.CreateIncrementalAnalyzer(workspace)

                ' Verify available diagnostic descriptors
                Dim descriptorsMap = diagnosticService.GetDiagnosticDescriptors(project)
                Assert.Equal(1, descriptorsMap.Count)
                Dim descriptors = descriptorsMap.First().Value
                Assert.Equal(1, descriptors.Length)
                Assert.Equal(HiddenDiagnosticsCompilationAnalyzer.Descriptor.Id, descriptors.Single().Id)

                ' Force project analysis
                incrementalAnalyzer.AnalyzeProjectAsync(project, semanticsChanged:=True, cancellationToken:=CancellationToken.None).Wait()

                ' Get cached project diagnostics.
                Dim diagnostics = diagnosticService.GetCachedDiagnosticsAsync(workspace, project.Id).WaitAndGetResult(CancellationToken.None)

                Assert.Equal(1, diagnostics.Count())
                Assert.Equal(HiddenDiagnosticsCompilationAnalyzer.Descriptor.Id, diagnostics.Single().Id)
            End Using
        End Sub
    End Class
End Namespace
