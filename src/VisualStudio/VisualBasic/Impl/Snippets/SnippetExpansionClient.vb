' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets.SnippetFunctions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager.Interop
Imports MSXML
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    Friend NotInheritable Class SnippetExpansionClient
        Inherits AbstractSnippetExpansionClient

        Public Sub New(languageServiceId As Guid, textView As ITextView, subjectBuffer As ITextBuffer, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService)
            MyBase.New(languageServiceId, textView, subjectBuffer, editorAdaptersFactoryService)
        End Sub

        Public Shared Function GetSnippetExpansionClient(textView As ITextView, subjectBuffer As ITextBuffer, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService) As AbstractSnippetExpansionClient
            Dim expansionClient As AbstractSnippetExpansionClient = Nothing

            If Not textView.Properties.TryGetProperty(GetType(AbstractSnippetExpansionClient), expansionClient) Then
                expansionClient = New SnippetExpansionClient(Guids.VisualBasicDebuggerLanguageId, textView, subjectBuffer, editorAdaptersFactoryService)
                textView.Properties.AddProperty(GetType(AbstractSnippetExpansionClient), expansionClient)
            End If

            Return expansionClient
        End Function

        Protected Overrides Function InsertEmptyCommentAndGetEndPositionTrackingSpan() As ITrackingSpan
            Dim endSpanInSurfaceBuffer(1) As VsTextSpan
            If ExpansionSession.GetEndSpan(endSpanInSurfaceBuffer) <> VSConstants.S_OK Then
                Return Nothing
            End If

            Dim endSpan As SnapshotSpan = Nothing
            If Not TryGetSubjectBufferSpan(endSpanInSurfaceBuffer(0), endSpan) Then
                Return Nothing
            End If

            Dim endPositionLine = SubjectBuffer.CurrentSnapshot.GetLineFromPosition(endSpan.Start.Position)
            Dim endLineText = endPositionLine.GetText()

            If endLineText.Trim() = String.Empty Then
                Dim commentString = "'"
                SubjectBuffer.Insert(endSpan.Start.Position, commentString)

                Dim commentSpan = New Span(endSpan.Start.Position, commentString.Length)
                Return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive)
            End If

            Return Nothing
        End Function

        Public Overrides Function GetExpansionFunction(xmlFunctionNode As IXMLDOMNode, bstrFieldName As String, ByRef pFunc As IVsExpansionFunction) As Integer
            Dim snippetFunctionName As String = Nothing
            Dim param As String = Nothing

            If Not TryGetSnippetFunctionInfo(xmlFunctionNode, snippetFunctionName, param) Then
                pFunc = Nothing
                Return VSConstants.E_INVALIDARG
            End If

            Select Case snippetFunctionName
                Case "SimpleTypeName"
                    pFunc = New SnippetFunctionSimpleTypeName(Me, TextView, SubjectBuffer, bstrFieldName, param)
                    Return VSConstants.S_OK
                Case "ClassName"
                    pFunc = New SnippetFunctionClassName(Me, TextView, SubjectBuffer, bstrFieldName)
                    Return VSConstants.S_OK
                Case "GenerateSwitchCases"
                    pFunc = New SnippetFunctionGenerateSwitchCases(Me, TextView, SubjectBuffer, bstrFieldName, param)
                    Return VSConstants.S_OK
                Case Else
                    pFunc = Nothing
                    Return VSConstants.E_INVALIDARG
            End Select
        End Function
    End Class
End Namespace