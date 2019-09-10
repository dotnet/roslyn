' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.TextManager
Imports Microsoft.VisualStudio.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    Friend Class TestState
        Inherits AbstractCommandHandlerTestState

        Friend Const RoslynItem = "RoslynItem"

        Friend ReadOnly SignatureHelpCommandHandler As SignatureHelpBeforeCompletionCommandHandler
        Friend ReadOnly CompletionCommandHandler As VSCommanding.ICommandHandler

        Private _context As AbstractDebuggerIntelliSenseContext
        Private ReadOnly SessionTestState As IIntelliSenseTestState

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        Private Sub New(workspaceElement As XElement,
                        extraCompletionProviders As IEnumerable(Of Lazy(Of CompletionProvider, OrderableLanguageAndRoleMetadata)),
                        isImmediateWindow As Boolean)

            MyBase.New(
                workspaceElement,
                excludedTypes:=CombineExcludedTypes(),
                extraParts:=ExportProviderCache.CreateTypeCatalog(CombineExtraTypes()),
                workspaceKind:=WorkspaceKind.Debugger)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            If extraCompletionProviders IsNot Nothing Then
                Dim completionService = DirectCast(languageServices.GetService(Of CompletionService), CommonCompletionService)
                completionService.SetTestProviders(extraCompletionProviders.Select(Function(lz) lz.Value).ToList())
            End If

            Me.SessionTestState = GetExportedValue(Of IIntelliSenseTestState)()

            Me.CompletionCommandHandler = GetExportedValues(Of VSCommanding.ICommandHandler)().
                Single(Function(e As VSCommanding.ICommandHandler) e.GetType().FullName = "Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation.CompletionCommandHandler")

            Me.SignatureHelpCommandHandler = Workspace.GetService(Of SignatureHelpBeforeCompletionCommandHandler)

            Dim featureServiceFactory = GetExportedValue(Of IFeatureServiceFactory)()
            featureServiceFactory.GlobalFeatureService.Disable(PredefinedEditorFeatureNames.AsyncCompletion, EmptyFeatureController.Instance)

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

        Private Shared Function CombineExcludedTypes() As IList(Of Type)
            Return New List(Of Type) From {
                GetType(IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)),
                GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            }
        End Function

        Private Shared Function CombineExtraTypes() As IList(Of Type)
            Return New List(Of Type) From {
                GetType(TestCompletionPresenter),
                GetType(TestSignatureHelpPresenter),
                GetType(IntelliSenseTestState)
            }
        End Function

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
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing) As TestState

            Return New TestState(documentElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                isImmediateWindow)
        End Function

        Public Shared Function CreateCSharpTestState(
                workspaceElement As XElement,
                isImmediateWindow As Boolean,
                Optional extraCompletionProviders As CompletionProvider() = Nothing,
                Optional extraSignatureHelpProviders As ISignatureHelpProvider() = Nothing) As TestState

            Return New TestState(
                workspaceElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.CSharp, roles:=Nothing),
                isImmediateWindow)
        End Function

