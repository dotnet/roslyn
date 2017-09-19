Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.CSharp.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Microsoft.VisualStudio.Utilities
Imports MSXML
Imports Roslyn.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    Public Class CompletionAndSnippetTests
        <WorkItem(15348, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=485413&_a=edit")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoNamesDuringExpansionSession() As Task

            Using state = TestState.CreateCSharpTestState(
                          <Document>
class Goo
{
    Goo G$$()
    {

    }
}
                              </Document>,
                          extraExportedTypes:={GetType(SnippetExpansionSessionIsActiveService)}.ToList())

                Dim client = New TestSnippetClient(New Guid(), Nothing, Nothing, Nothing)
                client.ExpansionSession = New MockExpansionSession()
                state.TextView.Properties.AddProperty(GetType(AbstractSnippetExpansionClient), client)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        Private Class TestSnippetClient
            Inherits AbstractSnippetExpansionClient

            Public Sub New(languageServiceGuid As Guid, textView As ITextView, subjectBuffer As ITextBuffer, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService)
                MyBase.New(languageServiceGuid, textView, subjectBuffer, editorAdaptersFactoryService)
            End Sub

            Public Overrides Function GetExpansionFunction(xmlFunctionNode As IXMLDOMNode, bstrFieldName As String, ByRef pFunc As IVsExpansionFunction) As Integer
                Throw New NotImplementedException()
            End Function

            Public Overrides Function TryHandleTab() As Boolean
                Return MyBase.TryHandleTab()
            End Function

            Public Overrides Function TryHandleBackTab() As Boolean
                Return MyBase.TryHandleBackTab()
            End Function

            Public Overrides Function TryHandleEscape() As Boolean
                Return MyBase.TryHandleEscape()
            End Function

            Public Overrides Function TryHandleReturn() As Boolean
                Return MyBase.TryHandleReturn()
            End Function

            Public Overrides Function TryInsertExpansion(startPositionInSubjectBuffer As Integer, endPositionInSubjectBuffer As Integer) As Boolean
                Return MyBase.TryInsertExpansion(startPositionInSubjectBuffer, endPositionInSubjectBuffer)
            End Function

            Protected Overrides Function InsertEmptyCommentAndGetEndPositionTrackingSpan() As ITrackingSpan
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function AddImports(document As Document, position As Integer, snippetNode As XElement, placeSystemNamespaceFirst As Boolean, cancellationToken As CancellationToken) As Document
                Throw New NotImplementedException()
            End Function
        End Class

        Private Class MockExpansionSession
            Implements IVsExpansionSession

            Public Function EndCurrentExpansion(fLeaveCaret As Integer) As Integer Implements IVsExpansionSession.EndCurrentExpansion
                Throw New NotImplementedException()
            End Function

            Public Function GoToNextExpansionField(fCommitIfLast As Integer) As Integer Implements IVsExpansionSession.GoToNextExpansionField
                Throw New NotImplementedException()
            End Function

            Public Function GoToPreviousExpansionField() As Integer Implements IVsExpansionSession.GoToPreviousExpansionField
                Throw New NotImplementedException()
            End Function

            Public Function GetFieldValue(bstrFieldName As String, ByRef pbstrValue As String) As Integer Implements IVsExpansionSession.GetFieldValue
                Throw New NotImplementedException()
            End Function

            Public Function SetFieldDefault(bstrFieldName As String, bstrNewValue As String) As Integer Implements IVsExpansionSession.SetFieldDefault
                Throw New NotImplementedException()
            End Function

            Public Function GetFieldSpan(bstrField As String, ptsSpan() As VisualStudio.TextManager.Interop.TextSpan) As Integer Implements IVsExpansionSession.GetFieldSpan
                Throw New NotImplementedException()
            End Function

            Public Function GetHeaderNode(bstrNode As String, ByRef pNode As IXMLDOMNode) As Integer Implements IVsExpansionSession.GetHeaderNode
                Throw New NotImplementedException()
            End Function

            Public Function GetDeclarationNode(bstrNode As String, ByRef pNode As IXMLDOMNode) As Integer Implements IVsExpansionSession.GetDeclarationNode
                Throw New NotImplementedException()
            End Function

            Public Function GetSnippetNode(bstrNode As String, ByRef pNode As IXMLDOMNode) As Integer Implements IVsExpansionSession.GetSnippetNode
                Throw New NotImplementedException()
            End Function

            Public Function GetSnippetSpan(pts() As VisualStudio.TextManager.Interop.TextSpan) As Integer Implements IVsExpansionSession.GetSnippetSpan
                Throw New NotImplementedException()
            End Function

            Public Function SetEndSpan(ts As VisualStudio.TextManager.Interop.TextSpan) As Integer Implements IVsExpansionSession.SetEndSpan
                Throw New NotImplementedException()
            End Function

            Public Function GetEndSpan(pts() As VisualStudio.TextManager.Interop.TextSpan) As Integer Implements IVsExpansionSession.GetEndSpan
                Throw New NotImplementedException()
            End Function
        End Class

        Private Function CreateCSharpSnippetExpansionNoteTestState(xElement As XElement, ParamArray snippetShortcuts As String()) As TestState
            Dim state = TestState.CreateCSharpTestState(
                xElement,
                New CompletionProvider() {New MockCompletionProvider()},
                Nothing,
                New List(Of Type) From {GetType(TestCSharpSnippetInfoService)})

            Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
            testSnippetInfoService.SetSnippetShortcuts(snippetShortcuts)
            Return state
        End Function
    End Class
End Namespace
