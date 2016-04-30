' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.BraceCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Moq
Imports MSXML

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend NotInheritable Class SnippetTestState
        Inherits AbstractCommandHandlerTestState
        Implements IIntelliSenseTestState

        Public Sub New(workspaceElement As XElement, languageName As String, startActiveSession As Boolean, extraParts As IEnumerable(Of Type), Optional workspaceKind As String = Nothing)
            MyBase.New(workspaceElement, extraParts:=CreatePartCatalog(extraParts), workspaceKind:=workspaceKind)

            Dim optionService = Workspace.Services.GetService(Of IOptionService)()
            optionService.SetOptions(optionService.GetOptions().WithChangedOption(InternalFeatureOnOffOptions.Snippets, True))
            Dim mockEditorAdaptersFactoryService = New Mock(Of IVsEditorAdaptersFactoryService)
            Dim mockSVsServiceProvider = New Mock(Of SVsServiceProvider)
            SnippetCommandHandler = If(languageName = LanguageNames.CSharp,
                DirectCast(New CSharp.Snippets.SnippetCommandHandler(mockEditorAdaptersFactoryService.Object, mockSVsServiceProvider.Object), AbstractSnippetCommandHandler),
                New VisualBasic.Snippets.SnippetCommandHandler(mockEditorAdaptersFactoryService.Object, mockSVsServiceProvider.Object))

            If languageName = LanguageNames.VisualBasic Then
                Dim snippetProvider As CompletionListProvider = New VisualBasic.Snippets.SnippetCompletionProvider(Nothing)

                Dim asyncCompletionService = New AsyncCompletionService(
                    GetService(Of IEditorOperationsFactoryService)(),
                    UndoHistoryRegistry,
                    GetService(Of IInlineRenameService)(),
                    New TestCompletionPresenter(Me),
                    GetExports(Of IAsynchronousOperationListener, FeatureMetadata)(),
                    CreateLazyProviders({snippetProvider}, languageName, roles:=Nothing),
                    GetExports(Of IBraceCompletionSessionProvider, BraceCompletionMetadata)())

                Dim CompletionCommandHandler = New CompletionCommandHandler(asyncCompletionService)

                Me._completionCommandHandler = CompletionCommandHandler
            End If

            SnippetExpansionClient = New MockSnippetExpansionClient(startActiveSession)
            TextView.Properties.AddProperty(GetType(AbstractSnippetExpansionClient), SnippetExpansionClient)
        End Sub

        Public ReadOnly SnippetCommandHandler As AbstractSnippetCommandHandler
        Private ReadOnly _completionCommandHandler As CompletionCommandHandler
        Private _currentCompletionPresenterSession As TestCompletionPresenterSession
        Public Property SnippetExpansionClient As MockSnippetExpansionClient

        Private Shared Function CreatePartCatalog(types As IEnumerable(Of Type)) As ComposableCatalog
            Dim extraParts = types.Concat({GetType(SignatureHelpWaiter), GetType(CompletionWaiter)})
            Return MinimalTestExportProvider.CreateTypeCatalog(extraParts)
        End Function

        Public Property CurrentCompletionPresenterSession As TestCompletionPresenterSession Implements IIntelliSenseTestState.CurrentCompletionPresenterSession
            Get
                Return _currentCompletionPresenterSession
            End Get
            Set(value As TestCompletionPresenterSession)
                _currentCompletionPresenterSession = value
            End Set
        End Property

        Public Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession Implements IIntelliSenseTestState.CurrentSignatureHelpPresenterSession
            Get
                Throw New NotImplementedException()
            End Get
            Set(value As TestSignatureHelpPresenterSession)
                Throw New NotImplementedException()
            End Set
        End Property

        Public Shared Function CreateTestState(markup As String, languageName As String, Optional startActiveSession As Boolean = False, Optional extraParts As IEnumerable(Of Type) = Nothing) As SnippetTestState
            extraParts = If(extraParts, Type.EmptyTypes)
            Dim workspaceXml = <Workspace>
                                   <Project Language=<%= languageName %> CommonReferences="true">
                                       <Document><%= markup %></Document>
                                   </Project>
                               </Workspace>

            Return New SnippetTestState(workspaceXml, languageName, startActiveSession, extraParts)
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
            Dim handler = DirectCast(_completionCommandHandler, ICommandHandler(Of TabKeyCommandArgs))

            SendTab(AddressOf handler.ExecuteCommand, AddressOf SendTab)
        End Sub

        Friend Overloads Sub SendTab()
            SendTab(AddressOf SnippetCommandHandler.ExecuteCommand, Function() EditorOperations.InsertText("    "))
        End Sub

        Friend Overloads Sub SendBackTab()
            SendBackTab(AddressOf SnippetCommandHandler.ExecuteCommand, Function() EditorOperations.Unindent())
        End Sub

        Friend Overloads Sub SendReturn()
            SendReturn(AddressOf SnippetCommandHandler.ExecuteCommand, Function() EditorOperations.InsertNewLine())
        End Sub

        Friend Overloads Sub SendEscape()
            SendEscape(AddressOf SnippetCommandHandler.ExecuteCommand, Function() EditorOperations.InsertText("EscapePassedThrough!"))
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

            Public Sub New(startActiveSession As Boolean)
                MyBase.New(Nothing, Nothing, Nothing, Nothing)

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

            Friend Overrides Function AddImports(document As Document, snippetNode As XElement, placeSystemNamespaceFirst As Boolean, cancellationToken As CancellationToken) As Document
                Return document
            End Function
        End Class
    End Class
End Namespace
