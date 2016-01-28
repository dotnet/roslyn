Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.CodeAnalysis.UnitTests.Diagnostics
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
    Partial Public MustInherit Class AbstractCrossLanguageUserDiagnosticTest
        Protected Const DestinationDocument = "DestinationDocument"

        Friend MustOverride Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)

        Protected Async Function TestMissing(definition As XElement) As Task
            Using workspace = Await TestWorkspace.CreateAsync(definition)
                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Assert.Null(diagnosticAndFix)
            End Using
        End Function

        Protected Async Function TestAsync(definition As XElement,
                            Optional expected As String = Nothing,
                            Optional codeActionIndex As Integer = 0,
                            Optional verifyTokens As Boolean = True,
                            Optional fileNameToExpected As Dictionary(Of String, String) = Nothing,
                            Optional verifySolutions As Action(Of Solution, Solution) = Nothing,
                            Optional onAfterWorkspaceCreated As Action(Of TestWorkspace) = Nothing) As Task
            Using workspace = TestWorkspace.CreateWorkspace(definition)
                onAfterWorkspaceCreated?.Invoke(workspace)

                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim codeAction = diagnosticAndFix.Item2.Fixes.ElementAt(codeActionIndex).Action
                Dim operations = Await codeAction.GetOperationsAsync(CancellationToken.None)
                Dim edit = operations.OfType(Of ApplyChangesOperation)().First()

                Dim oldSolution = workspace.CurrentSolution
                Dim updatedSolution = edit.ChangedSolution

                verifySolutions?.Invoke(oldSolution, updatedSolution)

                If fileNameToExpected Is Nothing Then
                    Dim updatedDocument = SolutionUtilities.GetSingleChangedDocument(oldSolution, updatedSolution)

                    Await VerifyAsync(expected, verifyTokens, updatedDocument)
                Else
                    For Each kvp In fileNameToExpected
                        Dim updatedDocument = updatedSolution.Projects.SelectMany(Function(p) p.Documents).Single(Function(d) d.Name = kvp.Key)
                        Await VerifyAsync(kvp.Value, verifyTokens, updatedDocument)
                    Next
                End If
            End Using
        End Function

        Private Shared Async Function VerifyAsync(expected As String, verifyTokens As Boolean, updatedDocument As Document) As Task
            Dim actual = (Await updatedDocument.GetTextAsync()).ToString().Trim()

            If verifyTokens Then
                Utilities.AssertEx.TokensAreEqual(expected, actual, updatedDocument.Project.Language)
            Else
                AssertEx.Equal(expected, actual)
            End If
        End Function

        Friend Async Function GetDiagnosticAndFixAsync(workspace As TestWorkspace) As Task(Of Tuple(Of Diagnostic, CodeFixCollection))
            Return (Await GetDiagnosticAndFixesAsync(workspace)).FirstOrDefault()
        End Function

        Private Function GetHostDocument(workspace As TestWorkspace) As TestHostDocument
            Dim hostDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)

            Return hostDocument
        End Function

        Private Async Function GetDiagnosticAndFixesAsync(workspace As TestWorkspace) As Task(Of IEnumerable(Of Tuple(Of Diagnostic, CodeFixCollection)))
            Dim hostDocument = GetHostDocument(workspace)
            Dim providerAndFixer = CreateDiagnosticProviderAndFixer(workspace, hostDocument.Project.Language)
            Dim fixer = providerAndFixer.Item2

            Dim result = New List(Of Tuple(Of Diagnostic, CodeFixCollection))
            Dim docAndDiagnostics = Await GetDocumentAndDiagnosticsAsync(workspace, providerAndFixer.Item1)
            Dim _document = docAndDiagnostics.Item1

            Dim ids = New HashSet(Of String)(fixer.FixableDiagnosticIds)
            Dim diagnostics = docAndDiagnostics.Item2.Where(Function(d) ids.Contains(d.Id)).ToList()
            Dim tree = Await _document.GetSyntaxTreeAsync()

            For Each diagnostic In diagnostics
                Dim fixes = New List(Of CodeFix)
                Dim context = New CodeFixContext(_document, diagnostic, Sub(a, d) fixes.Add(New CodeFix(_document.Project, a, d)), CancellationToken.None)
                providerAndFixer.Item2.RegisterCodeFixesAsync(context).Wait()
                If fixes.Any() Then
                    result.Add(Tuple.Create(diagnostic, New CodeFixCollection(fixer, diagnostic.Location.SourceSpan, fixes)))
                End If
            Next

            Return result
        End Function

        Private Async Function GetDocumentAndDiagnosticsAsync(workspace As TestWorkspace, provider As DiagnosticAnalyzer) As Task(Of Tuple(Of Document, IEnumerable(Of Diagnostic)))
            Dim hostDocument = GetHostDocument(workspace)

            Dim invocationBuffer = hostDocument.TextBuffer
            Dim invocationPoint = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

            Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)

            Dim syntaxFacts = document.Project.LanguageServices.GetService(Of ISyntaxFactsService)()
            Dim root = Await document.GetSyntaxRootAsync()
            Dim start = syntaxFacts.GetContainingMemberDeclaration(root, invocationPoint)

            Dim result = Await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(provider, document, start.FullSpan)

            '' currently, we don't test compilation level user diagnostic
            Return Tuple.Create(document, result.Where(Function(d) d.Location.SourceSpan.IntersectsWith(invocationPoint)))
        End Function


        Protected Async Function TestAddProjectReferenceAsync(xmlDefinition As XElement,
                                              expectedProjectReferenceFrom As String,
                                              expectedProjectReferenceTo As String,
                                              Optional index As Integer = 0) As Task

            Using workspace = Await TestWorkspace.CreateAsync(xmlDefinition)
                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim codeAction = diagnosticAndFix.Item2.Fixes.ElementAt(index).Action
                Dim operations = Await codeAction.GetOperationsAsync(CancellationToken.None)
                Dim edit = operations.OfType(Of ApplyChangesOperation)().First()
                Dim addedProjectReference = SolutionUtilities.GetSingleAddedProjectReference(workspace.CurrentSolution, edit.ChangedSolution)

                Assert.Equal(expectedProjectReferenceFrom, addedProjectReference.Item1.Name)

                Dim projectTo = edit.ChangedSolution.GetProject(addedProjectReference.Item2.ProjectId)
                Assert.Equal(expectedProjectReferenceTo, projectTo.Name)
            End Using
        End Function

        Protected Async Function TestAddUnresolvedMetadataReferenceAsync(xmlDefinition As XElement,
                                                         expectedProjectToReceiveReference As String,
                                                         expectedAssemblyIdentity As String,
                                                         Optional index As Integer = 0) As Task

            Using workspace = Await TestWorkspace.CreateAsync(xmlDefinition)
                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim codeAction = diagnosticAndFix.Item2.Fixes.ElementAt(index).Action
                Dim operations = Await codeAction.GetOperationsAsync(CancellationToken.None)

                Dim edit = operations.OfType(Of ApplyChangesOperation)().FirstOrDefault()
                Assert.Equal(Nothing, edit)

                Dim postOp = operations.OfType(Of TestAddMetadataReferenceCodeActionOperationFactoryWorkspaceService.Operation).FirstOrDefault()
                Assert.NotEqual(Nothing, postOp)
                Assert.Equal(expectedAssemblyIdentity, postOp.AssemblyIdentity.GetDisplayName())
                Assert.Equal(expectedProjectToReceiveReference, workspace.CurrentSolution.GetProject(postOp.ProjectId).Name)
            End Using
        End Function

        Protected Overridable Function GetNode(doc As Document, position As Integer) As SyntaxNode
            Return doc.GetSyntaxRootAsync().Result.FindToken(position).Parent
        End Function
    End Class
End Namespace
