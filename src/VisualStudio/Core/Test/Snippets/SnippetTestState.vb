﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Moq
Imports MSXML

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend NotInheritable Class SnippetTestState
        Inherits TestState

        Private Sub New(workspaceElement As XElement, languageName As String, startActiveSession As Boolean, extraParts As IEnumerable(Of Type), excludedTypes As IEnumerable(Of Type), Optional workspaceKind As String = Nothing)
            ' Remove the default completion presenters to prevent them from conflicting with the test one
            ' that we are adding.
            MyBase.New(workspaceElement,
                       extraExportedTypes:={GetType(TestSignatureHelpPresenter), GetType(SnippetCompletionProvider), GetType(IntelliSenseTestState), GetType(MockCompletionPresenterProvider), GetType(StubVsEditorAdaptersFactoryService)}.Concat(If(extraParts, {})).ToList(),
                       workspaceKind:=workspaceKind,
                       excludedTypes:={GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)), GetType(FormatCommandHandler)}.Concat(If(excludedTypes, {})).ToList(),
                       includeFormatCommandHandler:=False)

            Workspace.TryApplyChanges(Workspace.CurrentSolution.WithOptions(Workspace.Options _
                    .WithChangedOption(InternalFeatureOnOffOptions.Snippets, True)))

            Dim mockSVsServiceProvider = New Mock(Of SVsServiceProvider)(MockBehavior.Strict)
            mockSVsServiceProvider.Setup(Function(s) s.GetService(GetType(SVsTextManager))).Returns(Nothing)

            SnippetCommandHandler = If(languageName = LanguageNames.CSharp,
                DirectCast(New CSharp.Snippets.SnippetCommandHandler(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), Workspace.ExportProvider.GetExportedValue(Of IVsEditorAdaptersFactoryService)(), mockSVsServiceProvider.Object), AbstractSnippetCommandHandler),
                New VisualBasic.Snippets.SnippetCommandHandler(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), Workspace.ExportProvider.GetExportedValue(Of IVsEditorAdaptersFactoryService)(), mockSVsServiceProvider.Object))

            SnippetExpansionClient = New MockSnippetExpansionClient(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), startActiveSession)
            TextView.Properties.AddProperty(GetType(AbstractSnippetExpansionClient), SnippetExpansionClient)
        End Sub

        Public ReadOnly SnippetCommandHandler As AbstractSnippetCommandHandler

        Public Property SnippetExpansionClient As MockSnippetExpansionClient

        Public Shared Function CreateTestState(markup As String, languageName As String, Optional startActiveSession As Boolean = False, Optional extraParts As IEnumerable(Of Type) = Nothing) As SnippetTestState
            extraParts = If(extraParts, Type.EmptyTypes)
            Dim workspaceXml = <Workspace>
                                   <Project Language=<%= languageName %> CommonReferences="true">
                                       <Document><%= markup %></Document>
                                   </Project>
                               </Workspace>

            Return New SnippetTestState(workspaceXml, languageName, startActiveSession, extraParts, excludedTypes:=New List(Of Type) From {GetType(CommitConnectionListener)})
        End Function

        Public Shared Function CreateSubmissionTestState(markup As String, languageName As String, Optional startActiveSession As Boolean = False, Optional extraParts As IEnumerable(Of Type) = Nothing) As SnippetTestState
            extraParts = If(extraParts, Type.EmptyTypes)
            Dim workspaceXml = <Workspace>
                                   <Submission Language=<%= languageName %> CommonReferences="true">
                                       <%= markup %>
                                   </Submission>
                               </Workspace>

            Dim state = New SnippetTestState(workspaceXml, languageName, startActiveSession, extraParts, excludedTypes:=Enumerable.Empty(Of Type), WorkspaceKind.Interactive)
            Dim workspace = state.Workspace
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                .WithChangedOption(InternalFeatureOnOffOptions.Snippets, False)))
            Return state
        End Function

        Friend Overloads Sub SendTabToCompletion()
            MyBase.SendTab()
        End Sub

        Friend Overloads Sub SendTab()
            If Not SendTab(AddressOf SnippetCommandHandler.ExecuteCommand) Then
                EditorOperations.InsertText("    ")
            End If
        End Sub

        Friend Overloads Sub SendBackSpace()
            EditorOperations.Backspace()
        End Sub

        Friend Overloads Sub SendBackTab()
            If Not SendBackTab(AddressOf SnippetCommandHandler.ExecuteCommand) Then
                EditorOperations.Unindent()
            End If
        End Sub

        Friend Overloads Sub SendReturn()
            If Not SendReturn(AddressOf SnippetCommandHandler.ExecuteCommand) Then
                EditorOperations.InsertNewLine()
            End If
        End Sub

        Friend Overloads Sub SendEscape()
            If Not SendEscape(AddressOf SnippetCommandHandler.ExecuteCommand) Then
                EditorOperations.InsertText("EscapePassedThrough!")
            End If
        End Sub

        Private Class MockOrderableContentTypeMetadata
            Inherits OrderableContentTypeMetadata

            Public Sub New(contentType As String)
                MyBase.New(New Dictionary(Of String, Object) From {{"ContentTypes", New List(Of String) From {contentType}},
                                                                  {"Name", String.Empty}})
            End Sub
        End Class

        Friend Class MockSnippetExpansionClient
            Inherits AbstractSnippetExpansionClient

            Public Sub New(threadingContext As IThreadingContext, startActiveSession As Boolean)
                MyBase.New(threadingContext, Nothing, Nothing, Nothing, Nothing)

                If startActiveSession Then
                    TryHandleTabReturnValue = True
                    TryHandleBackTabReturnValue = True
                    TryHandleEscapeReturnValue = True
                    TryHandleReturnReturnValue = True
                End If
            End Sub

            Public Property TryHandleReturnCalled As Boolean
            Public Property TryHandleReturnReturnValue As Boolean

            Public Property TryHandleTabCalled As Boolean
            Public Property TryHandleTabReturnValue As Boolean

            Public Property TryHandleBackTabCalled As Boolean
            Public Property TryHandleBackTabReturnValue As Boolean

            Public Property TryHandleEscapeCalled As Boolean
            Public Property TryHandleEscapeReturnValue As Boolean

            Public Property TryInsertExpansionCalled As Boolean
            Public Property TryInsertExpansionReturnValue As Boolean

            Public Property InsertExpansionSpan As Span

            Public Overrides Function TryHandleTab() As Boolean
                TryHandleTabCalled = True
                Return TryHandleTabReturnValue
            End Function

            Public Overrides Function TryHandleBackTab() As Boolean
                TryHandleBackTabCalled = True
                Return TryHandleBackTabReturnValue
            End Function

            Public Overrides Function TryHandleEscape() As Boolean
                TryHandleEscapeCalled = True
                Return TryHandleEscapeReturnValue
            End Function

            Public Overrides Function TryHandleReturn() As Boolean
                TryHandleReturnCalled = True
                Return TryHandleReturnReturnValue
            End Function

            Public Overrides Function TryInsertExpansion(startPosition As Integer, endPosition As Integer) As Boolean
                TryInsertExpansionCalled = True
                InsertExpansionSpan = New Span(startPosition, endPosition - startPosition)
                Return TryInsertExpansionReturnValue
            End Function

            Public Overrides Function GetExpansionFunction(xmlFunctionNode As IXMLDOMNode, bstrFieldName As String, ByRef pFunc As IVsExpansionFunction) As Integer
                Throw New NotImplementedException()
            End Function

            Protected Overrides Function InsertEmptyCommentAndGetEndPositionTrackingSpan() As ITrackingSpan
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function AddImports(document As Document, position As Integer, snippetNode As XElement, placeSystemNamespaceFirst As Boolean, cancellationToken As CancellationToken) As Document
                Return document
            End Function
        End Class
    End Class
End Namespace
