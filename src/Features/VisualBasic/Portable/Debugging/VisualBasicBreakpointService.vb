' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging

    <ExportLanguageService(GetType(IBreakpointResolutionService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicBreakpointResolutionService
        Implements IBreakpointResolutionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Shared Async Function GetBreakpointAsync(document As Document, position As Integer, length As Integer, cancellationToken As CancellationToken) As Task(Of BreakpointResolutionResult)
            Try
                Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                If tree Is Nothing Then
                    Return Nothing
                End If

                ' Non-zero length means that the span is passed by the debugger and we may need validate it.
                ' In a rare VB case, this span may contain multiple methods, e.g., 
                '
                '    [Sub Goo() Handles A
                '
                '     End Sub
                '
                '     Sub Bar() Handles B]
                '
                '     End Sub
                '
                ' It happens when IDE services (e.g., NavBar or CodeFix) inserts a new method at the beginning of the existing one
                ' which happens to have a breakpoint on its head. In this situation, we should attempt to validate the span of the existing method,
                ' not that of a newly-prepended method.

                If length > 0 Then
                    Dim root = Await tree.GetRootAsync(cancellationToken).ConfigureAwait(False)

                    Dim item = root.DescendantNodes(
                            span:=New TextSpan(position, length),
                            descendIntoChildren:=Function(n) Not n.IsKind(SyntaxKind.ConstructorBlock, SyntaxKind.SubBlock)).
                        OfType(Of MethodBlockSyntax).Skip(1).LastOrDefault()

                    If item IsNot Nothing Then
                        position = item.SpanStart
                    End If
                End If

                Dim span As TextSpan
                If Not BreakpointSpans.TryGetBreakpointSpan(tree, position, cancellationToken, span) Then
                    Return Nothing
                End If

                If span.Length = 0 Then
                    Return BreakpointResolutionResult.CreateLineResult(document)
                End If

                Return BreakpointResolutionResult.CreateSpanResult(document, span)
            Catch e As Exception When FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken)
                Return Nothing
            End Try
        End Function

        Public Function ResolveBreakpointAsync(document As Document, textSpan As TextSpan, Optional cancellationToken As CancellationToken = Nothing) As Task(Of BreakpointResolutionResult) Implements IBreakpointResolutionService.ResolveBreakpointAsync
            Return GetBreakpointAsync(document, textSpan.Start, textSpan.Length, cancellationToken)
        End Function

        Public Function ResolveBreakpointsAsync(
            solution As Solution,
            name As String,
            Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of BreakpointResolutionResult)) Implements IBreakpointResolutionService.ResolveBreakpointsAsync

            Return New BreakpointResolver(solution, name).DoAsync(cancellationToken)
        End Function
    End Class
End Namespace
