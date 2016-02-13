' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.BraceCompletion
Imports Microsoft.VisualStudio.Text.BraceCompletion.Implementation
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.TextManager

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    Friend Class TestState
        Inherits AbstractCommandHandlerTestState
        Implements IIntelliSenseTestState

        Friend ReadOnly AsyncCompletionService As IAsyncCompletionService
        Friend ReadOnly SignatureHelpCommandHandler As SignatureHelpCommandHandler
        Friend ReadOnly CompletionCommandHandler As CompletionCommandHandler
        Friend ReadOnly IntelliSenseCommandHandler As IntelliSenseCommandHandler

        Private _context As AbstractDebuggerIntelliSenseContext

        Friend Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession Implements IIntelliSenseTestState.CurrentSignatureHelpPresenterSession
        Friend Property CurrentCompletionPresenterSession As TestCompletionPresenterSession Implements IIntelliSenseTestState.CurrentCompletionPresenterSession

        Private Sub New(workspaceElement As XElement,
                        extraCompletionProviders As IEnumerable(Of Lazy(Of CompletionListProvider, OrderableLanguageAndRoleMetadata)),
                        extraSignatureHelpProviders As IEnumerable(Of Lazy(Of ISignatureHelpProvider, OrderableLanguageMetadata)),
                        isImmediateWindow As Boolean)

            MyBase.New(
                workspaceElement,
                exportProvider:=MinimalTestExportProvider.CreateExportProvider(VisualStudioTestExportProvider.PartCatalog.WithParts(
                            GetType(CompletionWaiter),
                            GetType(SignatureHelpWaiter))),
                workspaceKind:=WorkspaceKind.Debugger)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            Dim completionProviders = GetExports(Of CompletionListProvider, OrderableLanguageAndRoleMetadata)() _
                .Where(Function(f) f.Metadata.Language = language) _
                .Concat(extraCompletionProviders) _
                .ToList()

            Me.AsyncCompletionService = New AsyncCompletionService(
                GetService(Of IEditorOperationsFactoryService)(),
                UndoHistoryRegistry,
                GetService(Of IInlineRenameService)(),
                New TestCompletionPresenter(Me),
                GetExports(Of IAsynchronousOperationListener, FeatureMetadata)(),
                completionProviders,
                GetExports(Of IBraceCompletionSessionProvider, IBraceCompletionMetadata)())

            Me.CompletionCommandHandler = New CompletionCommandHandler(Me.AsyncCompletionService)

            Me.SignatureHelpCommandHandler = New SignatureHelpCommandHandler(
                GetService(Of IInlineRenameService)(),
                New TestSignatureHelpPresenter(Me),
                GetExports(Of ISignatureHelpProvider, OrderableLanguageMetadata)().Concat(extraSignatureHelpProviders),
                GetExports(Of IAsynchronousOperationListener, FeatureMetadata)())

            Me.IntelliSenseCommandHandler = New IntelliSenseCommandHandler(CompletionCommandHandler, SignatureHelpCommandHandler, Nothing)

            languageServices.GetService(Of ICompletionService).ClearMRUCache()

            Dim spanDocument = Workspace.Documents.First(Function(x) x.SelectedSpans.Any())
            Dim statementSpan = spanDocument.SelectedSpans.First()
            Dim span = New Interop.TextSpan() {statementSpan.ToSnapshotSpan(spanDocument.GetTextBuffer().CurrentSnapshot).ToVsTextSpan()}

            Dim componentModel = New MockComponentModel(Workspace.ExportProvider)

            If language = LanguageNames.CSharp Then
                _context = New CSharpDebuggerIntelliSenseContext(
                    Workspace.Projects.First().Documents.First().GetTextView(),
                    Workspace.Projects.First().Documents.Last().GetTextBuffer(),
                    span,
                    componentModel,
                    isImmediateWindow)
            Else
                ' VB
                _context = New VisualBasicDebuggerIntelliSenseContext(
                    Workspace.Projects.First().Documents.First().GetTextView(),
                    Workspace.Projects.First().Documents.Last().GetTextBuffer(),
                    span,
                    componentModel,
                    isImmediateWindow)
            End If

            _context.TryInitialize()
        End Sub

        Public Overrides ReadOnly Property TextView As ITextView
            Get
                Return _context.DebuggerTextView
            End Get
        End Property

        Public Overrides ReadOnly Property SubjectBuffer As ITextBuffer
            Get
                Return _context.Buffer
            End Get
        End Property

        Public ReadOnly Property IsImmediateWindow As Boolean
            Get
                Return False
            End Get
        End Property

        Public Shared Function CreateVisualBasicTestState(
                documentElement As XElement,
                isImmediateWindow As Boolean,
                Optional extraCompletionProviders As CompletionListProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing) As TestState

            Return New TestState(documentElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                CreateLazyProviders(extraSignatureHelpProviders, LanguageNames.VisualBasic),
                isImmediateWindow)
        End Function

        Public Shared Function CreateCSharpTestState(
                workspaceElement As XElement,
                isImmediateWindow As Boolean,
                Optional extraCompletionProviders As CompletionListProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing) As TestState

            Return New TestState(
                workspaceElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.CSharp, roles:=Nothing),
                CreateLazyProviders(extraSignatureHelpProviders, LanguageNames.CSharp),
                isImmediateWindow)
        End Function

