' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense
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
        Inherits IntelliSense.TestState

        Friend ReadOnly SignatureHelpCommandHandler As SignatureHelpBeforeCompletionCommandHandler
        Friend ReadOnly CompletionCommandHandler As VSCommanding.ICommandHandler

        Private _context As AbstractDebuggerIntelliSenseContext

        Private Sub New(workspaceElement As XElement,
                        extraCompletionProviders As IEnumerable(Of Lazy(Of CompletionProvider, OrderableLanguageAndRoleMetadata)),
                        isImmediateWindow As Boolean)

            MyBase.New(
                workspaceElement,
                extraCompletionProviders:=Nothing,
                excludedTypes:=CombineExcludedTypes(),
                extraExportedTypes:=CombineExtraTypes(),
                workspaceKind:=WorkspaceKind.Debugger,
                includeFormatCommandHandler:=False)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            If extraCompletionProviders IsNot Nothing Then
                Dim completionService = DirectCast(languageServices.GetService(Of CompletionService), CommonCompletionService)
                completionService.SetTestProviders(extraCompletionProviders.Select(Function(lz) lz.Value).ToList())
            End If

            Me.CompletionCommandHandler = GetExportedValues(Of VSCommanding.ICommandHandler)().
                Single(Function(e As VSCommanding.ICommandHandler) e.GetType().FullName = "Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation.CompletionCommandHandler")

            Me.SignatureHelpCommandHandler = Workspace.GetService(Of SignatureHelpBeforeCompletionCommandHandler)

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

        Private Shared Function CombineExcludedTypes() As List(Of Type)
            Return New List(Of Type) From {
                GetType(IIntelliSensePresenter(Of ICompletionPresenterSession, ICompletionSession)),
                GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            }
        End Function

        Private Shared Function CombineExtraTypes() As List(Of Type)
            Return New List(Of Type) From {
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
                Optional extraCompletionProviders As CompletionProvider() = Nothing) As TestState

            Return New TestState(documentElement,
                CreateLazyProviders(extraCompletionProviders, LanguageNames.VisualBasic, roles:=Nothing),
                isImmediateWindow)
        End Function

        Public Shared Function CreateCSharpTestState(
                workspaceElement As XElement,
                isImmediateWindow As Boolean,
                Optional extraCompletionProviders As CompletionProvider() = Nothing) As TestState

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

        Public Function GetCurrentViewLineText() As String
            Return Me.TextView.TextViewLines.Last().Extent.GetText()
        End Function

    End Class
End Namespace
