' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics

Public Class DiagnosticAnalyzerDriverTests
    <Fact>
    Public Async Function DiagnosticAnalyzerDriverAllInOne() As Task
        Dim source = TestResource.AllInOneVisualBasicCode
        Dim analyzer = New BasicTrackingDiagnosticAnalyzer()
        Using workspace = Await TestWorkspace.CreateVisualBasicAsync(source)
            Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
            AccessSupportedDiagnostics(analyzer)
            Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(analyzer, document, New TextSpan(0, document.GetTextAsync().Result.Length))
            analyzer.VerifyAllAnalyzerMembersWereCalled()
            analyzer.VerifyAnalyzeSymbolCalledForAllSymbolKinds()
            analyzer.VerifyAnalyzeNodeCalledForAllSyntaxKinds()
            analyzer.VerifyOnCodeBlockCalledForAllSymbolAndMethodKinds(allowUnexpectedCalls:=True)
        End Using
    End Function

    <Fact, WorkItem(908658)>
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
        Using ideEngineWorkspace = Await TestWorkspace.CreateVisualBasicAsync(source.Value)
            Dim ideEngineDocument = ideEngineWorkspace.CurrentSolution.Projects.Single().Documents.Single()
            Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(ideEngineAnalyzer, ideEngineDocument, New TextSpan(0, ideEngineDocument.GetTextAsync().Result.Length))
            For Each method In methodNames
                Assert.False(ideEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Event, False)))
                Assert.False(ideEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.NamedType, False)))
                Assert.True(ideEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Property, False)))
            Next method
        End Using

        Dim compilerEngineAnalyzer = New BasicTrackingDiagnosticAnalyzer()
        Using compilerEngineWorkspace = Await TestWorkspace.CreateVisualBasicAsync(source.Value)
            Dim compilerEngineCompilation = CType(compilerEngineWorkspace.CurrentSolution.Projects.Single().GetCompilationAsync().Result, VisualBasicCompilation)
            compilerEngineCompilation.GetAnalyzerDiagnostics({compilerEngineAnalyzer})
            For Each method In methodNames
                Assert.False(compilerEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Event, False)))
                Assert.False(compilerEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.NamedType, False)))
                Assert.True(compilerEngineAnalyzer.CallLog.Any(Function(e) e.CallerName = method AndAlso If(e.SymbolKind = SymbolKind.Property, False)))
            Next method
        End Using
    End Function

    <Fact>
    <WorkItem(759)>
    Public Async Function DiagnosticAnalyzerDriverIsSafeAgainstAnalyzerExceptions() As Task
        Dim source = TestResource.AllInOneVisualBasicCode
        Using Workspace = Await TestWorkspace.CreateVisualBasicAsync(source)
            Dim document = Workspace.CurrentSolution.Projects.Single().Documents.Single()
            Await ThrowingDiagnosticAnalyzer(Of SyntaxKind).VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(
                Async Function(analyzer) Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(analyzer, document, New TextSpan(0, document.GetTextAsync().Result.Length), logAnalyzerExceptionAsDiagnostics:=True))
        End Using
    End Function

    <WorkItem(908621)>
    <Fact>
    Public Sub DiagnosticServiceIsSafeAgainstAnalyzerExceptions()
        Dim analyzer = New ThrowingDiagnosticAnalyzer(Of SyntaxKind)()
        analyzer.ThrowOn(GetType(DiagnosticAnalyzer).GetProperties().Single().Name)
        AccessSupportedDiagnostics(analyzer)
    End Sub

    Private Sub AccessSupportedDiagnostics(analyzer As DiagnosticAnalyzer)
        Dim diagnosticService = New TestDiagnosticAnalyzerService(LanguageNames.VisualBasic, analyzer)
        diagnosticService.GetDiagnosticDescriptors(projectOpt:=Nothing)
    End Sub

    <Fact>
    Public Async Function AnalyzerOptionsArePassedToAllAnalyzers() As Task
        Using workspace = Await TestWorkspace.CreateVisualBasicAsync(TestResource.AllInOneVisualBasicCode)
            Dim currentProject = workspace.CurrentSolution.Projects.Single()
            Dim additionalDocId = DocumentId.CreateNewId(currentProject.Id)
            Dim newSln = workspace.CurrentSolution.AddAdditionalDocument(additionalDocId, "add.config", SourceText.From("random text"))

            currentProject = newSln.Projects.Single()
            Dim additionalDocument = currentProject.GetAdditionalDocument(additionalDocId)

            Dim additionalStream As AdditionalText = New AdditionalTextDocument(additionalDocument.GetDocumentState())
            Dim options = New AnalyzerOptions(ImmutableArray.Create(additionalStream))
            Dim analyzer = New OptionsDiagnosticAnalyzer(Of SyntaxKind)(expectedOptions:=options)

            Dim sourceDocument = currentProject.Documents.Single()
            Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(analyzer, sourceDocument, New Text.TextSpan(0, sourceDocument.GetTextAsync().Result.Length))
            analyzer.VerifyAnalyzerOptions()
        End Using
    End Function

End Class
