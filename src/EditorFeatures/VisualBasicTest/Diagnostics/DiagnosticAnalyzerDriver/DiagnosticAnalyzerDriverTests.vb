' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics

<[UseExportProvider]>
Public Class DiagnosticAnalyzerDriverTests

    Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures

    <Fact>
    Public Async Function DiagnosticAnalyzerDriverAllInOne() As Task
        Dim source = TestResource.AllInOneVisualBasicCode
        Dim analyzer = New BasicTrackingDiagnosticAnalyzer()
        Using workspace = TestWorkspace.CreateVisualBasic(source, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
            Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
            Dim newSolution = workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}).
                Projects.Single().AddAdditionalDocument(name:="dummy.txt", text:="", filePath:="dummy.txt").Project.Solution
            workspace.TryApplyChanges(newSolution)

            Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
            AccessSupportedDiagnostics(analyzer)
            Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, New TextSpan(0, document.GetTextAsync().Result.Length))
            analyzer.VerifyAllAnalyzerMembersWereCalled()
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds()
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds(New HashSet(Of SyntaxKind)())
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(allowUnexpectedCalls:=True)
        End Using
    End Function

    <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908658")>
    Public Async Function DiagnosticAnalyzerDriverVsAnalyzerDriverOnCodeBlock() As Task
        Dim methodNames As String() = {"Initialize", "AnalyzeCodeBlock"}
        Dim source = <file><![CDATA[
<System.Obsolete>
Class C
    Property P As Integer = 0
    Event E()
End Class
]]></file>

        Dim ideEngineAnalyzer = New BasicTrackingDiagnosticAnalyzer()
        Using ideEngineWorkspace = TestWorkspace.CreateVisualBasic(source.Value, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
            Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(ideEngineAnalyzer))
            ideEngineWorkspace.TryApplyChanges(ideEngineWorkspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

            Dim ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single()
            Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(ideEngineWorkspace, ideEngineDocument, New TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length))
            For Each method In methodNames
                Assert.False(ideEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Event, False)))
                Assert.True(ideEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.NamedType, False)))
                Assert.True(ideEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Property, False)))
            Next method
        End Using

        Dim compilerEngineAnalyzer = New BasicTrackingDiagnosticAnalyzer()
        Using compilerEngineWorkspace = TestWorkspace.CreateVisualBasic(source.Value, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
            Dim compilerEngineCompilation = CType(compilerEngineWorkspace.CurrentSolution.Projects.Single().GetCompilationAsync().Result, VisualBasicCompilation)
            compilerEngineCompilation.GetAnalyzerDiagnostics({compilerEngineAnalyzer})
            For Each method In methodNames
                Assert.False(compilerEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Event, False)))
                Assert.True(compilerEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.NamedType, False)))
                Assert.True(compilerEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Property, False)))
            Next method
        End Using
    End Function

    <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/759")>
    Public Async Function DiagnosticAnalyzerDriverIsSafeAgainstAnalyzerExceptions() As Task
        Dim source = TestResource.AllInOneVisualBasicCode
        Await ThrowingDiagnosticAnalyzer(Of SyntaxKind).VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(
            Async Function(analyzer)
                Using workspace = TestWorkspace.CreateVisualBasic(source, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                    Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(analyzer))
                    workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                    Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                    Return Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, New TextSpan(0, document.GetTextAsync().Result.Length))
                End Using
            End Function)
    End Function

    <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908621")>
    Public Sub DiagnosticServiceIsSafeAgainstAnalyzerExceptions()
        Dim analyzer = New ThrowingDiagnosticAnalyzer(Of SyntaxKind)()
        analyzer.ThrowOn(GetType(DiagnosticAnalyzer).GetProperties().Single().Name)
        AccessSupportedDiagnostics(analyzer)
    End Sub

    Private Shared Sub AccessSupportedDiagnostics(analyzer As DiagnosticAnalyzer)
        Dim diagnosticService = New HostDiagnosticAnalyzers({New AnalyzerImageReference(ImmutableArray.Create(analyzer))})
        diagnosticService.GetDiagnosticDescriptorsPerReference(New DiagnosticAnalyzerInfoCache())
    End Sub

    <Fact>
    Public Async Function AnalyzerOptionsArePassedToAllAnalyzers() As Task
        Using workspace = TestWorkspace.CreateVisualBasic(TestResource.AllInOneVisualBasicCode, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
            Dim projectId = workspace.CurrentSolution.Projects.Single().Id

            Dim additionalDocId = DocumentId.CreateNewId(projectId)
            Dim additionalText = New TestAdditionalText("add.config", SourceText.From("random text"))
            Dim options = New AnalyzerOptions(ImmutableArray.Create(Of AdditionalText)(additionalText))
            Dim analyzer = New OptionsDiagnosticAnalyzer(Of SyntaxKind)(expectedOptions:=options)

            Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
            workspace.TryApplyChanges(workspace.CurrentSolution.
                WithAnalyzerReferences({analyzerReference}).
                AddAdditionalDocument(additionalDocId, "add.config", additionalText.GetText()))

            Dim sourceDocument = workspace.CurrentSolution.GetProject(projectId).Documents.Single()
            Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, sourceDocument, New TextSpan(0, sourceDocument.GetTextAsync().Result.Length))
            analyzer.VerifyAnalyzerOptions()
        End Using
    End Function

End Class