#Region "IntelliSense Operations"

        Public Overloads Sub SendEscape()
            MyBase.SendEscape(Sub(a, n) IntelliSenseCommandHandler.ExecuteCommand(a, n), Sub() Return)
        End Sub

        Public Overloads Sub SendDownKey()
            MyBase.SendDownKey(Sub(a, n) IntelliSenseCommandHandler.ExecuteCommand(a, n), Sub() Return)
        End Sub

        Public Overloads Sub SendUpKey()
            MyBase.SendUpKey(Sub(a, n) IntelliSenseCommandHandler.ExecuteCommand(a, n), Sub() Return)
        End Sub

#End Region

#Region "Completion Operations"
        Public Overloads Sub SendTab()
            Dim handler = DirectCast(CompletionCommandHandler, ICommandHandler(Of TabKeyCommandArgs))
            MyBase.SendTab(Sub(a, n) handler.ExecuteCommand(a, n), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn()
            Dim handler = DirectCast(CompletionCommandHandler, ICommandHandler(Of ReturnKeyCommandArgs))
            MyBase.SendReturn(Sub(a, n) handler.ExecuteCommand(a, n), Sub() EditorOperations.InsertNewLine())
            Me._context.RebuildSpans()
        End Sub

        Public Overloads Sub SendPageUp()
            Dim handler = DirectCast(CompletionCommandHandler, ICommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n) handler.ExecuteCommand(a, n), Sub() Return)
        End Sub

        Public Overloads Sub SendInvokeCompletionList()
            Dim handler = DirectCast(CompletionCommandHandler, ICommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n) handler.ExecuteCommand(a, n), Sub() Return)
        End Sub

        Public Overloads Sub SendCommitUniqueCompletionListItem()
            Dim handler = DirectCast(CompletionCommandHandler, ICommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n) handler.ExecuteCommand(a, n), Sub() Return)
        End Sub

        Public Overloads Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)
            AssertNoAsynchronousOperationsRunning()
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Async Function AssertNoCompletionSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If

            Assert.Null(Me.CurrentCompletionPresenterSession)
        End Function

        Public Async Function AssertCompletionSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentCompletionPresenterSession)
        End Function

        Public Async Function AssertCompletionItemsContainAll(ParamArray displayTexts As String()) As Task
            Await WaitForAsynchronousOperationsAsync()

            If Me.CurrentCompletionPresenterSession Is Nothing Then
                Assert.False(True, "No completion session active")
            End If

            For Each displayText In displayTexts
                If Not CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = displayText) Then
                    Assert.False(True, "Didn't find '" & displayText & "' in completion.")
                End If
            Next
        End Function

        Public Async Function AssertCompletionItemsContainNone(ParamArray displayTexts As String()) As Task
            Await WaitForAsynchronousOperationsAsync()

            If Me.CurrentCompletionPresenterSession Is Nothing Then
                Assert.False(True, "No completion session active")
            End If

            For Each displayText In displayTexts
                If CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = displayText) Then
                    Assert.False(True, "Found '" & displayText & "' in completion.")
                End If
            Next
        End Function

        Public Async Function AssertSelectedCompletionItem(
            Optional displayText As String = Nothing,
            Optional description As String = Nothing,
            Optional isSoftSelected As Boolean? = Nothing,
            Optional isHardSelected As Boolean? = Nothing
        ) As Task

            Await WaitForAsynchronousOperationsAsync()
            If isSoftSelected.HasValue Then
                Assert.Equal(isSoftSelected.Value, Me.CurrentCompletionPresenterSession.IsSoftSelected)
            End If

            If isHardSelected.HasValue Then
                Assert.Equal(isHardSelected.Value, Not Me.CurrentCompletionPresenterSession.IsSoftSelected)
            End If

            If displayText IsNot Nothing Then
                Assert.Equal(displayText, Me.CurrentCompletionPresenterSession.SelectedItem.DisplayText)
            End If

