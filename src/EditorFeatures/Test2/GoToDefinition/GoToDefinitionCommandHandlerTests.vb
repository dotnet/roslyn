' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.GoToDefinition
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    <[UseExportProvider]>
    Public Class GoToDefinitionCancellationTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCancellation()
            ' Run without cancelling.
            Dim updates = Cancel(Integer.MaxValue, False)
            Assert.InRange(updates, 0, Integer.MaxValue)
            Dim i As Integer = 0
            While i < updates
                Dim n = Cancel(i, True)
                Assert.Equal(n, i + 1)
                i += 1
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

            Using workspace = TestWorkspace.Create(definition, composition:=GoToTestHelpers.Composition)

                Dim baseDocument = workspace.Documents.First(Function(d) Not d.IsLinkFile)
                Dim linkDocument = workspace.Documents.First(Function(d) d.IsLinkFile)
                Dim view = baseDocument.GetTextView()

                Dim mockDocumentNavigationService =
                    DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)

                Dim handler = New GoToDefinitionCommandHandler()
                handler.ExecuteCommand(New GoToDefinitionCommandArgs(view, baseDocument.GetTextBuffer()), TestCommandExecutionContext.Create())
                Assert.True(mockDocumentNavigationService._triedNavigationToSpan)
                Assert.Equal(New TextSpan(78, 2), mockDocumentNavigationService._span)

                workspace.SetDocumentContext(linkDocument.Id)

                handler.ExecuteCommand(New GoToDefinitionCommandArgs(view, baseDocument.GetTextBuffer()), TestCommandExecutionContext.Create())
                Assert.True(mockDocumentNavigationService._triedNavigationToSpan)
                Assert.Equal(New TextSpan(121, 2), mockDocumentNavigationService._span)
            End Using
        End Sub

        Private Shared Function Cancel(updatesBeforeCancel As Integer, expectedCancel As Boolean) As Integer
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|] { $$C c; }"
        </Document>
    </Project>
</Workspace>

            Using workspace = TestWorkspace.Create(definition, composition:=GoToTestHelpers.Composition)
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim mockDocumentNavigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)

                Dim navigatedTo = False
                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
                Dim presenter = New MockStreamingFindUsagesPresenter(workspace.GlobalOptions, Sub() navigatedTo = True)

                Dim cursorBuffer = cursorDocument.GetTextBuffer()
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim goToDefService = New CSharpGoToDefinitionService(threadingContext, presenter)

                Dim waitContext = New TestUIThreadOperationContext(updatesBeforeCancel)
                GoToDefinitionCommandHandler.TestAccessor.ExecuteCommand(document, cursorPosition, goToDefService, New CommandExecutionContext(waitContext))

                Assert.Equal(navigatedTo OrElse mockDocumentNavigationService._triedNavigationToSpan, Not expectedCancel)

                Return waitContext.Updates
            End Using
        End Function
    End Class
End Namespace