#Region "Completion Operations"
        Public Overloads Sub SendTab()
            Dim handler = DirectCast(CompletionCommandHandler, IChainedCommandHandler(Of TabKeyCommandArgs))
            MyBase.SendTab(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn()
            Dim handler = DirectCast(CompletionCommandHandler, IChainedCommandHandler(Of ReturnKeyCommandArgs))
            MyBase.SendReturn(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertNewLine())
            Me._context.RebuildSpans()
        End Sub

        Public Overloads Sub SendInvokeCompletionList()
            Dim handler = DirectCast(CompletionCommandHandler, IChainedCommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendToggleCompletionMode()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.ICommandHandler(Of ToggleCompletionModeCommandArgs))
            MyBase.SendToggleCompletionMode(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Function HasSuggestedItem() As Boolean
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.SuggestionItem IsNot Nothing
        End Function

        Public Async Function AssertNoCompletionSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If session Is Nothing Then
                Return
            End If

            If session.IsDismissed Then
                Return
            End If

            Dim completionItems = session.GetComputedItems(CancellationToken.None)
            ' During the computation we can explicitly dismiss the session or we can return no items.
            ' Each of these conditions mean that there is no active completion.
            Assert.True(session.IsDismissed OrElse completionItems.Items.Count() = 0, "AssertNoCompletionSession")
        End Function

        Public Async Function AssertCompletionSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim view = TextView

            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.True(session IsNot Nothing, NameOf(AssertCompletionSession))
        End Function

        Public Function GetCompletionItems() As IList(Of CompletionItem)
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Return session.GetComputedItems(CancellationToken.None).Items.Select(Function(item) GetRoslynCompletionItem(item)).ToList()
        End Function

        Private Shared Function GetRoslynCompletionItem(item As Data.CompletionItem) As CompletionItem
            Return If(item IsNot Nothing, DirectCast(item.Properties(RoslynItem), CompletionItem), Nothing)
        End Function

        Public Async Function AssertCompletionItemsContainAll(ParamArray displayTexts As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.True(displayTexts.All(Function(v) items.Any(Function(i) i.DisplayText = v)))
        End Function

        Public Async Function AssertCompletionItemsContainNone(ParamArray displayTexts As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.False(displayTexts.Any(Function(v) items.Any(Function(i) i.DisplayText = v)))
        End Function

        Public Async Function AssertSelectedCompletionItem(
                                                    Optional displayText As String = Nothing,
                                                    Optional displayTextSuffix As String = Nothing,
                                                    Optional description As String = Nothing,
                                                    Optional isSoftSelected As Boolean? = Nothing,
                                                    Optional isHardSelected As Boolean? = Nothing,
                                                    Optional shouldFormatOnCommit As Boolean? = Nothing,
                                                    Optional inlineDescription As String = Nothing,
                                                    Optional automationText As String = Nothing,
                                                    Optional projectionsView As ITextView = Nothing) As Task

            Await WaitForAsynchronousOperationsAsync()
            Dim view = If(projectionsView, TextView)

            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)

            If isSoftSelected.HasValue Then
                If isSoftSelected.Value Then
                    Assert.True(items.UsesSoftSelection, "Current completion is not soft-selected. Expected: soft-selected")
                Else
                    Assert.False(items.UsesSoftSelection, "Current completion is soft-selected. Expected: not soft-selected")
                End If
            End If

            If isHardSelected.HasValue Then
                If isHardSelected.Value Then
                    Assert.True(Not items.UsesSoftSelection, "Current completion is not hard-selected. Expected: hard-selected")
                Else
                    Assert.True(items.UsesSoftSelection, "Current completion is hard-selected. Expected: not hard-selected")
                End If
            End If

            If displayText IsNot Nothing Then
                Assert.NotNull(items.SelectedItem)
                If displayTextSuffix IsNot Nothing Then
                    Assert.NotNull(items.SelectedItem)
                    Assert.Equal(displayText + displayTextSuffix, items.SelectedItem.DisplayText)
                Else
                    Assert.Equal(displayText, items.SelectedItem.DisplayText)
                End If
            End If

            If shouldFormatOnCommit.HasValue Then
                Assert.Equal(shouldFormatOnCommit.Value, GetRoslynCompletionItem(items.SelectedItem).Rules.FormatOnCommit)
            End If

            If description IsNot Nothing Then
                Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim roslynItem = GetRoslynCompletionItem(items.SelectedItem)
                Dim itemDescription = Await service.GetDescriptionAsync(document, roslynItem)
                Assert.Equal(description, itemDescription.Text)
            End If

            If inlineDescription IsNot Nothing Then
                Assert.Equal(inlineDescription, items.SelectedItem.Suffix)
            End If

            If automationText IsNot Nothing Then
                Assert.Equal(automationText, items.SelectedItem.AutomationText)
            End If
        End Function

#End Region

#Region "Signature Help and Completion Operations"

        Private Sub ExecuteCommand(Of TCommandArgs As EditorCommandArgs)(args As TCommandArgs, finalHandler As Action, context As CommandExecutionContext)
            Dim sigHelpHandler = DirectCast(SignatureHelpCommandHandler, IChainedCommandHandler(Of TCommandArgs))
            Dim compHandler = DirectCast(DirectCast(CompletionCommandHandler, Object), IChainedCommandHandler(Of TCommandArgs))

            sigHelpHandler.ExecuteCommand(args, Sub() compHandler.ExecuteCommand(args, finalHandler, context), context)
        End Sub

        Public Overloads Sub SendTypeChars(typeChars As String)
            MyBase.SendTypeChars(typeChars, Sub(a, n, c) ExecuteCommand(a, n, c))
        End Sub
#End Region

#Region "Signature Help Operations"

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

        Private Function GetDisplayText(parts As IEnumerable(Of TaggedText)) As String
            Return String.Join(String.Empty, parts.Select(Function(p) p.ToString()))
        End Function

#End Region

        Public Function GetCurrentViewLineText() As String
            Return Me.TextView.TextViewLines.Last().Extent.GetText()
        End Function

    End Class
End Namespace
