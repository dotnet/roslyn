' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.TextManager

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    Friend Class TestState
        Inherits IntelliSense.TestState

        Private ReadOnly _context As AbstractDebuggerIntelliSenseContext
        Private Shared ReadOnly s_roles As ImmutableArray(Of String) = ImmutableArray.Create(PredefinedTextViewRoles.Editable, "DEBUGVIEW", PredefinedTextViewRoles.Interactive)

        Private Sub New(workspaceElement As XElement,
                        isImmediateWindow As Boolean)

            MyBase.New(
                workspaceElement,
                excludedTypes:=Nothing,
                extraExportedTypes:=Nothing,
                workspaceKind:=WorkspaceKind.Debugger,
                includeFormatCommandHandler:=False,
                makeSeparateBufferForCursor:=True,
                roles:=s_roles)

            Dim language = Workspace.CurrentSolution.Projects.First().Language

            Dim spanDocument = Workspace.Documents.First(Function(x) x.SelectedSpans.Any())
            Dim statementSpan = spanDocument.SelectedSpans.First()
            Dim span = New TextManager.Interop.TextSpan() {statementSpan.ToSnapshotSpan(spanDocument.GetTextBuffer().CurrentSnapshot).ToVsTextSpan()}

            Dim componentModel = New MockComponentModel(Workspace.ExportProvider)

            If language = LanguageNames.CSharp Then
                _context = New CSharpDebuggerIntelliSenseContext(
                    DirectCast(MyBase.TextView, IWpfTextView),
                    Workspace.Projects.First().Documents.Last().GetTextBuffer(),
                    span,
                    componentModel,
                    isImmediateWindow)
            Else
                ' VB
                _context = New VisualBasicDebuggerIntelliSenseContext(
                    DirectCast(MyBase.TextView, IWpfTextView),
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

        Public Shared Function CreateVisualBasicTestState(
                workspaceElement As XElement,
                isImmediateWindow As Boolean) As TestState

            Return New TestState(workspaceElement, isImmediateWindow)
        End Function

        Public Shared Function CreateCSharpTestState(
                workspaceElement As XElement,
                isImmediateWindow As Boolean) As TestState

            Return New TestState(workspaceElement, isImmediateWindow)
        End Function

        Public Function GetCurrentViewLineText() As String
            Return Me.TextView.TextViewLines.Last().Extent.GetText()
        End Function

        Public Async Function VerifyCompletionAndDotAfter(item As String) As Task
            SendTypeChars(item)
            Await WaitForAsynchronousOperationsAsync()
            Await AssertSelectedCompletionItem(item)
            SendTab()
            SendTypeChars(".")
            Await WaitForAsynchronousOperationsAsync()
            Await AssertCompletionSession()
            For i As Integer = 0 To item.Length
                SendBackspace()
            Next

            Await AssertNoCompletionSession()
        End Function

    End Class
End Namespace
