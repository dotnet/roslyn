' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Navigation
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    Public Class GoToDefinitionCancellationTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCancellation()
            ' Run without cancelling.
            Dim updates As Integer = Me.Cancel(Integer.MaxValue, False)
            Assert.InRange(updates, 0, Integer.MaxValue)
            Dim i As Integer = 0
            While i < updates
                Dim n As Integer = Me.Cancel(i, True)
                Assert.Equal(n, i + 1)
                i = i + 1
            End While
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestInLinkedFiles()
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
        <Document FilePath="C.cs">
class C
{
    void M()
    {
        M1$$(5);
    }
#if Proj1
    void M1(int x) { }
#endif
#if Proj2
    void M1(int x) { }
#endif
}
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
        <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
    </Project>
</Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(
                    definition,
                    exportProvider:=MinimalTestExportProvider.CreateExportProvider(GoToTestHelpers.Catalog.WithPart(GetType(CSharpGoToDefinitionService))))

                Dim baseDocument = workspace.Documents.First(Function(d) Not d.IsLinkFile)
                Dim linkDocument = workspace.Documents.First(Function(d) d.IsLinkFile)
                Dim view = baseDocument.GetTextView()

                Dim mockDocumentNavigationService =
                    DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)

                Dim commandHandler = New GoToDefinitionCommandHandler(New TestWaitIndicator(New TestWaitContext(100)))
                commandHandler.TryExecuteCommand(view.TextSnapshot, baseDocument.CursorPosition.Value)
                Assert.True(mockDocumentNavigationService._triedNavigationToSpan)
                Assert.Equal(New TextSpan(78, 2), mockDocumentNavigationService._span)

                workspace.SetDocumentContext(linkDocument.Id)

                commandHandler.TryExecuteCommand(view.TextSnapshot, baseDocument.CursorPosition.Value)
                Assert.True(mockDocumentNavigationService._triedNavigationToSpan)
                Assert.Equal(New TextSpan(121, 2), mockDocumentNavigationService._span)
            End Using
        End Sub

        Private Function Cancel(updatesBeforeCancel As Integer, expectedCancel As Boolean) As Integer
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|] { $$C c; }"
        </Document>
    </Project>
</Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(definition, exportProvider:=GoToTestHelpers.ExportProvider)
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim mockDocumentNavigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)

                Dim navigatedTo = False
                Dim presenter = New MockNavigableItemsPresenter(Sub(i) navigatedTo = True)
                Dim presenters = {New Lazy(Of INavigableItemsPresenter)(Function() presenter)}

                Dim cursorBuffer = cursorDocument.TextBuffer
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim goToDefService = New CSharpGoToDefinitionService(presenters)

                Dim waitContext = New TestWaitContext(updatesBeforeCancel)
                Dim waitIndicator = New TestWaitIndicator(waitContext)
                Dim commandHandler = New GoToDefinitionCommandHandler(waitIndicator)

                commandHandler.TryExecuteCommand(document, cursorPosition, goToDefService)

                Assert.Equal(navigatedTo OrElse mockDocumentNavigationService._triedNavigationToSpan, Not expectedCancel)

                Return waitContext.Updates
            End Using
        End Function
    End Class
End Namespace
