' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QuickInfo
    Public MustInherit Class SemanticQuickInfoSourceTestsBase
        Inherits AbstractSemanticQuickInfoSourceTests

        Protected Overrides Function TestAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Return TestWithReferencesAsync(markup, Array.Empty(Of String)(), expectedResults)
        End Function

        Protected Async Function TestSharedAsync(workspace As TestWorkspace, position As Integer, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim service = workspace.Services _
                .GetLanguageServices(LanguageNames.VisualBasic) _
                .GetService(Of QuickInfoService)

            Await TestSharedAsync(workspace, service, position, expectedResults)

            ' speculative semantic model
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
            If Await CanUseSpeculativeSemanticModelAsync(document, position) Then
                Dim buffer = workspace.Documents.Single().GetTextBuffer()
                Using edit = buffer.CreateEdit()
                    edit.Replace(0, buffer.CurrentSnapshot.Length, buffer.CurrentSnapshot.GetText())
                    edit.Apply()
                End Using

                Await TestSharedAsync(workspace, service, position, expectedResults)
            End If
        End Function

        Private Async Function TestSharedAsync(workspace As TestWorkspace, service As QuickInfoService, position As Integer, expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim info = Await service.GetQuickInfoAsync(
                workspace.CurrentSolution.Projects.First().Documents.First(),
                position, cancellationToken:=CancellationToken.None)

            If expectedResults Is Nothing Then
                Assert.Null(info)
            Else
                Assert.NotNull(info)

                For Each expected In expectedResults
                    expected(info)
                Next
            End If
        End Function

        Protected Async Function TestFromXmlAsync(markup As String, ParamArray expectedResults As Action(Of QuickInfoItem)()) As Task
            Using workspace = TestWorkspace.Create(markup)
                Await TestSharedAsync(workspace, workspace.Documents.First().CursorPosition.Value, expectedResults)
            End Using
        End Function

        Protected Async Function TestWithReferencesAsync(markup As String, metadataReferences As String(), ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim code As String = Nothing
            Dim position As Integer = Nothing
            MarkupTestFile.GetPosition(markup, code, position)

            Using workspace = TestWorkspace.CreateVisualBasic(code, VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest), metadataReferences:=metadataReferences)
                Await TestSharedAsync(workspace, position, expectedResults)
            End Using
        End Function

        Protected Async Function TestWithImportsAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim markupWithImports =
             "Imports System" & vbCrLf &
             "Imports System.Collections.Generic" & vbCrLf &
             "Imports System.Linq" & vbCrLf &
             markup

            Await TestAsync(markupWithImports, expectedResults)
        End Function

        Protected Async Function TestInClassAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim markupInClass =
             "Class C" & vbCrLf &
             markup & vbCrLf &
             "End Class"

            Await TestWithImportsAsync(markupInClass, expectedResults)
        End Function

        Protected Async Function TestInMethodAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim markupInClass =
             "Class C" & vbCrLf &
             "Sub M()" & vbCrLf &
             markup & vbCrLf &
             "End Sub" & vbCrLf &
             "End Class"

            Await TestWithImportsAsync(markupInClass, expectedResults)
        End Function
    End Class
End Namespace
