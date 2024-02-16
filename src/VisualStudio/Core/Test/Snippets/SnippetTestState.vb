' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.LanguageServices.Snippets
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Friend NotInheritable Class SnippetTestState
        Inherits TestState

        Private Sub New(workspaceElement As XElement, languageName As String, startActiveSession As Boolean, extraParts As IEnumerable(Of Type), excludedTypes As IEnumerable(Of Type), Optional workspaceKind As String = Nothing)
            ' Remove the default completion presenters to prevent them from conflicting with the test one
            ' that we are adding.
            MyBase.New(workspaceElement,
                       extraExportedTypes:=AugmentExtraTypesForSnippetTests(extraParts),
                       workspaceKind:=workspaceKind,
                       excludedTypes:=AugmentExcludedTypesForSnippetTests(excludedTypes),
                       includeFormatCommandHandler:=False)

            Workspace.GlobalOptions.SetGlobalOption(SnippetsOptionsStorage.Snippets, True)

            Dim contentType = If(languageName = LanguageNames.CSharp, ContentTypeNames.CSharpContentType, ContentTypeNames.VisualBasicContentType)
            Dim name = If(languageName = LanguageNames.CSharp, "CSharp Snippets", "VB Snippets")
            Dim snippetCommandHandler = Workspace.GetService(Of ICommandHandler)(contentType, name)

            Me.SnippetCommandHandler = DirectCast(snippetCommandHandler, AbstractSnippetCommandHandler)

            Dim editorOptionsService = Workspace.GetService(Of EditorOptionsService)()
            Dim snippetExpansionClientFactory = Workspace.Services.GetRequiredService(Of ISnippetExpansionClientFactory)()
            SnippetExpansionClient = CType(snippetExpansionClientFactory.GetOrCreateSnippetExpansionClient(SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext(), TextView, SubjectBuffer), MockSnippetExpansionClient)

            If startActiveSession Then
                SnippetExpansionClient.TryHandleTabReturnValue = True
                SnippetExpansionClient.TryHandleBackTabReturnValue = True
                SnippetExpansionClient.TryHandleEscapeReturnValue = True
                SnippetExpansionClient.TryHandleReturnReturnValue = True
            End If
        End Sub

        Private Shared Function AugmentExtraTypesForSnippetTests(extraParts As IEnumerable(Of Type)) As IEnumerable(Of Type)
            Return If(extraParts, Type.EmptyTypes).Concat(
                {
                    GetType(TestSignatureHelpPresenter),
                    GetType(IntelliSenseTestState),
                    GetType(MockCompletionPresenterProvider),
                    GetType(StubVsEditorAdaptersFactoryService),
                    GetType(CSharp.Snippets.SnippetCommandHandler),
                    GetType(VisualBasic.Snippets.SnippetCommandHandler),
                    GetType(MockCSharpSnippetLanguageHelper),
                    GetType(MockVisualBasicSnippetLanguageHelper),
                    GetType(MockSnippetExpansionClientFactory),
                    GetType(MockServiceProvider),
                    GetType(StubVsServiceExporter(Of )),
                    GetType(StubVsServiceExporter(Of ,))
                })
        End Function

        Private Shared Function AugmentExcludedTypesForSnippetTests(excludedTypes As IEnumerable(Of Type)) As IEnumerable(Of Type)
            Return If(excludedTypes, Type.EmptyTypes).Concat(
                {
                    GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)),
                    GetType(FormatCommandHandler)
                })
        End Function

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
            state.Workspace.GlobalOptions.SetGlobalOption(SnippetsOptionsStorage.Snippets, False)
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

        <ExportWorkspaceService(GetType(ISnippetExpansionClientFactory), ServiceLayer.Test)>
        <[Shared]>
        <PartNotDiscoverable>
        Friend Class MockSnippetExpansionClientFactory
            Inherits SnippetExpansionClientFactory

            Private ReadOnly _threadingContext As IThreadingContext
            Private ReadOnly _signatureHelpControllerProvider As SignatureHelpControllerProvider
            Private ReadOnly _editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory
            Private ReadOnly _editorAdaptersFactoryService As IVsEditorAdaptersFactoryService
            Private ReadOnly _argumentProviders As ImmutableArray(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata))
            Private ReadOnly _editorOptionsService As EditorOptionsService

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New(
                threadingContext As IThreadingContext,
                signatureHelpControllerProvider As SignatureHelpControllerProvider,
                editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory,
                editorAdaptersFactoryService As IVsEditorAdaptersFactoryService,
                <ImportMany> argumentProviders As IEnumerable(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata)),
                editorOptionsService As EditorOptionsService)
                MyBase.New(
                    threadingContext,
                    signatureHelpControllerProvider,
                    editorCommandHandlerServiceFactory,
                    editorAdaptersFactoryService,
                    argumentProviders.ToImmutableArray(),
                    editorOptionsService)

                _threadingContext = threadingContext
                _signatureHelpControllerProvider = signatureHelpControllerProvider
                _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory
                _editorAdaptersFactoryService = editorAdaptersFactoryService
                _argumentProviders = argumentProviders.ToImmutableArray()
                _editorOptionsService = editorOptionsService
            End Sub

            Protected Overrides Function CreateSnippetExpansionClient(document As Document, textView As ITextView, subjectBuffer As ITextBuffer) As SnippetExpansionClient
                Return New MockSnippetExpansionClient(
                    _threadingContext,
                    document.GetRequiredLanguageService(Of ISnippetExpansionLanguageHelper)(),
                    textView,
                    subjectBuffer,
                    _signatureHelpControllerProvider,
                    _editorCommandHandlerServiceFactory,
                    _editorAdaptersFactoryService,
                    _argumentProviders,
                    _editorOptionsService)
            End Function
        End Class

        <ExportLanguageService(GetType(ISnippetExpansionLanguageHelper), LanguageNames.CSharp, ServiceLayer.Test)>
        <[Shared]>
        <PartNotDiscoverable>
        Friend NotInheritable Class MockCSharpSnippetLanguageHelper
            Inherits MockSnippetLanguageHelper

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(Guids.CSharpLanguageServiceId)
            End Sub
        End Class

        <ExportLanguageService(GetType(ISnippetExpansionLanguageHelper), LanguageNames.VisualBasic, ServiceLayer.Test)>
        <[Shared]>
        <PartNotDiscoverable>
        Friend NotInheritable Class MockVisualBasicSnippetLanguageHelper
            Inherits MockSnippetLanguageHelper

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(Guids.VisualBasicDebuggerLanguageId)
            End Sub
        End Class

        Friend MustInherit Class MockSnippetLanguageHelper
            Inherits AbstractSnippetExpansionLanguageHelper

            Protected Sub New(languageServiceGuid As Guid)
                Me.LanguageServiceGuid = languageServiceGuid
            End Sub

            Public Overrides ReadOnly Property LanguageServiceGuid As Guid

            Public Overrides ReadOnly Property FallbackDefaultLiteral As String
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Overrides Function AddImports(document As Document, addImportOptions As AddImportPlacementOptions, formattingOptions As SyntaxFormattingOptions, position As Integer, snippetNode As XElement, cancellationToken As CancellationToken) As Document
                Return document
            End Function

            Public Overrides Function InsertEmptyCommentAndGetEndPositionTrackingSpan(expansionSession As IVsExpansionSession, textView As ITextView, subjectBuffer As ITextBuffer) As ITrackingSpan
                Throw New NotImplementedException()
            End Function
        End Class

        Friend Class MockSnippetExpansionClient
            Inherits SnippetExpansionClient

            Public Sub New(threadingContext As IThreadingContext,
                           languageHelper As ISnippetExpansionLanguageHelper,
                           textView As ITextView,
                           subjectBuffer As ITextBuffer,
                           signatureHelpControllerProvider As SignatureHelpControllerProvider,
                           editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory,
                           editorAdaptersFactoryService As IVsEditorAdaptersFactoryService,
                           argumentProviders As ImmutableArray(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata)),
                           editorOptionsService As EditorOptionsService)
                MyBase.New(threadingContext,
                           languageHelper,
                           textView,
                           subjectBuffer,
                           signatureHelpControllerProvider,
                           editorCommandHandlerServiceFactory,
                           editorAdaptersFactoryService,
                           argumentProviders,
                           editorOptionsService)
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

            Public Overrides Function TryInsertExpansion(startPosition As Integer, endPosition As Integer, cancellationToken As CancellationToken) As Boolean
                TryInsertExpansionCalled = True
                InsertExpansionSpan = New Span(startPosition, endPosition - startPosition)
                Return TryInsertExpansionReturnValue
            End Function
        End Class
    End Class
End Namespace