#If False Then
            If insertionText IsNot Nothing Then
                Assert.Equal(insertionText, Me.CurrentCompletionPresenterSession.SelectedItem.TextChange.NewText)
            End If
#End If

            If description IsNot Nothing Then
                Assert.Equal(description, (Await Me.CurrentCompletionPresenterSession.SelectedItem.GetDescriptionAsync()).GetFullText())
            End If
        End Function

#End Region

#Region "Signature Help and Completion Operations"

        Private Sub ExecuteCommand(Of TCommandArgs As CommandArgs)(args As TCommandArgs, finalHandler As Action)
            Dim sigHelpHandler = DirectCast(SignatureHelpCommandHandler, ICommandHandler(Of TCommandArgs))
            Dim compHandler = DirectCast(DirectCast(CompletionCommandHandler, Object), ICommandHandler(Of TCommandArgs))

            sigHelpHandler.ExecuteCommand(args, Sub() compHandler.ExecuteCommand(args, finalHandler))
        End Sub

        Public Overloads Sub SendTypeChars(typeChars As String)
            MyBase.SendTypeChars(typeChars, Sub(a, n) ExecuteCommand(a, n))
        End Sub
#End Region

#Region "Signature Help Operations"

        Public Overloads Sub SendInvokeSignatureHelp()
            Dim handler = DirectCast(SignatureHelpCommandHandler, ICommandHandler(Of InvokeSignatureHelpCommandArgs))
            MyBase.SendInvokeSignatureHelp(Sub(a, n) handler.ExecuteCommand(a, n), Sub() Return)
        End Sub

        Public Sub SendSelectSignatureHelpItemThroughPresenterSession(item As SignatureHelpItem)
            AssertNoAsynchronousOperationsRunning()
            CurrentSignatureHelpPresenterSession.SetSelectedItem(item)
        End Sub

        Public Async Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If

            Assert.Null(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Async Function AssertSignatureHelpSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Private Function GetDisplayText(item As SignatureHelpItem, selectedParameter As Integer) As String
            Dim suffix = If(selectedParameter < item.Parameters.Count,
                            GetDisplayText(item.Parameters(selectedParameter).SuffixDisplayParts),
                            String.Empty)
            Return String.Join(
                String.Empty,
                GetDisplayText(item.PrefixDisplayParts),
                String.Join(
                    GetDisplayText(item.SeparatorDisplayParts),
                    item.Parameters.Select(Function(p) GetDisplayText(p.DisplayParts))),
                GetDisplayText(item.SuffixDisplayParts),
                suffix)
        End Function

        Private Function GetDisplayText(parts As IEnumerable(Of SymbolDisplayPart)) As String
            Return String.Join(String.Empty, parts.Select(Function(p) p.ToString()))
        End Function

        Public Function SignatureHelpItemsContainsAll(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.All(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                       Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v))
        End Function

        Public Function SignatureHelpItemsContainsAny(displayText As String()) As Boolean
            AssertNoAsynchronousOperationsRunning()
            Return displayText.Any(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                       Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v))
        End Function

        Public Async Function AssertSelectedSignatureHelpItem(Optional displayText As String = Nothing,
                               Optional documentation As String = Nothing,
                               Optional selectedParameter As String = Nothing) As Task
            Await WaitForAsynchronousOperationsAsync()

            If displayText IsNot Nothing Then
                Assert.Equal(displayText, GetDisplayText(Me.CurrentSignatureHelpPresenterSession.SelectedItem, Me.CurrentSignatureHelpPresenterSession.SelectedParameter.Value))
            End If

            If documentation IsNot Nothing Then
                Assert.Equal(documentation, Me.CurrentSignatureHelpPresenterSession.SelectedItem.DocumentationFactory(CancellationToken.None).GetFullText())
            End If

            If selectedParameter IsNot Nothing Then
                Assert.Equal(selectedParameter, GetDisplayText(
                    Me.CurrentSignatureHelpPresenterSession.SelectedItem.Parameters(
                        Me.CurrentSignatureHelpPresenterSession.SelectedParameter.Value).DisplayParts))
            End If
        End Function
#End Region

        Public Function GetCurrentViewLineText() As String
            Return Me.TextView.TextViewLines.Last().Extent.GetText()
        End Function

    End Class
End Namespace
