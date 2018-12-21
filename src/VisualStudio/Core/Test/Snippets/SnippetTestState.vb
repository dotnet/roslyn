﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Moq
Imports MSXML

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend NotInheritable Class SnippetTestState
        Inherits AbstractCommandHandlerTestState

        Public Sub New(workspaceElement As XElement, languageName As String, startActiveSession As Boolean, extraParts As IEnumerable(Of Type), excludedTypes As IEnumerable(Of Type), Optional workspaceKind As String = Nothing)
            ' Remove the default completion presenters to prevent them from conflicting with the test one
            ' that we are adding.
            MyBase.New(workspaceElement,
                       extraParts:=CreatePartCatalog(extraParts),
                       workspaceKind:=workspaceKind,
                       excludedTypes:={GetType(IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession))}.Concat(If(excludedTypes, {})).ToList())

            Workspace.Options = Workspace.Options.WithChangedOption(InternalFeatureOnOffOptions.Snippets, True)

            SessionTestState = Workspace.GetService(Of IIntelliSenseTestState)

            Dim mockEditorAdaptersFactoryService = New Mock(Of IVsEditorAdaptersFactoryService)
            Dim mockSVsServiceProvider = New Mock(Of SVsServiceProvider)
            SnippetCommandHandler = If(languageName = LanguageNames.CSharp,
                DirectCast(New CSharp.Snippets.SnippetCommandHandler(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), mockEditorAdaptersFactoryService.Object, mockSVsServiceProvider.Object), AbstractSnippetCommandHandler),
                New VisualBasic.Snippets.SnippetCommandHandler(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), mockEditorAdaptersFactoryService.Object, mockSVsServiceProvider.Object))

            If languageName = LanguageNames.VisualBasic Then
                Dim snippetProvider As CompletionProvider = New VisualBasic.Snippets.SnippetCompletionProvider(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), Nothing)

                Dim completionService = DirectCast(Workspace.Services.GetLanguageServices(languageName).GetService(Of CompletionService), CommonCompletionService)
                completionService.SetTestProviders({snippetProvider})

                _completionCommandHandler = Workspace.GetService(Of CompletionCommandHandler)
            End If

            SnippetExpansionClient = New MockSnippetExpansionClient(Workspace.ExportProvider.GetExportedValue(Of IThreadingContext), startActiveSession)
            TextView.Properties.AddProperty(GetType(AbstractSnippetExpansionClient), SnippetExpansionClient)
        End Sub

        Public Sub New(workspaceElement As XElement, languageName As String, startActiveSession As Boolean, extraParts As IEnumerable(Of Type), Optional workspaceKind As String = Nothing)
            Me.New(workspaceElement, languageName, startActiveSession, extraParts, excludedTypes:=Enumerable.Empty(Of Type), workspaceKind)
        End Sub

        Public ReadOnly SnippetCommandHandler As AbstractSnippetCommandHandler
        Private ReadOnly _completionCommandHandler As CompletionCommandHandler
        Private _currentCompletionPresenterSession As TestCompletionPresenterSession
        Private ReadOnly SessionTestState As IIntelliSenseTestState

        Public Property SnippetExpansionClient As MockSnippetExpansionClient

        Private Shared Function CreatePartCatalog(types As IEnumerable(Of Type)) As ComposableCatalog
            Return ExportProviderCache.CreateTypeCatalog(types).WithParts(GetType(TestCompletionPresenter), GetType(IntelliSenseTestState))
        End Function

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        Friend ReadOnly Property CurrentCompletionPresenterSession As TestCompletionPresenterSession
            Get
                Return SessionTestState.CurrentCompletionPresenterSession
            End Get
        End Property

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

            Dim state = New SnippetTestState(workspaceXml, languageName, startActiveSession, extraParts, WorkspaceKind.Interactive)
            state.Workspace.Options = state.Workspace.Options.WithChangedOption(InternalFeatureOnOffOptions.Snippets, False)
            Return state
        End Function

        Friend Overloads Sub SendTabToCompletion()
            Dim handler = DirectCast(_completionCommandHandler, IChainedCommandHandler(Of TabKeyCommandArgs))

            SendTab(AddressOf handler.ExecuteCommand, AddressOf SendTab)
        End Sub

        Friend Overloads Sub SendTab()
            If Not SendTab(AddressOf SnippetCommandHandler.ExecuteCommand) Then
                EditorOperations.InsertText("    ")
            End If
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

        Public Overloads Sub SendTypeChars(typeChars As String)
            Dim handler = DirectCast(_completionCommandHandler, IChainedCommandHandler(Of TypeCharCommandArgs))
            MyBase.SendTypeChars(typeChars, AddressOf handler.ExecuteCommand)
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

            Private _startActiveSession As Boolean

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
